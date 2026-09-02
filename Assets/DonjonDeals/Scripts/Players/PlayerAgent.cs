using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    public enum EtatJoueur { Vivant, ATerre, Mort, Sorti }

    /// <summary>
    /// Un porteur. Déplacement lié à la caméra, charge qui ralentit et fait du bruit,
    /// un pacte par descente, et la possibilité de tomber sans mourir quand on n'est
    /// pas seul — c'est un camarade qui vous relève.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAgent : MonoBehaviour
    {
        [Header("Identité")]
        public int index;
        public string nom = "J1";
        public Color couleur = Color.white;

        [Header("Références")]
        public GameTuning reglages;
        public Animator animator;
        public Light lanterne;
        public Transform ancreArme;      // os handslot.r
        public Transform ancreBouclier;  // os handslot.l

        [Header("État")]
        public EtatJoueur etat = EtatJoueur.Vivant;
        public float pv = 100f, pvMax = 100f;
        public int valeur;
        public int charge;
        public List<string> besace = new List<string>();
        public string pacte;             // id, ou null
        public float pacteRestant;
        public PlayerAgent jure, jurePar, bouc;
        public float progressionRelevage;
        public int gagne;

        CharacterController cc;
        RunManager run;
        float cdAttaque, invulnerabilite, cdEsquive, esquiveRestante, derive, degatsEvites;

        static readonly int HashVitesse = Animator.StringToHash("vitesse");
        static readonly int HashFurtif  = Animator.StringToHash("furtif");
        static readonly int HashFrappe  = Animator.StringToHash("frappe");
        static readonly int HashTouche  = Animator.StringToHash("touche");
        static readonly int HashTombe   = Animator.StringToHash("tombe");

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            run = FindObjectOfType<RunManager>();
        }

        // ---------------------------------------------------------------- valeurs dérivées

        public int Capacite
        {
            get
            {
                int c = reglages.capacite;
                if (Boutique.Possede("besace")) c += 15;
                if (Boutique.Possede("rouage")) c -= 10;
                if (pacte == "geant") c = Mathf.RoundToInt(c * 0.6f);
                return c;
            }
        }

        public float TauxCharge => Mathf.Clamp(charge / (float)Capacite, 0f, 1.3f);

        public float VitesseBase
        {
            get
            {
                float v = reglages.vitesseBase;
                if (Boutique.Possede("besace")) v *= 0.94f;
                if (Boutique.Possede("rouage")) v *= 1.12f;
                if (Boutique.Possede("bouclier")) v *= 0.92f;
                if (pacte == "ivresse") v *= 1.4f;
                return v;
            }
        }

        public float Vitesse
        {
            get
            {
                float f = TauxCharge;
                float v = VitesseBase * (1f - reglages.penaliteCharge * f);
                // le mur : au-delà du seuil on ne court plus. Une pente, on s'y habitue ;
                // un mur, on le redoute.
                if (f > reglages.seuilBlocage) v = Mathf.Min(v, reglages.vitesseBloquee);
                if (Furtif) v *= 0.45f;
                return Mathf.Max(1f, v);
            }
        }

        /// <summary>À quelle distance les squelettes vous entendent.</summary>
        public float RayonDetection
        {
            get
            {
                float r = (reglages.bruitAVide + reglages.bruitParCharge * TauxCharge) * run.Eveil;
                if (Boutique.Possede("lanterne")) r *= 1.20f;
                if (Boutique.Possede("rouage")) r *= 1.10f;
                if (pacte == "geant") r *= 1.4f;
                if (pacte == "silence") r *= 0.15f;
                else if (run.PacteActifChezUnAutre("silence", this) != null) r *= 2f;
                if (Furtif) r *= reglages.bruitFurtif;
                return r;
            }
        }

        public float PorteeLanterne
        {
            get
            {
                float d = 11.5f;
                if (Boutique.Possede("lanterne")) d *= 1.35f;
                if (pacte == "lampe" && pacteRestant > 0f) d *= 2f;
                else if (run.PacteActifChezUnAutre("lampe", this) != null) d *= 0.5f;
                return d;
            }
        }

        public bool Furtif { get; private set; }
        public Vector2 Direction { get; set; }   // remplie par PlayerInputRouter
        public bool MaintientInteraction { get; set; }

        // ---------------------------------------------------------------- boucle

        void Update()
        {
            if (run == null || !run.EnCours) return;
            float dt = Time.deltaTime;

            cdAttaque = Mathf.Max(0f, cdAttaque - dt);
            invulnerabilite = Mathf.Max(0f, invulnerabilite - dt);
            cdEsquive = Mathf.Max(0f, cdEsquive - dt);
            esquiveRestante = Mathf.Max(0f, esquiveRestante - dt);

            if (pacteRestant > 0f)
            {
                pacteRestant -= dt;
                if (pacteRestant <= 0f) FinDePacte();
            }
            if (lanterne != null) lanterne.range = PorteeLanterne;
            if (etat != EtatJoueur.Vivant) return;

            Deplacer(dt);
        }

        void Deplacer(float dt)
        {
            float v = esquiveRestante > 0f ? reglages.vitesseEsquive : Vitesse;
            Vector2 e = Direction;
            bool bouge = e.sqrMagnitude > 0.0025f;

            if (bouge || esquiveRestante > 0f)
            {
                // base liée à la caméra : « en avant » va vers le fond de l'écran.
                // L'ivresse fait tourner lentement ce repère : c'est tout le pacte.
                if (pacte == "ivresse") derive += dt * 0.55f;
                float yaw = (PartyCamera.Instance ? PartyCamera.Instance.Yaw : 0f) + derive;
                float sy = Mathf.Sin(yaw), cy = Mathf.Cos(yaw);
                Vector3 monde = new Vector3(cy * e.x + sy * e.y, 0f, -sy * e.x + cy * e.y);
                if (monde.sqrMagnitude > 1f) monde.Normalize();
                cc.Move(monde * v * dt + Physics.gravity * dt * 0.2f);
                if (monde.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(monde), 1f - Mathf.Pow(0.0001f, dt));
            }

            if (animator != null)
            {
                animator.SetFloat(HashVitesse, bouge ? v : 0f);
                animator.SetBool(HashFurtif, Furtif);
            }
        }

        public void ReglerFurtif(bool f) => Furtif = f;

        // ---------------------------------------------------------------- actions

        public void Attaquer()
        {
            if (etat != EtatJoueur.Vivant || cdAttaque > 0f || esquiveRestante > 0f) return;
            cdAttaque = reglages.recuperationAttaque * (Boutique.Possede("lame") ? 1.18f : 1f);
            if (animator != null) animator.SetTrigger(HashFrappe);

            float portee = pacte == "geant" ? reglages.porteeAttaque * 1.6f : reglages.porteeAttaque;
            float mult = Boutique.Possede("lame") ? 1.35f : 1f;
            if (pacte == "serment" && jure != null && jure.etat == EtatJoueur.Vivant) mult *= 1.5f;
            if (jurePar != null && jurePar.etat == EtatJoueur.Vivant) mult *= 1.5f;

            foreach (var s in run.Squelettes)
            {
                if (s == null || s.Mort) continue;
                Vector3 d = s.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude > portee) continue;
                if (Vector3.Dot(d.normalized, transform.forward) < 0.35f) continue;
                s.Blesser(Random.Range(reglages.degatsMin, reglages.degatsMax) * mult, this);
            }
        }

        public void Esquiver()
        {
            if (etat != EtatJoueur.Vivant || cdEsquive > 0f) return;
            if (Direction.sqrMagnitude < 0.01f) return;
            cdEsquive = reglages.recuperationEsquive;
            esquiveRestante = reglages.dureeEsquive;
            invulnerabilite = Mathf.Max(invulnerabilite, 0.34f);
        }

        // ---------------------------------------------------------------- besace

        public bool PeutPrendre(LootDef d) => d.fragment || charge + d.poids <= Capacite;

        public void Prendre(LootDef d, bool silencieux = false)
        {
            if (d.fragment) { run.FragmentTrouve(); return; }
            besace.Add(d.id);
            int v = d.valeur;
            if (pacte == "meche") v = Mathf.RoundToInt(v * 1.5f);
            if (run.AubeSignee) v *= 2;
            valeur += v;
            charge += d.poids;
            if (pacte == "meche") run.AvancerLaMeche(this);
            if (!silencieux) run.Journal($"{nom} : {d.nom}  +{v}");
        }

        /// <summary>Lâche la pièce la plus lourde — le geste que le jeu doit rendre facile.</summary>
        public string LacherLaPlusLourde(bool forcer = false)
        {
            if (besace.Count == 0) return null;
            if (etat != EtatJoueur.Vivant && !forcer) return null;
            int idx = 0;
            for (int i = 1; i < besace.Count; i++)
                if (reglages.Loot(besace[i]).poids > reglages.Loot(besace[idx]).poids) idx = i;
            string id = besace[idx];
            var d = reglages.Loot(id);
            besace.RemoveAt(idx);
            int v = d.valeur;
            if (pacte == "meche") v = Mathf.RoundToInt(v * 1.5f);
            if (run.AubeSignee) v *= 2;
            valeur = Mathf.Max(0, valeur - v);
            charge = Mathf.Max(0, charge - d.poids);
            run.PoserButinAuSol(id, transform.position - transform.forward * 1.4f);
            run.Journal($"{nom} lâche {d.nom}");
            return id;
        }

        // ---------------------------------------------------------------- dégâts

        public void Blesser(float degats)
        {
            if (etat != EtatJoueur.Vivant) return;

            // le bouc : quelqu'un d'autre paie, et il le sait
            if (pacte == "bouc" && pacteRestant > 0f && bouc != null && bouc.etat == EtatJoueur.Vivant)
            { bouc.Blesser(degats); return; }

            // le sursis : la note est différée, pas annulée
            if (pacte == "sursis" && pacteRestant > 0f) { degatsEvites += degats; return; }

            if (invulnerabilite > 0f) return;
            if (Boutique.Possede("bouclier")) degats *= 0.75f;
            pv -= degats;
            invulnerabilite = reglages.invulnerabiliteApresCoup;
            if (animator != null) animator.SetTrigger(HashTouche);
            if (pv <= 0f) Tomber();
        }

        public void Tuer() { pv = 0f; Tomber(); }

        public void Tomber()
        {
            pv = 0f;
            if (animator != null) animator.SetTrigger(HashTombe);
            if (besace.Count > 0) run.DeposerBesace(this);

            if (run.NombreDeJoueurs > 1)
            {
                etat = EtatJoueur.ATerre;
                progressionRelevage = 0f;
                run.Journal($"{nom} est à terre. Maintenez la touche pour le relever.");
            }
            else
            {
                etat = EtatJoueur.Mort;
                run.DemanderFin(FinDeDescente.Mort, 1.4f);
            }

            if (jurePar != null && jurePar.etat == EtatJoueur.Vivant)
                run.LierParLeSerment(jurePar, 0.4f);

            run.VerifierFin();
        }

        public void Relever(PlayerAgent par, float dt)
        {
            progressionRelevage += dt;
            if (progressionRelevage < reglages.dureeRelevage) return;
            progressionRelevage = 0f;
            etat = EtatJoueur.Vivant;
            pv = reglages.pvApresRelevage;
            invulnerabilite = 1.2f;
            run.Journal($"{par.nom} relève {nom}.");
        }

        public void Sortir(float multiplicateur, string parOu)
        {
            gagne = Mathf.RoundToInt(valeur * multiplicateur);
            etat = EtatJoueur.Sorti;
            gameObject.SetActive(false);
            run.Journal($"{nom} remonte par {parOu} — {gagne} pièces.");
            run.VerifierFin();
        }

        void FinDePacte()
        {
            if (pacte == "sursis")
            {
                float d = degatsEvites;
                degatsEvites = 0f;
                invulnerabilite = 0f;
                run.Journal($"{nom} — le sursis s'achève : {Mathf.RoundToInt(d)} points.");
                Blesser(d);
            }
        }
    }
}

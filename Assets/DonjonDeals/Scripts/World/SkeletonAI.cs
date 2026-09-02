using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Le squelette n'a pas de champ de vision : il a une oreille.
    ///
    /// C'est le pivot de tout le jeu. Un joueur les mains vides est presque
    /// inaudible ; un joueur chargé s'entend de loin, et le gisement se réveille
    /// au fil de la descente. L'avidité n'est donc pas punie par une règle
    /// arbitraire mais par une conséquence physique : on fait du bruit.
    ///
    /// Voir PlayerAgent.RayonDetection — c'est le joueur qui porte son propre
    /// rayon, pas le squelette. Les objets de boutique et les pactes s'y branchent
    /// tous naturellement, sans que cette classe ait à les connaître.
    /// </summary>
    public class SkeletonAI : MonoBehaviour
    {
        public GameTuning reglages;
        public Animator animator;
        public Renderer yeux;          // matériau émissif : le dernier repère quand tout s'éteint
        public float pv = 60f;
        public float vitesse = 3.4f;
        public float portee = 2.2f;
        public float degats = 14f;
        public string butinLache;      // id de LootDef, ou vide

        public bool Mort { get; private set; }

        RunManager run;
        CharacterController cc;
        PlayerAgent cible;
        Vector3 ancre, but;
        float cdAttaque, cdRecherche, alerte;

        static readonly int HashVitesse = Animator.StringToHash("vitesse");
        static readonly int HashFrappe  = Animator.StringToHash("frappe");
        static readonly int HashTouche  = Animator.StringToHash("touche");
        static readonly int HashMeurt   = Animator.StringToHash("meurt");

        void Start()
        {
            run = RunManager.Instance;
            cc = GetComponent<CharacterController>();
            ancre = transform.position;
            but = ancre;
        }

        void Update()
        {
            if (Mort || run == null || !run.EnCours) return;
            float dt = Time.deltaTime;
            cdAttaque = Mathf.Max(0f, cdAttaque - dt);
            cdRecherche -= dt;

            if (cdRecherche <= 0f) { cdRecherche = 0.25f; ChercherUneProie(); }

            if (cible != null) Poursuivre(dt);
            else               Rôder(dt);

            if (yeux != null)
            {
                // les yeux brillent d'autant plus que la bête est excitée ;
                // pendant le pacte de la lampe, c'est tout ce que les autres voient.
                float k = cible != null ? 1f : 0.35f;
                yeux.material.SetColor("_EmissionColor", Palette.Rouille * (0.6f + k * 2.4f));
            }
        }

        void ChercherUneProie()
        {
            PlayerAgent meilleur = null;
            float meilleureMarge = 0f;
            foreach (var j in run.Joueurs)
            {
                if (j == null || j.etat != EtatJoueur.Vivant) continue;
                float d = Vector3.Distance(Plat(j.transform.position), Plat(transform.position));
                float ecoute = j.RayonDetection;
                if (d > ecoute) continue;
                float marge = ecoute - d;                 // le plus bruyant l'emporte
                if (marge > meilleureMarge) { meilleureMarge = marge; meilleur = j; }
            }
            cible = meilleur;
            if (cible != null) alerte = 2.5f;
            else
            {
                alerte -= 0.25f;
                if (alerte > 0f) cible = null;            // il continue vers le dernier bruit
            }
        }

        void Poursuivre(float dt)
        {
            Vector3 d = Plat(cible.transform.position) - Plat(transform.position);
            float dist = d.magnitude;

            if (dist <= portee)
            {
                Avancer(Vector3.zero, dt);
                if (cdAttaque <= 0f)
                {
                    cdAttaque = 1.25f;
                    if (animator != null) animator.SetTrigger(HashFrappe);
                    cible.Blesser(degats);
                }
                Regarder(d);
                return;
            }
            Avancer(d.normalized * vitesse, dt);
            Regarder(d);
        }

        void Rôder(float dt)
        {
            if (Vector3.Distance(Plat(transform.position), Plat(but)) < 0.6f)
            {
                var c = Random.insideUnitCircle * MazeGenerator.Tile * 1.4f;
                but = ancre + new Vector3(c.x, 0f, c.y);
            }
            Vector3 d = Plat(but) - Plat(transform.position);
            Avancer(d.normalized * vitesse * 0.42f, dt);
            Regarder(d);
        }

        void Avancer(Vector3 v, float dt)
        {
            if (cc != null) cc.Move(v * dt + Physics.gravity * dt * 0.2f);
            else transform.position += v * dt;
            if (animator != null) animator.SetFloat(HashVitesse, v.magnitude);
        }

        void Regarder(Vector3 d)
        {
            if (d.sqrMagnitude < 0.0004f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(Plat(d)), 1f - Mathf.Pow(0.002f, Time.deltaTime));
        }

        public void Blesser(float degatsRecus, PlayerAgent par)
        {
            if (Mort) return;
            pv -= degatsRecus;
            cible = par;                 // se faire frapper, ça réveille
            alerte = 3f;
            if (animator != null) animator.SetTrigger(HashTouche);
            if (pv > 0f) return;

            Mort = true;
            if (animator != null) animator.SetTrigger(HashMeurt);
            if (cc != null) cc.enabled = false;
            if (!string.IsNullOrEmpty(butinLache))
                run.PoserButinAuSol(butinLache, transform.position);
            run.Journal($"{par.nom} a mis un squelette en pièces.");
            Destroy(gameObject, 6f);
        }

        static Vector3 Plat(Vector3 v) { v.y = 0f; return v; }
    }
}

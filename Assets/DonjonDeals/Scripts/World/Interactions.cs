using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Tout ce avec quoi on peut interagir hérite de cette classe et s'inscrit
    /// tout seul auprès du RunManager. Ajouter un nouvel objet interactif ne
    /// demande donc de toucher à rien d'autre que ce fichier.
    /// </summary>
    public abstract class Interactif : MonoBehaviour
    {
        public virtual float Rayon => 2.4f;
        /// <summary>Ce que l'invite à l'écran doit dire. Vide = pas d'invite.</summary>
        public abstract string Invite(PlayerAgent j);
        public abstract void Utiliser(PlayerAgent j);

        protected virtual void OnEnable()
        {
            if (RunManager.Instance != null) RunManager.Instance.Interactifs.Add(this);
        }
        protected virtual void OnDisable()
        {
            if (RunManager.Instance != null) RunManager.Instance.Interactifs.Remove(this);
        }
    }

    // ====================================================================== butin au sol

    public class Ramassable : Interactif
    {
        public string lootId;
        public bool ramassageAutomatique = true;   // marcher dessus suffit

        LootDef def;
        RunManager run;

        void Start()
        {
            run = RunManager.Instance;
            def = run.reglages.Loot(lootId);
        }

        void Update()
        {
            if (!ramassageAutomatique || run == null || !run.EnCours || def == null) return;
            foreach (var j in run.Joueurs)
            {
                if (j == null || j.etat != EtatJoueur.Vivant) continue;
                Vector3 d = j.transform.position - transform.position; d.y = 0f;
                if (d.sqrMagnitude > 1.7f * 1.7f) continue;
                if (!j.PeutPrendre(def)) continue;   // besace pleine : l'objet reste là
                j.Prendre(def);
                Destroy(gameObject);
                return;
            }
        }

        public override string Invite(PlayerAgent j)
        {
            if (def == null) return "";
            return j.PeutPrendre(def) ? $"Prendre {def.nom}" : "Besace pleine";
        }

        public override void Utiliser(PlayerAgent j)
        {
            if (def == null || !j.PeutPrendre(def)) return;
            j.Prendre(def);
            Destroy(gameObject);
        }
    }

    // ====================================================================== coffre

    public class Coffre : Interactif
    {
        public List<string> contenu = new List<string>();
        public Transform couvercle;
        public bool ouvert;

        public override string Invite(PlayerAgent j) => ouvert ? "" : "Ouvrir le coffre";

        public override void Utiliser(PlayerAgent j)
        {
            if (ouvert) return;
            ouvert = true;
            var run = RunManager.Instance;
            if (couvercle != null) couvercle.localRotation = Quaternion.Euler(-104f, 0f, 0f);

            // le contenu tombe au sol en couronne : le joueur doit choisir quoi porter,
            // c'est là que la décision se joue, pas au moment de l'ouverture.
            for (int i = 0; i < contenu.Count; i++)
            {
                float a = i / (float)Mathf.Max(1, contenu.Count) * Mathf.PI * 2f;
                run.PoserButinAuSol(contenu[i],
                    transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.25f);
            }
            run.Journal($"{j.nom} ouvre un coffre — {contenu.Count} pièces.");
            contenu.Clear();
        }
    }

    // ====================================================================== autel à pactes

    /// <summary>
    /// L'autel est le cœur du jeu : c'est là qu'on emprunte, et que quelqu'un
    /// d'autre rembourse. Un seul pacte par joueur et par descente — sinon la
    /// décision n'en est plus une.
    /// </summary>
    public class Autel : Interactif
    {
        public bool consomme;
        public Light flamme;

        public override string Invite(PlayerAgent j)
        {
            if (consomme) return "";
            return string.IsNullOrEmpty(j.pacte) ? "Passer un pacte" : "Vous avez déjà signé";
        }

        public override void Utiliser(PlayerAgent j)
        {
            if (consomme || !string.IsNullOrEmpty(j.pacte)) return;
            RunManager.Instance.OuvrirLAutel(this, j);
        }

        public void Eteindre()
        {
            consomme = true;
            if (flamme != null) flamme.intensity = 0.15f;
        }
    }

    // ====================================================================== sorties

    public class Sortie : Interactif
    {
        [Tooltip("Le puits du fond paie une prime, mais il est au bout du labyrinthe.")]
        public bool puitsDuFond;

        public override string Invite(PlayerAgent j)
        {
            var run = RunManager.Instance;
            if (!run.SortieOuverte)
                return $"Verrouillé — {run.FragmentsTrouves}/{run.reglages.fragmentsRequis} fragments";
            return puitsDuFond ? "Remonter par le puits du fond (+50 %)" : "Remonter";
        }

        public override void Utiliser(PlayerAgent j)
        {
            var run = RunManager.Instance;
            if (!run.SortieOuverte) return;
            float m = puitsDuFond ? run.reglages.primePuitsDuFond : 1f;
            j.Sortir(m, puitsDuFond ? "le puits du fond" : "l'escalier");
        }
    }

    // ====================================================================== aiguillage

    public static class Interactions
    {
        /// <summary>
        /// Appelé par PlayerInputRouter sur l'appui. On prend l'objet utile le
        /// plus proche ; relever un camarade passe avant tout le reste, parce que
        /// c'est l'action qu'on veut voir réussir sous pression.
        /// </summary>
        public static void Declencher(PlayerAgent j)
        {
            var run = RunManager.Instance;
            if (run == null || !run.EnCours || j.etat != EtatJoueur.Vivant) return;

            if (Aterre(j, run) != null) return;   // le relevage se fait par maintien, pas par appui

            Interactif meilleur = null;
            float meilleureDistance = float.MaxValue;
            foreach (var it in run.Interactifs)
            {
                if (it == null) continue;
                if (string.IsNullOrEmpty(it.Invite(j))) continue;
                Vector3 d = it.transform.position - j.transform.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist > it.Rayon || dist >= meilleureDistance) continue;
                meilleureDistance = dist; meilleur = it;
            }
            meilleur?.Utiliser(j);
        }

        /// <summary>Le camarade à terre le plus proche, s'il y en a un à portée.</summary>
        public static PlayerAgent Aterre(PlayerAgent j, RunManager run)
        {
            foreach (var a in run.Joueurs)
            {
                if (a == null || a == j || a.etat != EtatJoueur.ATerre) continue;
                Vector3 d = a.transform.position - j.transform.position; d.y = 0f;
                if (d.sqrMagnitude < 2.6f * 2.6f) return a;
            }
            return null;
        }

        /// <summary>Texte de l'invite à afficher sous le joueur. Vide = rien.</summary>
        public static string InvitePour(PlayerAgent j, RunManager run)
        {
            var a = Aterre(j, run);
            if (a != null) return $"Maintenir pour relever {a.nom}";

            string meilleur = "";
            float d0 = float.MaxValue;
            foreach (var it in run.Interactifs)
            {
                if (it == null) continue;
                string t = it.Invite(j);
                if (string.IsNullOrEmpty(t)) continue;
                Vector3 d = it.transform.position - j.transform.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist > it.Rayon || dist >= d0) continue;
                d0 = dist; meilleur = t;
            }
            return meilleur;
        }
    }
}

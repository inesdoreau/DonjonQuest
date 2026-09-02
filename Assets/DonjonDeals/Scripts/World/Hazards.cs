using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Base commune aux quatre pièges.
    ///
    /// Choix d'implémentation à connaître avant de modifier quoi que ce soit :
    /// les pièges ne s'appuient pas sur des colliders déclencheurs. Ils
    /// interrogent eux-mêmes la liste des joueurs par distance. C'est moins
    /// « Unity » mais nettement plus sûr ici : un CharacterController qui
    /// traverse un trigger en un seul pas de temps peut le rater, et un piège
    /// mortel raté ou déclenché deux fois se remarque tout de suite en partie.
    /// Il n'y a jamais plus de trente pièges et quatre joueurs : le coût est nul.
    /// </summary>
    public abstract class Hazard : MonoBehaviour
    {
        public Vector2Int caseGrille;
        protected RunManager run;

        protected virtual void Start()
        {
            run = RunManager.Instance;
        }

        protected bool Actif => run != null && run.EnCours;

        /// <summary>Joueurs debout à moins de <paramref name="rayon"/> mètres, à plat.</summary>
        protected List<PlayerAgent> Autour(float rayon)
        {
            var res = new List<PlayerAgent>();
            if (run == null) return res;
            float r2 = rayon * rayon;
            foreach (var j in run.Joueurs)
            {
                if (j == null || j.etat != EtatJoueur.Vivant) continue;
                Vector3 d = j.transform.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude <= r2) res.Add(j);
            }
            return res;
        }
    }

    // ====================================================================== piques

    /// <summary>
    /// Les piques tuent net — c'était une demande explicite : un piège qui
    /// grignote des points de vie ne fait pas peur, un piège qui tue fait
    /// regarder le sol.
    ///
    /// En contrepartie elles sont cycliques et lisibles : elles sortent, restent
    /// dehors, redescendent, et le générateur leur interdit les passages obligés.
    /// On peut donc toujours attendre le bon moment, ou passer ailleurs.
    /// </summary>
    public class SpikeTrap : Hazard
    {
        public float periode = 5f;
        public float phase;
        public Transform lames;          // le maillage qui monte et descend
        public Light lueur;              // facultative : la braise sous la dalle

        float baseY = 1f;

        /// <summary>0 = lames rentrées, 1 = lames sorties. Lisible pour l'UI et le débogage.</summary>
        public float Sortie { get; private set; }
        public bool Armee { get; private set; }

        protected override void Start()
        {
            base.Start();
            if (lames != null) baseY = lames.localScale.y;
        }

        void Update()
        {
            if (!Actif) return;

            float cy = Mathf.Repeat(run.Horloge + phase, periode) / periode;
            float e;
            if (cy < 0.12f)      e = cy / 0.12f;                  // ça monte
            else if (cy < 0.52f) e = 1f;                          // ça reste
            else if (cy < 0.64f) e = 1f - (cy - 0.52f) / 0.12f;   // ça redescend
            else                 e = 0f;                          // au repos

            Sortie = e;
            Armee = e > 0.55f;

            // on multiplie l'échelle d'origine : les modèles KayKit sont importés
            // à des échelles minuscules, écraser scale.y les ferait exploser.
            if (lames != null)
            {
                var s = lames.localScale;
                s.y = baseY * (0.05f + e * 0.95f);
                lames.localScale = s;
            }
            if (lueur != null) lueur.intensity = 0.5f + e * 2.2f;

            if (!Armee) return;
            foreach (var j in Autour(1.9f))
            {
                run.Journal($"{j.nom} s'est empalé.");
                j.Tuer();
            }
        }
    }

    // ====================================================================== rouleau

    /// <summary>
    /// Le rouleau balaie trois cases en va-et-vient. Il tue lui aussi, et le
    /// générateur vérifie que ses trois cases sont contournables.
    /// </summary>
    public class Roller : Hazard
    {
        public MazeGenerator.Axis axe;
        public float vitesse = 5.2f;
        public float amplitude = MazeGenerator.Tile;   // une case de part et d'autre

        Vector3 centre;

        protected override void Start()
        {
            base.Start();
            centre = transform.position;
        }

        void Update()
        {
            if (!Actif) return;

            float t = Mathf.Sin(run.Horloge * vitesse / amplitude);
            Vector3 dir = axe == MazeGenerator.Axis.NorthSouth ? Vector3.forward : Vector3.right;
            transform.position = centre + dir * t * amplitude;
            transform.Rotate((axe == MazeGenerator.Axis.NorthSouth ? Vector3.right : Vector3.forward),
                             Time.deltaTime * 220f * Mathf.Sign(Mathf.Cos(run.Horloge * vitesse / amplitude)),
                             Space.World);

            foreach (var j in Autour(1.7f))
            {
                run.Journal($"{j.nom} est passé sous le rouleau.");
                j.Tuer();
            }
        }
    }

    // ====================================================================== trappe

    /// <summary>
    /// La trappe ne tue pas : elle sépare. C'est le seul piège autorisé sur un
    /// passage obligé, précisément parce qu'il n'est pas mortel.
    ///
    /// La portée du saut est bornée en coopération : sur un écran unique, un
    /// joueur éjecté à l'autre bout de la carte disparaît du cadre et ne joue
    /// plus. Quatre à sept cases suffisent à casser le groupe sans le perdre.
    /// </summary>
    public class Trapdoor : Hazard
    {
        public Transform battant;
        public float dureeOuverte = 4f;

        bool ouverte;
        float restant;

        void Update()
        {
            if (!Actif) return;

            if (ouverte)
            {
                restant -= Time.deltaTime;
                if (battant != null)
                    battant.localRotation = Quaternion.Slerp(battant.localRotation,
                        Quaternion.Euler(0f, 0f, 96f), 1f - Mathf.Pow(0.001f, Time.deltaTime));
                if (restant <= 0f) { ouverte = false; }
                return;
            }

            if (battant != null)
                battant.localRotation = Quaternion.Slerp(battant.localRotation,
                    Quaternion.identity, 1f - Mathf.Pow(0.01f, Time.deltaTime));

            var proches = Autour(1.6f);
            if (proches.Count == 0) return;

            ouverte = true;
            restant = dureeOuverte;
            foreach (var j in proches) run.FaireTomberParLaTrappe(j, caseGrille);
        }
    }

    // ====================================================================== mur pivotant

    /// <summary>
    /// Le mur pivotant ne blesse personne : il ferme, puis il ouvre. Sa seule
    /// cruauté est le temps qu'il fait perdre — et le fait qu'il puisse couper
    /// une équipe en deux au mauvais moment.
    /// </summary>
    public class RotatingWall : Hazard
    {
        public float periode = 9f;
        public float phase;
        public Transform pivot;
        public MazeGenerator.Axis axe;

        void Update()
        {
            if (!Actif || pivot == null) return;

            // deux quarts de tour par période, avec un temps d'arrêt à chaque bout :
            // sans la pause, on ne peut jamais franchir le passage.
            float cy = Mathf.Repeat(run.Horloge + phase, periode) / periode;
            float a;
            if (cy < 0.38f)      a = 0f;
            else if (cy < 0.5f)  a = Mathf.SmoothStep(0f, 90f, (cy - 0.38f) / 0.12f);
            else if (cy < 0.88f) a = 90f;
            else                 a = Mathf.SmoothStep(90f, 180f, (cy - 0.88f) / 0.12f);

            float depart = axe == MazeGenerator.Axis.NorthSouth ? 0f : 90f;
            pivot.localRotation = Quaternion.Euler(0f, depart + a, 0f);
        }
    }
}

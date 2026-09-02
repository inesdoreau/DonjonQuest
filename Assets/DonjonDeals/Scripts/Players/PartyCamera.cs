using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Une seule caméra pour toute l'équipe.
    ///
    /// Deux décisions comptent ici :
    ///  · l'inclinaison est basse (30°), pour voir loin devant plutôt que sur les crânes ;
    ///  · le cadre s'ancre sur le joueur 1 et n'englobe que ceux qui sont à portée.
    ///    Sans cette limite, un joueur éjecté par une trappe tirait la caméra à
    ///    l'autre bout de la carte et tout le monde devenait minuscule.
    /// </summary>
    public class PartyCamera : MonoBehaviour
    {
        public static PartyCamera Instance { get; private set; }

        public GameTuning reglages;
        public List<PlayerAgent> joueurs = new List<PlayerAgent>();

        public float Yaw { get; private set; } = 0.6f;
        float distance, pitch, arrivee;
        Vector3 regard;

        void Awake() { Instance = this; }

        public void Commencer(Vector3 entree)
        {
            regard = entree + Vector3.up * 1.5f;
            distance = reglages.distanceBase + 26f;
            pitch = 1.23f;
            Yaw = 2.1f;
            arrivee = 2.2f;   // la descente d'ouverture
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var presents = joueurs.FindAll(j => j != null &&
                (j.etat == EtatJoueur.Vivant || j.etat == EtatJoueur.ATerre));
            if (presents.Count == 0) return;

            // les deux premières secondes : on descend du plafond jusqu'à l'équipe
            float inclinaisonCible = reglages.inclinaisonDegres * Mathf.Deg2Rad;
            if (arrivee > 0f)
            {
                arrivee = Mathf.Max(0f, arrivee - dt);
                float k = arrivee / 2.2f;
                float e = k * k * k;                  // atterrissage tout en douceur
                distance = reglages.distanceBase + e * 26f;
                pitch = inclinaisonCible + e * 0.70f;
                Yaw = 0.6f + e * 1.5f;
            }
            else pitch = inclinaisonCible;

            var ancre = presents.Find(j => j.index == 0) ?? presents[0];
            var groupe = presents.FindAll(j =>
                Vector3.Distance(Plat(j.transform.position), Plat(ancre.transform.position))
                    < reglages.rayonGroupe);
            if (groupe.Count == 0) groupe.Add(ancre);

            Vector3 centre = Vector3.zero;
            foreach (var j in groupe) centre += Plat(j.transform.position);
            centre /= groupe.Count;

            float ecart = 0f;
            foreach (var j in groupe)
                ecart = Mathf.Max(ecart, Vector3.Distance(Plat(j.transform.position), centre));

            if (arrivee <= 0f)
            {
                float vise = Mathf.Clamp(reglages.distanceBase + ecart * 1.15f,
                                         reglages.distanceBase, reglages.distanceMax);
                distance = Mathf.Lerp(distance, vise, 1f - Mathf.Pow(0.02f, dt));
            }

            regard = Vector3.Lerp(regard, centre + Vector3.up * 1.5f, 1f - Mathf.Pow(0.001f, dt));
            float h = Mathf.Sin(pitch) * distance, r = Mathf.Cos(pitch) * distance;
            transform.position = regard + new Vector3(Mathf.Sin(Yaw) * r, h, Mathf.Cos(Yaw) * r);
            transform.LookAt(regard);
        }

        public void TournerDe(float radians) => Yaw -= radians;

        static Vector3 Plat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>Position à l'écran d'un joueur hors cadre, pour dessiner sa pastille.</summary>
        public bool PastilleHorsCadre(PlayerAgent j, Camera cam, out Vector2 ecran)
        {
            Vector3 v = cam.WorldToViewportPoint(j.transform.position + Vector3.up * 1.2f);
            bool dedans = v.z > 0f && v.x > 0.04f && v.x < 0.96f && v.y > 0.04f && v.y < 0.96f;
            if (dedans) { ecran = Vector2.zero; return false; }
            float x = v.x - 0.5f, y = v.y - 0.5f;
            if (v.z < 0f) { x = -x; y = -y; }
            float m = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            if (m < 0.0001f) m = 1f;
            ecran = new Vector2((x / m) * 0.44f + 0.5f, (y / m) * 0.44f + 0.5f);
            return true;
        }
    }
}

using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Co-op canapé : le joueur 1 au clavier et à la souris, les joueurs 2 à 4 à la manette.
    ///
    /// Volontairement écrit avec l'ancien Input Manager, pour que le projet compile
    /// sans installer de paquet. Il faut déclarer dans Edit ▸ Project Settings ▸ Input
    /// les axes « J2_Horizontal », « J2_Vertical », « J2_Frappe »… jusqu'à J4 ; le
    /// fichier Docs/UNITY-INPUT.md donne le tableau complet.
    /// Si vous passez au nouvel Input System, seule cette classe est à réécrire.
    /// </summary>
    public class PlayerInputRouter : MonoBehaviour
    {
        public PlayerAgent[] joueurs;

        void Update()
        {
            for (int i = 0; i < joueurs.Length; i++)
            {
                var j = joueurs[i];
                if (j == null) continue;

                if (i == 0) LireClavier(j);
                else        LireManette(j, i + 1);
            }
        }

        void LireClavier(PlayerAgent j)
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            // « avancer » va vers le fond de l'écran : la composante Y est négative.
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y += 1f;

            var d = new Vector2(x, y);
            if (d.sqrMagnitude > 1f) d.Normalize();
            j.Direction = d;
            j.ReglerFurtif(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            j.MaintientInteraction = Input.GetKey(KeyCode.E);

            if (Input.GetMouseButtonDown(0)) j.Attaquer();
            if (Input.GetKeyDown(KeyCode.Space)) j.Esquiver();
            if (Input.GetKeyDown(KeyCode.E)) Interactions.Declencher(j);
            if (Input.GetKeyDown(KeyCode.R)) j.LacherLaPlusLourde();
        }

        void LireManette(PlayerAgent j, int numero)
        {
            string p = "J" + numero + "_";
            float x = Axe(p + "Horizontal");
            float y = Axe(p + "Vertical");
            var d = new Vector2(Mort(x), Mort(y));
            if (d.sqrMagnitude > 1f) d.Normalize();
            j.Direction = d;

            j.ReglerFurtif(Bouton(p + "Furtif"));
            j.MaintientInteraction = Bouton(p + "Interagir");

            if (BoutonBas(p + "Frappe"))    j.Attaquer();
            if (BoutonBas(p + "Esquive"))   j.Esquiver();
            if (BoutonBas(p + "Interagir")) Interactions.Declencher(j);
            if (BoutonBas(p + "Lacher"))    j.LacherLaPlusLourde();
        }

        static float Mort(float v) => Mathf.Abs(v) > 0.24f ? v : 0f;

        // ces enveloppes évitent une exception si un axe n'a pas encore été déclaré
        static float Axe(string nom)
        {
            try { return Input.GetAxisRaw(nom); } catch { return 0f; }
        }
        static bool Bouton(string nom)
        {
            try { return Input.GetButton(nom); } catch { return false; }
        }
        static bool BoutonBas(string nom)
        {
            try { return Input.GetButtonDown(nom); } catch { return false; }
        }

        public static int ManettesBranchees()
        {
            int n = 0;
            foreach (string s in Input.GetJoystickNames())
                if (!string.IsNullOrEmpty(s)) n++;
            return n;
        }
    }
}

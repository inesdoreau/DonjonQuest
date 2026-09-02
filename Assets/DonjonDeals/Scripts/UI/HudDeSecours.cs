using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Une interface complète mais volontairement laide, dessinée en IMGUI.
    ///
    /// Pourquoi : elle ne demande aucun Canvas, aucun prefab, aucune police
    /// importée. Elle permet donc de jouer et de régler l'équilibrage dès la
    /// première ouverture du projet, avant d'avoir dessiné quoi que ce soit.
    /// Quand l'interface définitive sera faite en uGUI ou UI Toolkit, il suffira
    /// de supprimer ce composant : il ne fait rien d'autre qu'afficher et lire
    /// des données publiques du RunManager.
    ///
    /// La direction artistique visée est décrite dans Docs/DIRECTION-ARTISTIQUE.md.
    /// </summary>
    public class HudDeSecours : MonoBehaviour
    {
        public RunManager run;

        readonly List<string> journal = new List<string>();
        Autel autelEnCours;
        PlayerAgent signataire;
        List<PactDef> offre;
        bool termine;
        FinDeDescente cause;

        GUIStyle titre, corps, petit;

        void Start()
        {
            if (run == null) run = FindObjectOfType<RunManager>();
            if (run == null) { enabled = false; return; }
            run.SurJournal += Ajouter;
            run.SurAutel += ProposerLesPactes;
            run.SurFin += Finir;
        }

        void OnDestroy()
        {
            if (run == null) return;
            run.SurJournal -= Ajouter;
            run.SurAutel -= ProposerLesPactes;
            run.SurFin -= Finir;
        }

        void Ajouter(string s)
        {
            journal.Add(s);
            if (journal.Count > 6) journal.RemoveAt(0);
        }

        void ProposerLesPactes(Autel a, PlayerAgent j, List<PactDef> pactes)
        {
            autelEnCours = a; signataire = j; offre = pactes;
            Time.timeScale = 0f;    // on arrête tout : signer un pacte est une décision, pas un réflexe
        }

        void Finir(FinDeDescente c, List<PlayerAgent> joueurs)
        {
            termine = true; cause = c;
        }

        void Update()
        {
            if (offre != null)
            {
                for (int i = 0; i < offre.Count && i < 3; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Choisir(i);
                if (Input.GetKeyDown(KeyCode.Escape)) Choisir(-1);
                return;
            }
            if (termine && Input.GetKeyDown(KeyCode.Return)) { termine = false; run.Rejouer(); }
        }

        void Choisir(int i)
        {
            if (i >= 0 && offre != null && i < offre.Count)
                run.SignerPacte(signataire, offre[i].id, autelEnCours);
            offre = null; signataire = null; autelEnCours = null;
            Time.timeScale = 1f;
        }

        // ================================================================== dessin

        void Preparer()
        {
            if (titre != null) return;
            titre = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            corps = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            petit = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            titre.normal.textColor = Palette.Os;
            corps.normal.textColor = Palette.Os;
            petit.normal.textColor = Palette.Cendre;
        }

        void OnGUI()
        {
            if (run == null) return;
            Preparer();

            if (termine)   { EcranDeFin(); return; }
            if (offre != null) { EcranDesPactes(); return; }

            // ---- bandeau du haut : le temps et le relevé
            int m = Mathf.Max(0, Mathf.FloorToInt(run.TempsRestant / 60f));
            int s = Mathf.Max(0, Mathf.FloorToInt(run.TempsRestant % 60f));
            GUI.color = run.TempsRestant < 60f ? Palette.Rouille : Palette.Os;
            GUI.Label(new Rect(Screen.width / 2f - 60f, 12f, 200f, 40f), $"{m}:{s:00}", titre);
            GUI.color = Color.white;

            string fr = run.SortieOuverte
                ? "Relevé complet — l'escalier est ouvert"
                : $"Relevé {run.FragmentsTrouves}/{run.reglages.fragmentsRequis}";
            GUI.color = run.SortieOuverte ? Palette.Ciel : Palette.Cendre;
            GUI.Label(new Rect(Screen.width / 2f - 130f, 46f, 300f, 24f), fr, corps);
            GUI.color = Color.white;

            // ---- une fiche par joueur, dans son coin
            for (int i = 0; i < run.Joueurs.Count; i++) Fiche(run.Joueurs[i], i);

            // ---- le journal, en bas à gauche
            for (int i = 0; i < journal.Count; i++)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * (i + 1) / journal.Count);
                GUI.Label(new Rect(16f, Screen.height - 26f * (journal.Count - i) - 12f, 700f, 22f),
                          journal[i], petit);
            }
            GUI.color = Color.white;

            // ---- l'invite d'action du joueur 1
            if (run.Joueurs.Count > 0 && run.Joueurs[0] != null)
            {
                string inv = Interactions.InvitePour(run.Joueurs[0], run);
                if (!string.IsNullOrEmpty(inv))
                {
                    GUI.color = Palette.Soleil;
                    GUI.Label(new Rect(Screen.width / 2f - 200f, Screen.height - 92f, 400f, 26f),
                              "[E]  " + inv, corps);
                    GUI.color = Color.white;
                }
            }
        }

        void Fiche(PlayerAgent j, int i)
        {
            if (j == null) return;
            float w = 226f, h = 84f, marge = 14f;
            float x = (i % 2 == 0) ? marge : Screen.width - w - marge;
            float y = (i < 2) ? marge : Screen.height - h - marge - 30f;
            var r = new Rect(x, y, w, h);

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.color = j.couleur;
            GUI.Label(new Rect(r.x + 8f, r.y + 4f, w, 20f), j.nom, corps);
            GUI.color = Color.white;

            string etat = j.etat == EtatJoueur.ATerre ? "à terre"
                        : j.etat == EtatJoueur.Mort ? "mort"
                        : j.etat == EtatJoueur.Sorti ? "remonté" : "";
            if (etat != "")
            {
                GUI.color = j.etat == EtatJoueur.Sorti ? Palette.Feuille : Palette.Rouille;
                GUI.Label(new Rect(r.x + 60f, r.y + 5f, w, 20f), etat, petit);
                GUI.color = Color.white;
            }

            // vitalité
            Jauge(new Rect(r.x + 8f, r.y + 28f, w - 16f, 8f), j.pv / j.pvMax,
                  Palette.Vitalite(j.pv / j.pvMax));

            // charge : au-delà du seuil, la jauge vire au rouge — c'est le mur
            float taux = j.TauxCharge;
            Jauge(new Rect(r.x + 8f, r.y + 42f, w - 16f, 8f), Mathf.Clamp01(taux),
                  taux > run.reglages.seuilBlocage ? Palette.Rouille : Palette.Soleil);

            GUI.color = Palette.Cendre;
            GUI.Label(new Rect(r.x + 8f, r.y + 52f, w, 18f),
                      $"{j.valeur} pièces · {j.charge}/{j.Capacite}", petit);
            if (!string.IsNullOrEmpty(j.pacte))
            {
                var d = run.reglages.Pacte(j.pacte);
                GUI.color = Palette.Flamme;
                string reste = j.pacteRestant > 0f ? $" ({Mathf.CeilToInt(j.pacteRestant)} s)" : "";
                GUI.Label(new Rect(r.x + 8f, r.y + 66f, w - 16f, 18f),
                          (d != null ? d.resume : j.pacte) + reste, petit);
            }
            if (j.etat == EtatJoueur.ATerre)
            {
                GUI.color = Palette.Ciel;
                Jauge(new Rect(r.x + 8f, r.y + 68f, w - 16f, 6f),
                      j.progressionRelevage / run.reglages.dureeRelevage, Palette.Ciel);
            }
            GUI.color = Color.white;
        }

        static void Jauge(Rect r, float t, Color c)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.14f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void EcranDesPactes()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(Screen.width / 2f - 280f, 70f, 620f, 40f),
                      $"{signataire.nom} — l'autel écoute", titre);
            GUI.color = Palette.Cendre;
            GUI.Label(new Rect(Screen.width / 2f - 280f, 108f, 620f, 24f),
                      "Chaque avantage est un emprunt. Quelqu'un rembourse.", corps);
            GUI.color = Color.white;

            for (int i = 0; i < offre.Count; i++)
            {
                var p = offre[i];
                float w = 300f, x = Screen.width / 2f - (offre.Count * w) / 2f + i * w;
                var r = new Rect(x + 8f, 168f, w - 16f, 250f);

                GUI.color = new Color(1f, 1f, 1f, 0.06f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = Palette.Flamme;
                GUI.Label(new Rect(r.x + 14f, r.y + 12f, r.width - 28f, 30f), $"[{i + 1}]  {p.nom}", corps);
                GUI.color = Palette.Feuille;
                GUI.Label(new Rect(r.x + 14f, r.y + 52f, r.width - 28f, 80f), p.gain, petit);
                GUI.color = Palette.Rouille;
                GUI.Label(new Rect(r.x + 14f, r.y + 138f, r.width - 28f, 90f), p.prix, petit);
                GUI.color = Color.white;
            }

            GUI.color = Palette.Cendre;
            GUI.Label(new Rect(Screen.width / 2f - 180f, 440f, 400f, 24f),
                      "1, 2, 3 pour signer  ·  Échap pour repartir les mains libres", petit);
            GUI.color = Color.white;
        }

        void EcranDeFin()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string t = cause == FinDeDescente.Sortie ? "Remontés"
                     : cause == FinDeDescente.TempsEcoule ? "Le temps a manqué"
                     : "Le donjon garde tout";
            GUI.Label(new Rect(Screen.width / 2f - 240f, 110f, 520f, 44f), t, titre);

            float y = 175f;
            foreach (var j in run.Joueurs)
            {
                if (j == null) continue;
                GUI.color = j.couleur;
                GUI.Label(new Rect(Screen.width / 2f - 240f, y, 260f, 24f), j.nom, corps);
                GUI.color = j.gagne > 0 ? Palette.Soleil : Palette.Cendre;
                GUI.Label(new Rect(Screen.width / 2f - 20f, y, 260f, 24f),
                          j.gagne > 0 ? $"{j.gagne} pièces remontées" : "rien remonté", corps);
                y += 30f;
            }
            GUI.color = Palette.Soleil;
            GUI.Label(new Rect(Screen.width / 2f - 240f, y + 20f, 520f, 26f),
                      $"Bourse : {Boutique.Or} pièces", corps);
            GUI.color = Palette.Cendre;
            GUI.Label(new Rect(Screen.width / 2f - 240f, y + 58f, 520f, 24f), "Entrée pour redescendre", petit);
            GUI.color = Color.white;
        }
    }
}

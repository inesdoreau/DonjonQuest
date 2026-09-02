using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    public enum FinDeDescente { Sortie, Mort, TempsEcoule, Abandon }

    /// <summary>
    /// Le chef d'orchestre d'une descente : le temps, les fragments, les pactes,
    /// la fin et le compte final.
    ///
    /// Une seule idée gouverne tout ce fichier : « chaque avantage est un
    /// emprunt, et c'est le groupe qui rembourse ». Concrètement, aucun bonus
    /// n'est appliqué ici sans que son prix ne soit appliqué dans la même
    /// méthode. Si vous ajoutez un pacte et que vous ne savez pas quoi écrire
    /// dans sa clause de prix, c'est que ce n'est pas un pacte.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Références")]
        public GameTuning reglages;
        public DungeonBuilder batisseur;
        public PartyCamera camera3e;
        public PlayerInputRouter entrees;

        [Header("Partie")]
        public int nombreDeJoueursSouhaite = 1;
        public bool graineDuJour = true;
        public int graine;

        // ------------------------------------------------------------------ état public
        public List<PlayerAgent> Joueurs { get; private set; } = new List<PlayerAgent>();
        public List<SkeletonAI> Squelettes { get; private set; } = new List<SkeletonAI>();
        public List<Interactif> Interactifs { get; private set; } = new List<Interactif>();
        public MazeGenerator Labyrinthe { get; private set; }

        public bool EnCours { get; private set; }
        public float Horloge { get; private set; }        // secondes depuis le départ
        public float TempsRestant { get; private set; }
        public int FragmentsTrouves { get; private set; }
        public bool SortieOuverte => FragmentsTrouves >= reglages.fragmentsRequis;
        public int NombreDeJoueurs => Joueurs.Count;
        public bool AubeSignee { get; private set; }

        /// <summary>Le gisement écoute de mieux en mieux. 1 au départ, 1,9 au bout.</summary>
        public float Eveil
        {
            get
            {
                float m = Horloge / 60f;
                float e = 1f + Mathf.Min(reglages.eveilMax, m * reglages.eveilParMinute);
                if (mecheSonnee) e += 0.5f;      // la mèche a brûlé : tout le monde est debout
                return e;
            }
        }

        /// <summary>Le journal de bord affiché en bas de l'écran. Branchez l'UI dessus.</summary>
        public event Action<string> SurJournal;
        public event Action<FinDeDescente, List<PlayerAgent>> SurFin;
        public event Action<Autel, PlayerAgent, List<PactDef>> SurAutel;

        // ------------------------------------------------------------------ interne
        struct FinDifferee { public FinDeDescente cause; public float quand; public bool armee; }
        FinDifferee fin;
        int mecheCharges;
        bool mecheSonnee;
        float extinctionRestante;      // pacte de la lampe : le labyrinthe est éteint
        readonly List<Light> torches = new List<Light>();

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (reglages == null)
            {
                Debug.LogError("[Donjon Deals] Aucun GameTuning branché sur le RunManager. " +
                               "Assets ▸ Create ▸ Donjon Deals ▸ Réglages, puis glissez-le ici.");
                enabled = false;
                return;
            }
            Commencer();
        }

        // ================================================================== départ

        public void Commencer()
        {
            graine = graineDuJour ? GameTuning.GraineDuJour() : (graine == 0 ? Environment.TickCount : graine);

            Labyrinthe = new MazeGenerator();
            Labyrinthe.Generate(graine);
            if (!Labyrinthe.Validate(out string probleme))
            {
                // ceci ne doit jamais arriver : le générateur est vérifié sur quarante
                // graines. Si ça arrive quand même, on préfère une autre graine à un
                // labyrinthe injouable.
                Debug.LogWarning($"[Donjon Deals] graine {graine} rejetée ({probleme}), on retire.");
                graine++;
                Labyrinthe.Generate(graine);
            }

            torches.Clear();
            Interactifs.Clear();
            Squelettes.Clear();

            batisseur.Construire(Labyrinthe, this);
            Joueurs = batisseur.CreerJoueurs(Mathf.Clamp(nombreDeJoueursSouhaite, 1, 4), Labyrinthe.Entrance);
            if (entrees != null) entrees.joueurs = Joueurs.ToArray();
            if (camera3e != null)
            {
                camera3e.reglages = reglages;
                camera3e.joueurs = Joueurs;
                camera3e.Commencer(MazeGenerator.World(Labyrinthe.Entrance.x, Labyrinthe.Entrance.y));
            }

            Horloge = 0f;
            TempsRestant = reglages.dureeSecondes;
            FragmentsTrouves = 0;
            AubeSignee = false;
            mecheCharges = 0; mecheSonnee = false;
            extinctionRestante = 0f;
            fin = default;
            EnCours = true;

            Journal(Joueurs.Count > 1
                ? "Descente. Quatre fragments de relevé ouvrent l'escalier."
                : "Descente en solitaire. Quatre fragments de relevé ouvrent l'escalier.");
        }

        public void EnregistrerTorche(Light l) { if (l != null) torches.Add(l); }
        public void EnregistrerSquelette(SkeletonAI s) { if (s != null) Squelettes.Add(s); }

        // ================================================================== boucle

        void Update()
        {
            if (!EnCours) return;
            float dt = Time.deltaTime;
            Horloge += dt;
            TempsRestant -= dt;

            if (extinctionRestante > 0f)
            {
                extinctionRestante -= dt;
                if (extinctionRestante <= 0f) RallumerLesTorches();
            }

            // le relevage : par maintien, et par quelqu'un d'autre. C'est la
            // seule mécanique du jeu où aider ne coûte que du temps — il fallait
            // au moins une action gratuite, sinon plus personne ne s'entraide.
            foreach (var j in Joueurs)
            {
                if (j == null || j.etat != EtatJoueur.Vivant || !j.MaintientInteraction) continue;
                var a = Interactions.Aterre(j, this);
                if (a != null) a.Relever(j, dt);
            }

            if (TempsRestant <= 0f && !fin.armee) DemanderFin(FinDeDescente.TempsEcoule, 0.2f);

            if (fin.armee && Horloge >= fin.quand) Terminer(fin.cause);
        }

        // ================================================================== butin

        public void PoserButinAuSol(string lootId, Vector3 position)
        {
            batisseur.PoserButin(lootId, position);
        }

        /// <summary>
        /// Quand on tombe, on lâche tout. Le sac se déverse là où on est tombé :
        /// pour l'équipe, un camarade à terre est aussi un tas d'or à récupérer,
        /// et c'est exactement le dilemme qu'on veut voir apparaître.
        /// </summary>
        public void DeposerBesace(PlayerAgent j)
        {
            int n = j.besace.Count;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)Mathf.Max(1, n) * Mathf.PI * 2f;
                float r = 1f + (i % 3) * 0.5f;
                PoserButinAuSol(j.besace[i],
                    j.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r);
            }
            j.besace.Clear();
            j.charge = 0;
            j.valeur = 0;
            if (n > 0) Journal($"{j.nom} répand {n} pièces sur le sol.");
        }

        public void FragmentTrouve()
        {
            FragmentsTrouves++;
            if (SortieOuverte) Journal("Le relevé est complet. L'escalier est ouvert.");
            else Journal($"Fragment de relevé — {FragmentsTrouves}/{reglages.fragmentsRequis}.");
        }

        // ================================================================== pactes

        public PlayerAgent PacteActifChezUnAutre(string id, PlayerAgent sauf)
        {
            foreach (var j in Joueurs)
            {
                if (j == null || j == sauf || j.pacte != id) continue;
                var def = reglages.Pacte(id);
                if (def != null && def.duree > 0f && j.pacteRestant <= 0f) continue;
                return j;
            }
            return null;
        }

        /// <summary>Trois pactes tirés au sort, un par famille quand c'est possible.</summary>
        public void OuvrirLAutel(Autel autel, PlayerAgent j)
        {
            var offre = new List<PactDef>();
            var dispo = new List<PactDef>();
            foreach (var p in reglages.pactes)
            {
                if (p.groupe && Joueurs.Count < 2) continue;      // sans témoin, pas de discorde
                if (p.id == "miroir" && AucunPacteSigne(j)) continue;
                dispo.Add(p);
            }
            foreach (Famille f in Enum.GetValues(typeof(Famille)))
            {
                var dansFamille = dispo.FindAll(p => p.famille == f);
                if (dansFamille.Count == 0) continue;
                offre.Add(dansFamille[UnityEngine.Random.Range(0, dansFamille.Count)]);
            }
            while (offre.Count < 3 && dispo.Count > offre.Count)
            {
                var p = dispo[UnityEngine.Random.Range(0, dispo.Count)];
                if (!offre.Contains(p)) offre.Add(p);
            }
            SurAutel?.Invoke(autel, j, offre);
        }

        bool AucunPacteSigne(PlayerAgent sauf)
        {
            foreach (var a in Joueurs)
                if (a != sauf && !string.IsNullOrEmpty(a.pacte)) return false;
            return true;
        }

        public void SignerPacte(PlayerAgent j, string id, Autel autel = null)
        {
            var def = reglages.Pacte(id);
            if (def == null || !string.IsNullOrEmpty(j.pacte)) return;

            j.pacte = id;
            j.pacteRestant = def.duree;
            autel?.Eteindre();

            switch (id)
            {
                case "lampe":
                    EteindreLesTorches();
                    extinctionRestante = def.duree;
                    break;

                case "aube":
                    AubeSignee = true;
                    TempsRestant = Mathf.Min(TempsRestant, 120f);
                    break;

                case "bouc":
                    j.bouc = UnAutreAuHasard(j);
                    if (j.bouc != null) Journal($"{j.bouc.nom} paiera pour {j.nom}. Il ne l'a pas demandé.");
                    break;

                case "serment":
                    j.jure = UnAutreAuHasard(j);
                    if (j.jure != null)
                    {
                        j.jure.jurePar = j;
                        Journal($"{j.nom} et {j.jure.nom} sont liés. L'un tombe, l'autre suit.");
                    }
                    break;

                case "fardeau":
                    TransfererLaMoitie(j);
                    break;

                case "miroir":
                    VolerUnPacte(j);
                    break;
            }

            Journal($"{j.nom} signe « {def.nom} » — {def.resume}");
        }

        PlayerAgent UnAutreAuHasard(PlayerAgent sauf)
        {
            var l = Joueurs.FindAll(a => a != sauf && a.etat == EtatJoueur.Vivant);
            return l.Count == 0 ? null : l[UnityEngine.Random.Range(0, l.Count)];
        }

        void TransfererLaMoitie(PlayerAgent j)
        {
            var cible = UnAutreAuHasard(j);
            if (cible == null) return;
            int n = j.besace.Count / 2;
            for (int i = 0; i < n; i++)
            {
                string id = j.LacherLaPlusLourde(true);
                if (id == null) break;
                var d = reglages.Loot(id);
                cible.besace.Add(id);
                cible.charge += d.poids;
                cible.valeur += d.valeur;
            }
            Journal($"{cible.nom} hérite de la moitié du sac de {j.nom}. Et de l'or avec.");
        }

        void VolerUnPacte(PlayerAgent j)
        {
            foreach (var a in Joueurs)
            {
                if (a == j || string.IsNullOrEmpty(a.pacte) || a.pacte == "miroir") continue;
                string vole = a.pacte;
                float reste = a.pacteRestant;
                a.pacte = null; a.pacteRestant = 0f;
                j.pacte = vole; j.pacteRestant = reste;
                Journal($"{j.nom} arrache « {reglages.Pacte(vole).nom} » à {a.nom}.");
                return;
            }
        }

        public void AvancerLaMeche(PlayerAgent j)
        {
            if (mecheSonnee) return;
            mecheCharges++;
            if (mecheCharges < 14) return;
            mecheSonnee = true;
            Journal("La mèche a brûlé. Tout le gisement est debout.");
        }

        public void LierParLeSerment(PlayerAgent jureur, float delai)
        {
            DemanderChute(jureur, delai);
        }

        void DemanderChute(PlayerAgent j, float delai)
        {
            StartCoroutine(ChuteDiferee(j, delai));
        }

        System.Collections.IEnumerator ChuteDiferee(PlayerAgent j, float delai)
        {
            yield return new WaitForSeconds(delai);
            if (EnCours && j != null && j.etat == EtatJoueur.Vivant)
            {
                Journal($"{j.nom} tombe avec son juré. Le serment tient parole.");
                j.Tuer();
            }
        }

        void EteindreLesTorches()
        {
            foreach (var t in torches) if (t != null) t.enabled = false;
            Journal("Toutes les torches s'éteignent. Il ne reste que les yeux.");
        }
        void RallumerLesTorches()
        {
            foreach (var t in torches) if (t != null) t.enabled = true;
            Journal("Les torches reprennent.");
        }

        // ================================================================== trappe

        /// <summary>
        /// La trappe déplace, elle ne tue pas. En coopération, le saut est borné :
        /// personne ne doit finir hors de l'écran commun.
        /// </summary>
        public void FaireTomberParLaTrappe(PlayerAgent j, Vector2Int depuis)
        {
            int min = reglages.sautTrappeMin;
            int max = Joueurs.Count > 1 ? reglages.sautTrappeMaxCoop : reglages.sautTrappeMaxSolo;

            var candidates = new List<Vector2Int>();
            for (int y = 1; y < MazeGenerator.H - 1; y++)
                for (int x = 1; x < MazeGenerator.W - 1; x++)
                {
                    if (!Labyrinthe.IsFloor(x, y) || !Labyrinthe.IsDeathFree(x, y)) continue;
                    int d = Mathf.Abs(x - depuis.x) + Mathf.Abs(y - depuis.y);
                    if (d < min || d > max) continue;
                    candidates.Add(new Vector2Int(x, y));
                }
            if (candidates.Count == 0) return;

            var c = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var cc = j.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            j.transform.position = MazeGenerator.World(c.x, c.y) + Vector3.up * 0.2f;
            if (cc != null) cc.enabled = true;

            j.Blesser(reglages.degatsChute);
            Journal($"{j.nom} passe à travers le plancher.");
        }

        // ================================================================== fin

        public void Journal(string texte)
        {
            SurJournal?.Invoke(texte);
        }

        /// <summary>
        /// La fin est différée puis vérifiée dans Update, jamais posée par un
        /// simple minuteur : une fenêtre en arrière-plan gèle les minuteurs et
        /// la partie ne se terminait plus. Le bug a coûté cher, il est documenté ici.
        /// </summary>
        public void DemanderFin(FinDeDescente cause, float delai)
        {
            if (fin.armee) return;
            fin = new FinDifferee { cause = cause, quand = Horloge + delai, armee = true };
        }

        /// <summary>Reste-t-il quelqu'un capable de jouer ?</summary>
        public void VerifierFin()
        {
            if (!EnCours || fin.armee) return;

            bool quelquUnDebout = false, quelquUnATerre = false;
            foreach (var j in Joueurs)
            {
                if (j == null) continue;
                if (j.etat == EtatJoueur.Vivant) quelquUnDebout = true;
                if (j.etat == EtatJoueur.ATerre) quelquUnATerre = true;
            }
            if (quelquUnDebout) return;

            // plus personne debout : soit tout le monde est sorti, soit tout le
            // monde est au sol et il n'y a plus de main pour relever.
            DemanderFin(quelquUnATerre ? FinDeDescente.Mort : FinDeDescente.Sortie, 1.2f);
        }

        void Terminer(FinDeDescente cause)
        {
            EnCours = false;

            bool personneEnBas = true;
            foreach (var j in Joueurs)
                if (j != null && j.etat != EtatJoueur.Sorti) personneEnBas = false;

            int total = 0;
            foreach (var j in Joueurs)
            {
                if (j == null) continue;
                if (j.etat != EtatJoueur.Sorti) { j.gagne = 0; continue; }   // ce qu'on n'a pas remonté n'existe pas
                if (personneEnBas)
                    j.gagne = Mathf.RoundToInt(j.gagne * reglages.primeGroupe);
                total += j.gagne;
            }

            if (personneEnBas && Joueurs.Count > 1)
                Journal($"Personne n'est resté en bas. Prime d'équipe ×{reglages.primeGroupe:0.00}.");

            Boutique.Crediter(total);
            SurFin?.Invoke(cause, Joueurs);
        }

        public void Rejouer()
        {
            batisseur.Detruire();
            foreach (var j in Joueurs) if (j != null) Destroy(j.gameObject);
            Joueurs.Clear();
            graineDuJour = false;
            graine = Environment.TickCount;
            Commencer();
        }
    }
}

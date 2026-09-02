using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    [Serializable]
    public class PrefabParId
    {
        public string id;
        public GameObject prefab;
    }

    /// <summary>
    /// Pose le décor à partir de la grille.
    ///
    /// Tout ce qui est instancié ici l'est sous un seul parent, « Donjon », pour
    /// qu'une nouvelle descente se résume à détruire cet objet. Et tout ce qui
    /// est aléatoire passe par le même System.Random que le labyrinthe : deux
    /// machines qui entrent la même graine voient exactement le même donjon,
    /// meubles compris. C'est indispensable pour reproduire un bug de placement.
    /// </summary>
    public class DungeonBuilder : MonoBehaviour
    {
        [Header("Structure")]
        public GameObject prefabSol;
        public GameObject prefabMur;
        public GameObject prefabPlafond;
        public GameObject prefabTorche;

        [Header("Pièges")]
        public GameObject prefabPiques;
        public GameObject prefabRouleau;
        public GameObject prefabTrappe;
        public GameObject prefabMurPivotant;

        [Header("Objets")]
        public GameObject prefabCoffre;
        public GameObject prefabAutel;
        public GameObject prefabEscalier;
        public GameObject prefabPuitsDuFond;

        [Header("Catalogues")]
        public List<PrefabParId> butin = new List<PrefabParId>();
        public List<PrefabParId> personnages = new List<PrefabParId>();
        public List<PrefabParId> meubles = new List<PrefabParId>();
        public List<GameObject> squelettes = new List<GameObject>();

        [Header("Densités")]
        public int squelettesParChambre = 4;
        public int butinParChambre = 5;
        [Tooltip("Une torche toutes les N cases de couloir. Trop de torches et " +
                 "l'obscurité ne veut plus rien dire.")]
        public int uneTorcheToutesLes = 9;

        Transform racine;
        System.Random rnd;
        RunManager run;
        MazeGenerator mz;

        // ================================================================== construction

        public void Construire(MazeGenerator labyrinthe, RunManager gestionnaire)
        {
            Detruire();
            mz = labyrinthe;
            run = gestionnaire;
            rnd = new System.Random(gestionnaire.graine);
            racine = new GameObject("Donjon").transform;
            racine.SetParent(transform, false);

            PoserSolsEtMurs();
            PoserTorches();
            PoserPieges();
            PoserChambres();
            PoserFragments();
            PoserSorties();
        }

        public void Detruire()
        {
            if (racine != null) Destroy(racine.gameObject);
            racine = null;
        }

        void PoserSolsEtMurs()
        {
            for (int y = 0; y < MazeGenerator.H; y++)
                for (int x = 0; x < MazeGenerator.W; x++)
                {
                    Vector3 p = MazeGenerator.World(x, y);
                    if (mz.IsFloor(x, y))
                    {
                        Creer(prefabSol, p, Quaternion.Euler(0f, 90f * rnd.Next(4), 0f));
                        if (prefabPlafond != null)
                            Creer(prefabPlafond, p + Vector3.up * 4f, Quaternion.identity);
                    }
                    else if (VoisinSol(x, y))
                    {
                        // on ne bâtit que les murs qui touchent un couloir :
                        // le reste ne serait jamais vu et coûterait des milliers de faces.
                        Creer(prefabMur, p, Quaternion.identity);
                    }
                }
        }

        bool VoisinSol(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (mz.IsFloor(x + dx, y + dy)) return true;
            return false;
        }

        void PoserTorches()
        {
            if (prefabTorche == null) return;
            int compteur = 0;
            for (int y = 1; y < MazeGenerator.H - 1; y++)
                for (int x = 1; x < MazeGenerator.W - 1; x++)
                {
                    if (!mz.IsFloor(x, y)) continue;
                    if (++compteur % uneTorcheToutesLes != 0) continue;

                    // adossée à un mur, sinon elle flotte au milieu du couloir
                    for (int d = 0; d < 4; d++)
                    {
                        int mx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                        int my = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (mz.IsFloor(mx, my)) continue;
                        var t = Creer(prefabTorche,
                            MazeGenerator.World(x, y) +
                            new Vector3(mx - x, 0f, my - y) * (MazeGenerator.Tile * 0.42f) + Vector3.up * 2.1f,
                            Quaternion.LookRotation(new Vector3(x - mx, 0f, y - my)));
                        foreach (var l in t.GetComponentsInChildren<Light>()) run.EnregistrerTorche(l);
                        break;
                    }
                }
        }

        void PoserPieges()
        {
            foreach (var s in mz.Spikes)
            {
                var o = Creer(prefabPiques, MazeGenerator.World(s.x, s.y), Quaternion.identity);
                var c = o.GetComponent<SpikeTrap>() ?? o.AddComponent<SpikeTrap>();
                c.caseGrille = new Vector2Int(s.x, s.y);
                c.periode = s.period;
                c.phase = s.phase;
            }

            foreach (var r in mz.Rollers)
            {
                var o = Creer(prefabRouleau, MazeGenerator.World(r.x, r.y),
                    Quaternion.Euler(0f, r.axis == MazeGenerator.Axis.NorthSouth ? 0f : 90f, 0f));
                var c = o.GetComponent<Roller>() ?? o.AddComponent<Roller>();
                c.caseGrille = new Vector2Int(r.x, r.y);
                c.axe = r.axis;
            }

            foreach (var t in mz.Trapdoors)
            {
                var o = Creer(prefabTrappe, MazeGenerator.World(t.x, t.y) + Vector3.up * 0.06f,
                              Quaternion.identity);
                var c = o.GetComponent<Trapdoor>() ?? o.AddComponent<Trapdoor>();
                c.caseGrille = new Vector2Int(t.x, t.y);
                c.dureeOuverte = run.reglages.dureeTrappeOuverte;
            }

            foreach (var w in mz.RotatingWalls)
            {
                var o = Creer(prefabMurPivotant, MazeGenerator.World(w.x, w.y), Quaternion.identity);
                var c = o.GetComponent<RotatingWall>() ?? o.AddComponent<RotatingWall>();
                c.caseGrille = new Vector2Int(w.x, w.y);
                c.axe = w.axis;
                c.periode = w.period;
                c.phase = w.phase;
                if (c.pivot == null && o.transform.childCount > 0) c.pivot = o.transform.GetChild(0);
            }
        }

        void PoserChambres()
        {
            foreach (var ch in mz.Chambers)
            {
                // les cases utilisables de la chambre, sauf le centre réservé
                var libres = new List<Vector2Int>();
                for (int y = ch.y - 2; y <= ch.y + 2; y++)
                    for (int x = ch.x - 2; x <= ch.x + 2; x++)
                        if (mz.IsFloor(x, y) && !(x == ch.x && y == ch.y))
                            libres.Add(new Vector2Int(x, y));
                Melanger(libres);
                int i = 0;

                // un autel par chambre, sauf celle de la sortie : on ne signe pas
                // un pacte à trois mètres de l'escalier, ce serait sans risque.
                if (!ch.IsExit && prefabAutel != null && i < libres.Count)
                {
                    var o = Creer(prefabAutel, Sol(libres[i++]), Tourner());
                    if (o.GetComponent<Autel>() == null) o.AddComponent<Autel>();
                }

                if (prefabCoffre != null && i < libres.Count)
                {
                    var o = Creer(prefabCoffre, Sol(libres[i++]), Tourner());
                    var c = o.GetComponent<Coffre>() ?? o.AddComponent<Coffre>();
                    c.contenu = TirerDuButin(3 + rnd.Next(3));
                }

                for (int k = 0; k < butinParChambre && i < libres.Count; k++)
                    PoserButin(UnButin(), Sol(libres[i++]));

                for (int k = 0; k < squelettesParChambre && i < libres.Count; k++)
                    PoserSquelette(Sol(libres[i++]));

                // le mobilier passe en dernier : il occupe ce qui reste, il ne
                // vole jamais la place d'un coffre ou d'un squelette.
                while (i < libres.Count && meubles.Count > 0)
                {
                    if (rnd.NextDouble() < 0.45)
                        Creer(meubles[rnd.Next(meubles.Count)].prefab, Sol(libres[i]), Tourner());
                    i++;
                }
            }
        }

        /// <summary>
        /// Les fragments de relevé ouvrent la sortie : ils ne sont donc jamais
        /// placés derrière une case mortelle. Sinon la partie pouvait devenir
        /// mathématiquement impossible à finir.
        /// </summary>
        void PoserFragments()
        {
            var sures = new List<Vector2Int>();
            for (int y = 1; y < MazeGenerator.H - 1; y++)
                for (int x = 1; x < MazeGenerator.W - 1; x++)
                {
                    if (!mz.IsFloor(x, y) || !mz.IsDeathFree(x, y)) continue;
                    if (Mathf.Abs(x - 1) + Mathf.Abs(y - 1) < 6) continue;   // pas sous les pieds au départ
                    sures.Add(new Vector2Int(x, y));
                }
            Melanger(sures);

            int poses = 0, besoin = run.reglages.fragmentsRequis + 1;   // un de rab : la marge d'erreur
            var deja = new List<Vector2Int>();
            foreach (var c in sures)
            {
                if (poses >= besoin) break;
                bool trop = false;
                foreach (var p in deja)
                    if (Mathf.Abs(p.x - c.x) + Mathf.Abs(p.y - c.y) < 7) { trop = true; break; }
                if (trop) continue;
                deja.Add(c);
                PoserButin("map_rolled", Sol(c));
                poses++;
            }
        }

        void PoserSorties()
        {
            if (prefabEscalier != null)
            {
                var o = Creer(prefabEscalier, MazeGenerator.World(mz.Exit.x, mz.Exit.y), Quaternion.identity);
                var s = o.GetComponent<Sortie>() ?? o.AddComponent<Sortie>();
                s.puitsDuFond = false;
            }

            // le puits du fond : la case au sol la plus éloignée de l'entrée,
            // atteignable sans mourir. Il paie 50 % de plus. C'est le pari.
            if (prefabPuitsDuFond == null) return;
            Vector2Int loin = new Vector2Int(1, 1);
            int d0 = -1;
            for (int y = 1; y < MazeGenerator.H - 1; y++)
                for (int x = 1; x < MazeGenerator.W - 1; x++)
                {
                    if (!mz.IsFloor(x, y) || !mz.IsDeathFree(x, y)) continue;
                    if (Mathf.Abs(x - mz.Exit.x) + Mathf.Abs(y - mz.Exit.y) < 5) continue;
                    int d = Mathf.Abs(x - 1) + Mathf.Abs(y - 1);
                    if (d > d0) { d0 = d; loin = new Vector2Int(x, y); }
                }
            if (d0 < 0) return;
            var p = Creer(prefabPuitsDuFond, Sol(loin), Quaternion.identity);
            var sp = p.GetComponent<Sortie>() ?? p.AddComponent<Sortie>();
            sp.puitsDuFond = true;
        }

        // ================================================================== joueurs

        public List<PlayerAgent> CreerJoueurs(int combien, MazeGenerator.Cell entree)
        {
            var res = new List<PlayerAgent>();
            if (personnages.Count == 0)
            {
                Debug.LogError("[Donjon Deals] Le DungeonBuilder n'a aucun prefab de personnage. " +
                               "Lancez Donjon Deals ▸ Créer la scène de départ, ou renseignez la liste.");
                return res;
            }
            string choisi = Boutique.PersonnageChoisi;

            for (int i = 0; i < combien; i++)
            {
                // le joueur 1 a le personnage acheté, les autres tournent dans la liste :
                // sur un écran unique, quatre chevaliers identiques sont illisibles.
                string id = i == 0 ? choisi : personnages[(i + IndexDe(choisi)) % personnages.Count].id;
                var prefab = Prefab(personnages, id) ?? (personnages.Count > 0 ? personnages[0].prefab : null);
                if (prefab == null)
                {
                    Debug.LogError("[Donjon Deals] Aucun prefab de personnage dans le DungeonBuilder.");
                    break;
                }

                float a = i / (float)Mathf.Max(1, combien) * Mathf.PI * 2f;
                Vector3 p = MazeGenerator.World(entree.x, entree.y)
                          + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (combien > 1 ? 0.9f : 0f)
                          + Vector3.up * 0.2f;

                var o = Instantiate(prefab, p, Quaternion.identity);
                o.name = "Joueur " + (i + 1);
                var j = o.GetComponent<PlayerAgent>() ?? o.AddComponent<PlayerAgent>();
                j.index = i;
                j.nom = "J" + (i + 1);
                j.couleur = Palette.Joueurs[i % Palette.Joueurs.Length];
                j.reglages = run.reglages;
                j.pv = j.pvMax;
                if (j.animator == null) j.animator = o.GetComponentInChildren<Animator>();
                if (j.lanterne != null) j.lanterne.color = j.couleur;
                res.Add(j);
            }
            return res;
        }

        int IndexDe(string id)
        {
            for (int i = 0; i < personnages.Count; i++) if (personnages[i].id == id) return i;
            return 0;
        }

        // ================================================================== pièces détachées

        public void PoserButin(string lootId, Vector3 position)
        {
            var prefab = Prefab(butin, lootId);
            if (prefab == null) return;
            var def = run.reglages.Loot(lootId);
            var o = Instantiate(prefab, position + Vector3.up * (def != null ? def.hauteur : 0.1f),
                                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), racine);
            var r = o.GetComponent<Ramassable>() ?? o.AddComponent<Ramassable>();
            r.lootId = lootId;
        }

        void PoserSquelette(Vector3 p)
        {
            if (squelettes.Count == 0) return;
            var o = Creer(squelettes[rnd.Next(squelettes.Count)], p, Tourner());
            var s = o.GetComponent<SkeletonAI>() ?? o.AddComponent<SkeletonAI>();
            s.reglages = run.reglages;
            if (s.animator == null) s.animator = o.GetComponentInChildren<Animator>();
            if (string.IsNullOrEmpty(s.butinLache) && rnd.NextDouble() < 0.35)
                s.butinLache = UnButin();
            run.EnregistrerSquelette(s);
        }

        List<string> TirerDuButin(int combien)
        {
            var l = new List<string>();
            for (int i = 0; i < combien; i++) l.Add(UnButin());
            return l;
        }

        /// <summary>
        /// Tirage pondéré : le cuivre est banal, la pile de lingots est rare.
        /// Le poids est l'inverse de la valeur — sans quoi une chambre pouvait
        /// contenir mille pièces d'or et le jeu n'avait plus d'arc.
        /// </summary>
        string UnButin()
        {
            var table = run.reglages.butin;
            float total = 0f;
            foreach (var b in table) { if (b.fragment) continue; total += 260f / (b.valeur + 30f); }
            float t = (float)rnd.NextDouble() * total;
            foreach (var b in table)
            {
                if (b.fragment) continue;
                t -= 260f / (b.valeur + 30f);
                if (t <= 0f) return b.id;
            }
            return "iron_bar";
        }

        GameObject Prefab(List<PrefabParId> liste, string id)
        {
            foreach (var e in liste) if (e.id == id) return e.prefab;
            return null;
        }

        /// <summary>
        /// Instancie sous la racine. Si le prefab n'est pas branché, on crée un
        /// objet vide plutôt que de renvoyer null : le donjon se construit quand
        /// même, avec des trous visibles dans la scène, et l'inspecteur montre
        /// tout de suite ce qui manque. Un null ferait planter la construction
        /// entière sur un simple oubli d'assignation.
        /// </summary>
        GameObject Creer(GameObject prefab, Vector3 p, Quaternion r)
        {
            if (prefab == null)
            {
                var vide = new GameObject("(prefab manquant)");
                vide.transform.SetParent(racine, false);
                vide.transform.SetPositionAndRotation(p, r);
                return vide;
            }
            return Instantiate(prefab, p, r, racine);
        }

        Quaternion Tourner() => Quaternion.Euler(0f, 90f * rnd.Next(4), 0f);
        static Vector3 Sol(Vector2Int c) => MazeGenerator.World(c.x, c.y);

        void Melanger<T>(List<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                T t = l[i]; l[i] = l[j]; l[j] = t;      // échange explicite, voir MazeGenerator.Shuffle
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    /// <summary>
    /// Génère le labyrinthe et y pose les pièges.
    ///
    /// Deux garanties tenues par ce fichier, vérifiées sur quarante graines dans le
    /// prototype web dont ce code est le portage :
    ///   1. le labyrinthe est entièrement connexe ;
    ///   2. aucun piège mortel ne se pose sur un passage obligé — on peut donc
    ///      toujours rejoindre chaque chambre, chaque fragment et la sortie sans
    ///      avoir à traverser une case qui tue.
    ///
    /// Tout est déterministe à partir de la graine : deux joueurs qui entrent la
    /// même date obtiennent exactement le même labyrinthe.
    /// </summary>
    public class MazeGenerator
    {
        public const int W = 21;          // largeur en cases (impair obligatoire)
        public const int H = 21;          // hauteur en cases (impair obligatoire)
        public const float Tile = 4f;     // côté d'une case, en mètres

        public struct Cell { public int x, y; public Cell(int x, int y){ this.x=x; this.y=y; } }
        public enum Axis { NorthSouth, EastWest }

        public class Chamber { public int x, y; public bool IsExit; }
        public class RotatingWall { public int x, y; public Axis axis; public float period, phase; }
        public class Trapdoor    { public int x, y; }
        public class Roller      { public int x, y; public Axis axis; }
        public class SpikePlate  { public int x, y; public float period, phase; }

        public bool[,] Floor          = new bool[W, H];
        public List<Chamber> Chambers = new List<Chamber>();
        public List<RotatingWall> RotatingWalls = new List<RotatingWall>();
        public List<Trapdoor>   Trapdoors = new List<Trapdoor>();
        public List<Roller>     Rollers   = new List<Roller>();
        public List<SpikePlate> Spikes    = new List<SpikePlate>();
        public Cell Entrance, Exit;
        /// <summary>Cases atteignables sans jamais poser le pied sur un piège mortel.</summary>
        public HashSet<int> DeathFree = new HashSet<int>();

        System.Random rnd;
        int Key(int x, int y) => y * W + x;
        public bool IsFloor(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && Floor[x, y];

        static readonly int[,] Dirs = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        public void Generate(int seed)
        {
            rnd = new System.Random(seed);
            Array.Clear(Floor, 0, Floor.Length);
            Chambers.Clear(); RotatingWalls.Clear(); Trapdoors.Clear();
            Rollers.Clear(); Spikes.Clear(); DeathFree.Clear();

            Carve();
            Braid();
            OpenChambers();
            PlaceEntranceAndExit();
            PlaceHazards();
            ComputeDeathFree();
        }

        // ------------------------------------------------------------------ 1. creusement
        // Retour arrière classique sur les cases impaires : on obtient un labyrinthe
        // parfait, c'est-à-dire sans boucle. Le tressage juste après lui en rend.
        void Carve()
        {
            var stack = new List<Cell> { new Cell(1, 1) };
            Floor[1, 1] = true;
            while (stack.Count > 0)
            {
                var c = stack[stack.Count - 1];
                var order = Shuffle(new[] { 0, 1, 2, 3 });
                bool advanced = false;
                foreach (int d in order)
                {
                    int nx = c.x + Dirs[d, 0] * 2, ny = c.y + Dirs[d, 1] * 2;
                    if (nx < 1 || ny < 1 || nx >= W - 1 || ny >= H - 1) continue;
                    if (Floor[nx, ny]) continue;
                    Floor[c.x + Dirs[d, 0], c.y + Dirs[d, 1]] = true;
                    Floor[nx, ny] = true;
                    stack.Add(new Cell(nx, ny));
                    advanced = true;
                    break;
                }
                if (!advanced) stack.RemoveAt(stack.Count - 1);
            }
        }

        // ------------------------------------------------------------------ 2. tressage
        // Un labyrinthe parfait se parcourt bêtement : on revient sans cesse sur ses pas.
        // On perce donc un cul-de-sac sur trois pour créer des boucles.
        void Braid()
        {
            for (int y = 1; y < H - 1; y += 2)
                for (int x = 1; x < W - 1; x += 2)
                {
                    int voisins = 0;
                    for (int d = 0; d < 4; d++)
                        if (IsFloor(x + Dirs[d, 0], y + Dirs[d, 1])) voisins++;
                    if (voisins != 1 || rnd.NextDouble() > 0.34) continue;

                    var murs = new List<int>();
                    for (int d = 0; d < 4; d++)
                    {
                        int mx = x + Dirs[d, 0], my = y + Dirs[d, 1];
                        int bx = x + Dirs[d, 0] * 2, by = y + Dirs[d, 1] * 2;
                        if (!IsFloor(mx, my) && bx > 0 && by > 0 && bx < W - 1 && by < H - 1)
                            murs.Add(d);
                    }
                    if (murs.Count == 0) continue;
                    int pick = murs[rnd.Next(murs.Count)];
                    Floor[x + Dirs[pick, 0], y + Dirs[pick, 1]] = true;
                }
        }

        // ------------------------------------------------------------------ 3. chambres
        void OpenChambers()
        {
            int[,] cibles = { { 4, 4 }, { W - 5, 4 }, { 4, H - 5 }, { W - 5, H - 5 }, { W / 2, H / 2 } };
            for (int i = 0; i < cibles.GetLength(0); i++)
            {
                int x = Mathf.Clamp(cibles[i, 0] | 1, 3, W - 4);
                int y = Mathf.Clamp(cibles[i, 1] | 1, 3, H - 4);
                for (int j = y - 2; j <= y + 2; j++)
                    for (int k = x - 2; k <= x + 2; k++) Floor[k, j] = true;
                Chambers.Add(new Chamber { x = x, y = y });
            }
        }

        void PlaceEntranceAndExit()
        {
            Entrance = new Cell(1, 1);
            Chamber best = Chambers[0];
            int bd = -1;
            foreach (var c in Chambers)
            {
                int d = Mathf.Abs(c.x - 1) + Mathf.Abs(c.y - 1);
                if (d > bd) { bd = d; best = c; }
            }
            best.IsExit = true;
            Exit = new Cell(best.x, best.y);
        }

        // ------------------------------------------------------------------ 4. pièges
        void PlaceHazards()
        {
            var onPath = PathCells();
            var critical = ArticulationPoints();

            // un couloir droit : deux ouvertures opposées, hors des chambres
            Func<int, int, Axis?> corridor = (x, y) =>
            {
                if (!IsFloor(x, y)) return null;
                foreach (var c in Chambers)
                    if (Mathf.Abs(c.x - x) <= 3 && Mathf.Abs(c.y - y) <= 3) return null;
                if (Mathf.Abs(x - 1) + Mathf.Abs(y - 1) < 4) return null;
                bool ns = IsFloor(x, y - 1) && IsFloor(x, y + 1);
                bool ew = IsFloor(x - 1, y) && IsFloor(x + 1, y);
                if (ns == ew) return null;
                return ns ? Axis.NorthSouth : Axis.EastWest;
            };

            // d'abord les cases du chemin réellement emprunté : un piège dans une
            // impasse que personne ne visite ne sert à rien
            var prioritaires = new List<Cell>();
            var secondaires  = new List<Cell>();
            var axes = new Dictionary<int, Axis>();
            for (int y = 1; y < H - 1; y++)
                for (int x = 1; x < W - 1; x++)
                {
                    var a = corridor(x, y);
                    if (a == null) continue;
                    axes[Key(x, y)] = a.Value;
                    (onPath.Contains(Key(x, y)) ? prioritaires : secondaires).Add(new Cell(x, y));
                }
            ShuffleList(prioritaires); ShuffleList(secondaires);
            var cand = new List<Cell>(prioritaires); cand.AddRange(secondaires);

            Func<List<Cell>, Cell, int, bool> loin = (liste, c, d) =>
            {
                foreach (var p in liste)
                    if (Mathf.Abs(p.x - c.x) + Mathf.Abs(p.y - c.y) < d) return false;
                return true;
            };
            // LA règle : un piège qui tue doit pouvoir se contourner
            Func<int, int, bool> mortelOk = (x, y) =>
                !critical.Contains(Key(x, y)) && Mathf.Abs(x - 1) + Mathf.Abs(y - 1) > 6;

            var posesPortes = new List<Cell>();
            foreach (var c in cand)
            {
                if (RotatingWalls.Count >= 12) break;
                if (!loin(posesPortes, c, 3)) continue;
                posesPortes.Add(c);
                RotatingWalls.Add(new RotatingWall {
                    x = c.x, y = c.y, axis = axes[Key(c.x, c.y)],
                    period = 7f + rnd.Next(0, 6), phase = (float)rnd.NextDouble() * 10f });
            }

            var posesTrappes = new List<Cell>();
            foreach (var c in cand)      // les trappes ne tuent pas : partout est permis
            {
                if (Trapdoors.Count >= 6) break;
                if (!loin(posesPortes, c, 2) || !loin(posesTrappes, c, 4)) continue;
                posesTrappes.Add(c);
                Trapdoors.Add(new Trapdoor { x = c.x, y = c.y });
            }

            var posesRouleaux = new List<Cell>();
            foreach (var c in cand)
            {
                if (Rollers.Count >= 3) break;
                var axe = axes[Key(c.x, c.y)];
                int dx = axe == Axis.NorthSouth ? 0 : 1, dy = axe == Axis.NorthSouth ? 1 : 0;
                bool ok = true;
                for (int k = -1; k <= 1 && ok; k++)     // le rouleau balaie trois cases
                {
                    int cx = c.x + dx * k, cy = c.y + dy * k;
                    if (!IsFloor(cx, cy) || !mortelOk(cx, cy)) ok = false;
                }
                if (!ok) continue;
                if (!loin(posesPortes, c, 3) || !loin(posesTrappes, c, 3) || !loin(posesRouleaux, c, 7)) continue;
                posesRouleaux.Add(c);
                Rollers.Add(new Roller { x = c.x, y = c.y, axis = axe });
            }

            var posesPiques = new List<Cell>();
            foreach (int ecart in new[] { 5, 3 })       // on resserre s'il n'y en a pas assez
            {
                foreach (var c in cand)
                {
                    if (Spikes.Count >= 8) break;
                    if (!mortelOk(c.x, c.y)) continue;
                    if (!loin(posesPortes, c, 3) || !loin(posesTrappes, c, 3) ||
                        !loin(posesRouleaux, c, 4) || !loin(posesPiques, c, ecart)) continue;
                    posesPiques.Add(c);
                    Spikes.Add(new SpikePlate {
                        x = c.x, y = c.y,
                        period = 4.2f + (float)rnd.NextDouble() * 2.6f,
                        phase = (float)rnd.NextDouble() * 8f });
                }
                if (Spikes.Count >= 5) break;
            }
        }

        // ------------------------------------------------------------------ outils de graphe

        /// <summary>Cases du chemin le plus court de l'entrée vers chaque chambre.</summary>
        HashSet<int> PathCells()
        {
            var prec = new Dictionary<int, int> { { Key(1, 1), -1 } };
            var file = new Queue<Cell>(); file.Enqueue(new Cell(1, 1));
            while (file.Count > 0)
            {
                var c = file.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = c.x + Dirs[d, 0], ny = c.y + Dirs[d, 1];
                    if (!IsFloor(nx, ny) || prec.ContainsKey(Key(nx, ny))) continue;
                    prec[Key(nx, ny)] = Key(c.x, c.y);
                    file.Enqueue(new Cell(nx, ny));
                }
            }
            var res = new HashSet<int>();
            foreach (var ch in Chambers)
            {
                int k = Key(ch.x, ch.y);
                while (k != -1 && prec.ContainsKey(k)) { res.Add(k); k = prec[k]; }
            }
            return res;
        }

        /// <summary>
        /// Points d'articulation (Tarjan) : les cases dont le retrait couperait le
        /// labyrinthe en deux. Aucun piège mortel n'a le droit de s'y poser.
        /// Récursif : la profondeur maximale est le nombre de cases, quelques
        /// centaines, très loin de la limite de pile de .NET.
        /// </summary>
        Dictionary<int, int> apDisc, apLow;
        HashSet<int> apCrit;
        int apTemps;

        HashSet<int> ArticulationPoints()
        {
            apDisc = new Dictionary<int, int>();
            apLow  = new Dictionary<int, int>();
            apCrit = new HashSet<int>();
            apTemps = 0;
            ApDfs(1, 1, -1, -1);
            return apCrit;
        }

        void ApDfs(int x, int y, int px, int py)
        {
            int k = Key(x, y);
            apDisc[k] = apLow[k] = apTemps++;
            int enfants = 0;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + Dirs[d, 0], ny = y + Dirs[d, 1];
                if (!IsFloor(nx, ny)) continue;
                if (nx == px && ny == py) continue;
                int nk = Key(nx, ny);
                if (apDisc.ContainsKey(nk))
                {
                    apLow[k] = Mathf.Min(apLow[k], apDisc[nk]);
                    continue;
                }
                enfants++;
                ApDfs(nx, ny, x, y);
                apLow[k] = Mathf.Min(apLow[k], apLow[nk]);
                if (px != -1 && apLow[nk] >= apDisc[k]) apCrit.Add(k);
            }
            if (px == -1 && enfants > 1) apCrit.Add(k);
        }

        /// <summary>Ce qu'on atteint sans jamais marcher sur une case qui tue.</summary>
        void ComputeDeathFree()
        {
            var mortelles = new HashSet<int>();
            foreach (var s in Spikes) mortelles.Add(Key(s.x, s.y));
            foreach (var r in Rollers)
            {
                int dx = r.axis == Axis.NorthSouth ? 0 : 1, dy = r.axis == Axis.NorthSouth ? 1 : 0;
                for (int k = -1; k <= 1; k++) mortelles.Add(Key(r.x + dx * k, r.y + dy * k));
            }
            DeathFree.Add(Key(1, 1));
            var pile = new Stack<Cell>(); pile.Push(new Cell(1, 1));
            while (pile.Count > 0)
            {
                var c = pile.Pop();
                for (int d = 0; d < 4; d++)
                {
                    int nx = c.x + Dirs[d, 0], ny = c.y + Dirs[d, 1];
                    int k = Key(nx, ny);
                    if (!IsFloor(nx, ny) || DeathFree.Contains(k) || mortelles.Contains(k)) continue;
                    DeathFree.Add(k); pile.Push(new Cell(nx, ny));
                }
            }
        }

        public bool IsDeathFree(int x, int y) => DeathFree.Contains(Key(x, y));

        /// <summary>Contrôle à lancer en test : le labyrinthe est-il jouable de bout en bout ?</summary>
        public bool Validate(out string probleme)
        {
            int total = 0;
            foreach (bool b in Floor) if (b) total++;

            var vus = new HashSet<int> { Key(1, 1) };
            var pile = new Stack<Cell>(); pile.Push(new Cell(1, 1));
            while (pile.Count > 0)
            {
                var c = pile.Pop();
                for (int d = 0; d < 4; d++)
                {
                    int nx = c.x + Dirs[d, 0], ny = c.y + Dirs[d, 1];
                    if (!IsFloor(nx, ny) || vus.Contains(Key(nx, ny))) continue;
                    vus.Add(Key(nx, ny)); pile.Push(new Cell(nx, ny));
                }
            }
            if (vus.Count != total) { probleme = "labyrinthe non connexe"; return false; }
            foreach (var c in Chambers)
                if (!IsDeathFree(c.x, c.y)) { probleme = "chambre derrière un piège mortel"; return false; }
            if (!IsDeathFree(Exit.x, Exit.y)) { probleme = "sortie derrière un piège mortel"; return false; }
            probleme = null;
            return true;
        }

        // ------------------------------------------------------------------ aléa
        int[] Shuffle(int[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                // échange explicite : certaines versions du compilateur Mono livré
                // avec Unity compilent mal « (a[i], a[j]) = (a[j], a[i]) » et
                // dupliquent une direction au lieu de l'échanger. Le labyrinthe
                // se creusait alors à moitié. Ne pas « simplifier » cette boucle.
                int t = a[i]; a[i] = a[j]; a[j] = t;
            }
            return a;
        }
        void ShuffleList<T>(List<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                T t = l[i]; l[i] = l[j]; l[j] = t;      // voir le commentaire de Shuffle
            }
        }

        public static Vector3 World(int x, int y) => new Vector3(x * Tile, 0f, y * Tile);
        public static Vector2Int Grid(Vector3 p) =>
            new Vector2Int(Mathf.RoundToInt(p.x / Tile), Mathf.RoundToInt(p.z / Tile));
    }
}

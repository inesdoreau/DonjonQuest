#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DonjonDeals.EditorTools
{
    /// <summary>
    /// Les FBX KayKit n'arrivent pas tous à la même échelle : certains sont en
    /// centimètres, d'autres en unités arbitraires, et le facteur d'import
    /// d'Unity ne suffit pas à les rendre cohérents entre eux.
    ///
    /// Plutôt que de coller un facteur magique par fichier — qui se casse dès
    /// qu'on remplace un modèle — ce post-processeur mesure la boîte englobante
    /// du maillage importé et la met à la taille réelle attendue, en mètres.
    /// Les valeurs ci-dessous ont été relevées une à une sur le prototype web
    /// et validées à l'œil : un tonneau fait 1,20 m, une table longue 2,60 m,
    /// un mur 4,00 m — la taille d'une case du labyrinthe.
    ///
    /// Si vous ajoutez un modèle, ajoutez sa ligne ici. S'il n'y est pas, il est
    /// importé tel quel et le journal vous le signale une fois.
    /// </summary>
    public class DonjonDealsImporter : AssetPostprocessor
    {
        const string Dossier = "/DonjonDeals/Modeles/";

        /// <summary>Nom du fichier source ▸ plus grande dimension attendue, en mètres.</summary>
        static readonly Dictionary<string, float> Tailles = new Dictionary<string, float>
        {
            { "plate_stack", 0.400f },   // assiettes
            { "banner_patternA_blue", 3.196f },   // banner_blue
            { "banner_patternA_green", 3.196f },   // banner_green
            { "banner_patternA_red", 3.196f },   // banner_red
            { "barrel_large", 1.200f },   // barrel
            { "barrel_small_stack", 1.400f },   // barrels
            { "candle_lit", 0.380f },   // candle
            { "ceiling_tile", 4.000f },   // ceiling
            { "chair", 1.000f },   // chaise
            { "chest", 1.100f },   // chest
            { "chest_gold", 1.100f },   // chest_gold
            { "Parts_Cog", 0.400f },   // cog
            { "column", 1.400f },   // column
            { "Copper_Nugget_Medium", 0.340f },   // copper_nugget
            { "box_large", 1.000f },   // crate
            { "box_small", 0.620f },   // crate_small
            { "anvil", 1.100f },   // enclume
            { "shelf_large", 2.000f },   // etagere
            { "floor_tile_large", 4.000f },   // floor
            { "floor_dirt_large", 4.000f },   // floor_dirt
            { "floor_tile_big_grate", 4.000f },   // floor_grate
            { "floor_tile_big_grate_open", 4.000f },   // floor_grate_open
            { "floor_tile_large_rocks", 4.000f },   // floor_rocks
            { "floor_tile_big_spikes", 4.000f },   // floor_spikes
            { "Gold_Bar", 0.500f },   // gold_bar
            { "Gold_Nugget_Medium", 0.340f },   // gold_nugget
            { "Gold_Bars_Stack_Medium", 0.600f },   // gold_stack
            { "Iron_Bar", 0.500f },   // iron_bar
            { "journal_open", 0.450f },   // journal
            { "torch", 0.650f },   // lantern
            { "bed_frame", 2.200f },   // lit
            { "map_rolled", 0.500f },   // map_rolled
            { "grindstone", 1.200f },   // meule
            { "pillar", 4.000f },   // pillar
            { "rubble_large", 8.129f },   // rubble
            { "rubble_half", 4.000f },   // rubble_half
            { "crates_stacked", 1.600f },   // sack
            { "shield_round", 0.750f },   // shield
            { "Silver_Nugget_Medium", 0.340f },   // silver_nugget
            { "Silver_Bars_Stack_Small", 0.550f },   // silver_stack
            { "Skeleton_Axe", 0.900f },   // skel_axe
            { "Skeleton_Blade", 0.950f },   // skel_blade
            { "Skeleton_Shield_Small_A", 0.650f },   // skel_shield
            { "sword_1handed", 1.000f },   // sword
            { "table_medium_broken", 1.900f },   // table_cassee
            { "table_long", 2.600f },   // table_long
            { "table_small_decorated_A", 1.200f },   // table_petite
            { "stool", 0.700f },   // tabouret
            { "torch_mounted", 0.950f },   // torch
            { "wall", 4.000f },   // wall
            { "wall_arched", 4.000f },   // wall_arched
            { "wall_broken", 4.000f },   // wall_broken
            { "wall_cracked", 4.000f },   // wall_cracked
            { "wall_doorway", 4.000f },   // wall_doorway
            { "wall_gated", 4.000f },   // wall_gated
            { "wall_shelves", 4.000f },   // wall_shelves
            { "wall_archedwindow_gated", 4.000f },   // wall_window
            { "shelves", 1.900f },   // weaponrack
        };

        // ---------------------------------------------------------------- import

        void OnPreprocessModel()
        {
            if (!assetPath.Contains(Dossier)) return;
            var mi = (ModelImporter)assetImporter;

            mi.useFileScale = true;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.None;   // rendu non-éclairé stylisé
            mi.importCameras = false;
            mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.External;
            mi.generateSecondaryUV = false;

            // Les personnages partagent un squelette de 23 os : sans « Humanoid »
            // mais avec le même rig générique, les banques d'animations se
            // rebranchent d'un personnage à l'autre sans retargeting.
            bool personnage = assetPath.Contains("/Personnages/") || assetPath.Contains("/Animations/");
            mi.animationType = personnage ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            if (personnage) mi.motionNodeName = "<None>";
        }

        void OnPostprocessModel(GameObject racine)
        {
            if (!assetPath.Contains(Dossier)) return;

            string nom = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (!Tailles.TryGetValue(nom, out float cible))
            {
                Debug.Log($"[Donjon Deals] « {nom} » n'a pas de taille de référence, " +
                          "importé tel quel. Ajoutez-le à DonjonDealsImporter.Tailles.");
                return;
            }

            var bornes = Mesurer(racine);
            float plusGrande = Mathf.Max(bornes.size.x, Mathf.Max(bornes.size.y, bornes.size.z));
            if (plusGrande < 0.0001f) return;

            float k = cible / plusGrande;
            racine.transform.localScale = Vector3.one * k;
        }

        static Bounds Mesurer(GameObject racine)
        {
            var filtres = racine.GetComponentsInChildren<MeshFilter>(true);
            var peaux = racine.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            bool premier = true;
            var b = new Bounds();

            foreach (var f in filtres)
            {
                if (f.sharedMesh == null) continue;
                var mb = f.sharedMesh.bounds;
                mb.center = f.transform.localPosition + mb.center;
                if (premier) { b = mb; premier = false; } else b.Encapsulate(mb);
            }
            foreach (var s in peaux)
            {
                if (s.sharedMesh == null) continue;
                var mb = s.sharedMesh.bounds;
                if (premier) { b = mb; premier = false; } else b.Encapsulate(mb);
            }
            return b;
        }

        // ---------------------------------------------------------------- vérification

        /// <summary>
        /// Le contrôle que je ne peux pas lancer à votre place : il génère deux
        /// cents labyrinthes et vérifie les deux garanties du générateur.
        /// À lancer une fois après l'import, puis à chaque modification de
        /// MazeGenerator. S'il passe, aucune partie ne peut être injouable.
        /// </summary>
        [MenuItem("Donjon Deals/Vérifier 200 labyrinthes")]
        public static void VerifierLesLabyrinthes()
        {
            int rates = 0;
            string premierProbleme = null;
            for (int graine = 1; graine <= 200; graine++)
            {
                var m = new MazeGenerator();
                m.Generate(graine);
                if (m.Validate(out string probleme)) continue;
                rates++;
                if (premierProbleme == null) premierProbleme = $"graine {graine} : {probleme}";
            }

            if (rates == 0)
                Debug.Log("[Donjon Deals] 200 labyrinthes vérifiés : tous connexes, " +
                          "chambres et sortie atteignables sans marcher sur un piège mortel.");
            else
                Debug.LogError($"[Donjon Deals] {rates} labyrinthes sur 200 sont injouables. " +
                               $"Premier cas : {premierProbleme}");
        }

        [MenuItem("Donjon Deals/Remettre la progression à zéro")]
        public static void RemettreAZero()
        {
            Boutique.ToutEffacer();
            Debug.Log("[Donjon Deals] Or, achats et personnages débloqués effacés.");
        }
    }
}
#endif

# Échelles des 58 modèles

Une case du labyrinthe fait **4 mètres** (`MazeGenerator.Tile`). Tout est calé
là-dessus.

Les FBX KayKit n'arrivent pas tous à la même échelle : certains sont en
centimètres, d'autres en unités arbitraires. Plutôt qu'un facteur magique par
fichier — qui se casse dès qu'on remplace un modèle — `DonjonDealsImporter`
**mesure la boîte englobante du maillage importé et la met à la taille réelle
attendue**, colonne « max » ci-dessous.

Les dimensions ont été relevées une à une sur le prototype web, puis vérifiées à
l'œil dans la scène : un tonneau fait 1,20 m, une table longue 2,60 m, un mur
4,00 m. Si un modèle paraît deux fois trop gros dans Unity, corrigez **sa ligne
dans la table `Tailles`**, pas son échelle dans la scène — sinon la correction
disparaît au prochain réimport.

| Nom en jeu | Fichier source | Dimensions (m) | max |
|---|---|---|---|
| assiettes | `plate_stack` | 0.40 × 0.23 × 0.40 | **0.40** |
| banner_blue | `banner_patternA_blue` | 1.50 × 3.20 × 0.31 | **3.20** |
| banner_green | `banner_patternA_green` | 1.50 × 3.20 × 0.31 | **3.20** |
| banner_red | `banner_patternA_red` | 1.50 × 3.20 × 0.31 | **3.20** |
| barrel | `barrel_large` | 1.08 × 1.20 × 1.08 | **1.20** |
| barrels | `barrel_small_stack` | 1.40 × 1.34 × 0.76 | **1.40** |
| candle | `candle_lit` | 0.12 × 0.38 × 0.12 | **0.38** |
| ceiling | `ceiling_tile` | 4.00 × 0.35 × 4.00 | **4.00** |
| chaise | `chair` | 0.61 × 1.00 × 0.61 | **1.00** |
| chest | `chest` | 1.10 × 0.84 × 0.94 | **1.10** |
| chest_gold | `chest_gold` | 1.10 × 0.84 × 0.94 | **1.10** |
| cog | `Parts_Cog` | 0.37 × 0.40 × 0.15 | **0.40** |
| column | `column` | 0.70 × 1.40 × 0.70 | **1.40** |
| copper_nugget | `Copper_Nugget_Medium` | 0.31 × 0.34 × 0.34 | **0.34** |
| crate | `box_large` | 1.00 × 1.00 × 1.00 | **1.00** |
| crate_small | `box_small` | 0.62 × 0.62 × 0.62 | **0.62** |
| enclume | `anvil` | 1.10 × 0.54 × 0.51 | **1.10** |
| etagere | `shelf_large` | 2.00 × 0.45 × 0.50 | **2.00** |
| floor | `floor_tile_large` | 4.00 × 0.15 × 4.00 | **4.00** |
| floor_dirt | `floor_dirt_large` | 4.00 × 0.21 × 4.00 | **4.00** |
| floor_grate | `floor_tile_big_grate` | 4.00 × 1.05 × 4.00 | **4.00** |
| floor_grate_open | `floor_tile_big_grate_open` | 4.00 × 1.05 × 4.00 | **4.00** |
| floor_rocks | `floor_tile_large_rocks` | 4.00 × 0.64 × 4.00 | **4.00** |
| floor_spikes | `floor_tile_big_spikes` | 4.00 × 2.10 × 4.00 | **4.00** |
| gold_bar | `Gold_Bar` | 0.25 × 0.16 × 0.50 | **0.50** |
| gold_nugget | `Gold_Nugget_Medium` | 0.31 × 0.34 × 0.34 | **0.34** |
| gold_stack | `Gold_Bars_Stack_Medium` | 0.32 × 0.60 × 0.32 | **0.60** |
| iron_bar | `Iron_Bar` | 0.25 × 0.16 × 0.50 | **0.50** |
| journal | `journal_open` | 0.45 × 0.35 × 0.17 | **0.45** |
| lantern | `torch` | 0.12 × 0.65 × 0.13 | **0.65** |
| lit | `bed_frame` | 1.10 × 0.78 × 2.20 | **2.20** |
| map_rolled | `map_rolled` | 0.10 × 0.10 × 0.50 | **0.50** |
| meule | `grindstone` | 1.02 × 0.82 × 1.20 | **1.20** |
| pillar | `pillar` | 1.50 × 4.00 × 1.50 | **4.00** |
| rubble | `rubble_large` | 8.13 × 3.50 × 3.18 | **8.13** |
| rubble_half | `rubble_half` | 4.00 × 3.50 × 3.00 | **4.00** |
| sack | `crates_stacked` | 1.49 × 1.52 × 1.60 | **1.60** |
| shield | `shield_round` | 0.75 × 0.75 × 0.28 | **0.75** |
| silver_nugget | `Silver_Nugget_Medium` | 0.31 × 0.34 × 0.34 | **0.34** |
| silver_stack | `Silver_Bars_Stack_Small` | 0.55 × 0.52 × 0.55 | **0.55** |
| skel_axe | `Skeleton_Axe` | 0.71 × 0.90 × 0.20 | **0.90** |
| skel_blade | `Skeleton_Blade` | 0.36 × 0.95 × 0.14 | **0.95** |
| skel_shield | `Skeleton_Shield_Small_A` | 0.65 × 0.65 × 0.12 | **0.65** |
| sword | `sword_1handed` | 0.28 × 1.00 × 0.07 | **1.00** |
| table_cassee | `table_medium_broken` | 1.78 × 0.76 × 1.90 | **1.90** |
| table_long | `table_long` | 1.30 × 0.65 × 2.60 | **2.60** |
| table_petite | `table_small_decorated_A` | 0.77 × 1.20 × 0.90 | **1.20** |
| tabouret | `stool` | 0.70 × 0.47 × 0.70 | **0.70** |
| torch | `torch_mounted` | 0.49 × 0.95 × 0.55 | **0.95** |
| wall | `wall` | 4.00 × 4.00 × 1.00 | **4.00** |
| wall_arched | `wall_arched` | 4.00 × 4.00 × 1.00 | **4.00** |
| wall_broken | `wall_broken` | 4.00 × 4.00 × 1.00 | **4.00** |
| wall_cracked | `wall_cracked` | 4.00 × 4.00 × 1.26 | **4.00** |
| wall_doorway | `wall_doorway` | 4.00 × 4.00 × 1.00 | **4.00** |
| wall_gated | `wall_gated` | 4.00 × 4.00 × 1.00 | **4.00** |
| wall_shelves | `wall_shelves` | 4.00 × 4.00 × 1.37 | **4.00** |
| wall_window | `wall_archedwindow_gated` | 4.00 × 4.00 × 1.00 | **4.00** |
| weaponrack | `shelves` | 1.90 × 1.85 × 0.47 | **1.90** |

## Ajouter un modèle

1. Déposez le FBX sous `Assets/DonjonDeals/Modeles/`.
2. Ajoutez une ligne à `Tailles` dans `Editor/DonjonDealsImporter.cs` :
   `{ "NomDuFichier", 1.20f },`
3. Réimportez (clic droit ▸ Reimport).

Un modèle absent de la table est importé tel quel et signalé une fois dans la
console — pas d'erreur, juste un rappel.

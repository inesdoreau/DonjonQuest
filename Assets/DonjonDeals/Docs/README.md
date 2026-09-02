# Donjon Deals — paquet Unity

Portage C# du prototype web V3. Même générateur de labyrinthe, mêmes pactes,
mêmes réglages, mêmes garanties de jouabilité.

> **À lire d'abord.** Je n'ai pas pu ouvrir Unity pour écrire ce paquet : il n'y
> en a pas dans mon environnement. Le code a donc été compilé et testé contre une
> maquette de l'API Unity, ce qui garantit la syntaxe, les types et **la logique
> du générateur** (500 labyrinthes vérifiés, voir plus bas), mais **pas** le
> comportement en jeu : positions exactes, collisions, animations et rendu se
> règlent forcément dans l'éditeur. Attendez-vous à quelques ajustements de
> valeurs le premier jour, pas à une réécriture.

---

## 1. Démarrer en trois minutes

1. Unity **2021.3 LTS ou plus récent**, pipeline **URP** de préférence
   (le rendu intégré fonctionne aussi ; les matériaux provisoires gèrent les deux).
2. Copiez le dossier `Assets/DonjonDeals` dans votre projet.
3. Menu **Donjon Deals ▸ Créer la scène de départ**.
4. Ouvrez `Assets/DonjonDeals/Scenes/Donjon.unity`, appuyez sur **Play**.

À ce stade le jeu est **jouable avec des cubes** : labyrinthe, pièges, coffres,
autels, squelettes, boutique et fin de partie fonctionnent. C'est volontaire —
on valide la mécanique avant d'y poser les modèles, et si quelque chose casse
plus tard on sait que ça vient de l'art, pas du code.

### Commandes

| Action | Clavier (joueur 1) | Manette (joueurs 2 à 4) |
|---|---|---|
| Se déplacer | ZQSD / WASD / flèches | stick gauche |
| Frapper | clic gauche | bouton *Frappe* |
| Esquiver | Espace | bouton *Esquive* |
| Interagir / relever | E (maintenir pour relever) | bouton *Interagir* |
| Lâcher la pièce la plus lourde | R | bouton *Lâcher* |
| Marcher en silence | Maj | bouton *Furtif* |
| Signer un pacte | 1 / 2 / 3 à l'autel | idem clavier (voir UNITY-INPUT.md) |

Les axes manette sont à déclarer une fois : voir `UNITY-INPUT.md`.

---

## 2. Poser vos modèles KayKit

1. Créez `Assets/DonjonDeals/Modeles/` et déposez-y les FBX.
   Le chemin compte : le post-processeur ne s'active que sous ce dossier.
2. Les personnages et les banques d'animations vont dans
   `Modeles/Personnages/` et `Modeles/Animations/` — ils sont importés en
   *Generic* pour que les banques se rebranchent d'un personnage à l'autre.
3. `DonjonDealsImporter` met chaque modèle à sa taille réelle en mètres
   (table `Tailles`, 58 entrées relevées sur le prototype). Un modèle absent de
   la table est importé tel quel et signalé une fois dans la console.
4. Faites un prefab par modèle, puis remplacez les prefabs provisoires dans
   **Partie ▸ DungeonBuilder**, un par un. Chaque remplacement est réversible.

Une case du labyrinthe fait **4 mètres**. Tout est calé là-dessus : un mur fait
4 m, un sol 4 m, un tonneau 1,20 m. Si un modèle paraît deux fois trop gros,
c'est sa ligne dans `Tailles` qu'il faut corriger, pas son échelle dans la scène.

---

## 3. Ce que le code garantit, et comment le vérifier

Menu **Donjon Deals ▸ Vérifier 200 labyrinthes**.

Le contrôle génère des labyrinthes et vérifie deux propriétés :

1. **connexité** — toutes les cases de sol communiquent ;
2. **jouabilité** — chaque chambre et la sortie sont atteignables **sans jamais
   poser le pied sur une case qui tue**.

La deuxième garantie vient d'un calcul de *points d'articulation* (Tarjan) : une
case dont le retrait couperait le labyrinthe en deux ne peut pas recevoir de
piège mortel. C'est la réponse directe à « j'ai avancé de douze cases et mon
perso est mort, impossible d'atteindre une salle ».

**Résultat mesuré sur 500 graines** (exécuté hors Unity, sur le générateur seul) :

```
labyrinthes ratés : 0
piques   (moy.)   : 3.0      ← mortelles, cycliques, jamais sur un passage obligé
rouleaux (moy.)   : 2.9      ← mortels, balaient 3 cases, mêmes contraintes
trappes  (moy.)   : 6.0      ← non mortelles : elles séparent, elles ne tuent pas
murs piv.(moy.)   : 12.0     ← non mortels
cases de sol   (minimum) : 243
cases sûres    (minimum) : 204   ← 84 % du labyrinthe accessible sans risque mortel
```

Relancez ce contrôle après **toute** modification de `MazeGenerator`.

---

## 4. Carte des fichiers

```
Scripts/
  Maze/MazeGenerator.cs      génération, pièges, garanties de jouabilité
  Config/GameTuning.cs       TOUS les nombres du jeu + la palette
  Players/PlayerAgent.cs     charge, bruit, combat, pactes, chute et relevage
  Players/PlayerInputRouter.cs  clavier J1, manettes J2–J4
  Players/PartyCamera.cs     une caméra pour quatre, inclinaison 30°
  World/DungeonBuilder.cs    instancie le décor à partir de la grille
  World/Hazards.cs           piques, rouleau, trappe, mur pivotant
  World/SkeletonAI.cs        l'IA qui écoute au lieu de voir
  World/Interactions.cs      butin, coffre, autel, sorties
  Run/RunManager.cs          temps, fragments, pactes, fin, compte final
  Run/Boutique.cs            l'or entre deux descentes (PlayerPrefs)
  UI/HudDeSecours.cs         interface IMGUI jetable, à remplacer
Editor/
  DonjonDealsImporter.cs     échelles des 58 modèles + contrôle des labyrinthes
  CreateurDeScene.cs         monte la scène jouable en un clic
Docs/
  README.md  DIRECTION-ARTISTIQUE.md  UNITY-INPUT.md  PALETTE.md  ECHELLES.md
```

**Le fichier à connaître avant tous les autres est `Config/GameTuning.cs`.**
Vitesse, capacité, bruit, durées, valeurs du butin, texte des onze pactes : tout
y est, et rien n'est codé en dur ailleurs. Créez l'asset de réglages
(`Assets ▸ Create ▸ Donjon Deals ▸ Réglages`), modifiez-le en cours de partie,
l'effet est immédiat.

---

## 5. La règle qui tient tout

> **Chaque avantage est un emprunt, et c'est le groupe qui rembourse.**

Aucun objet de boutique et aucun pacte n'est un bonus pur. Le coffre porte plus
mais ralentit. La torche éclaire mais vous rend visible. Le sursis vous rend
invulnérable puis vous présente la note d'un coup. Le bouc désigne quelqu'un
d'autre pour encaisser vos coups, sans lui demander son avis.

Si vous ajoutez un pacte et que vous ne savez pas quoi écrire dans son champ
`prix`, c'est que ce n'est pas un pacte : c'est un bonus, et il n'a pas sa place.

---

## 6. Ce qui reste à faire

- **Animations** : `PlayerAgent` et `SkeletonAI` envoient déjà les paramètres
  `vitesse`, `furtif`, `frappe`, `touche`, `tombe`, `meurt`. Il reste à câbler
  l'Animator Controller sur les banques KayKit.
- **Interface définitive** : `HudDeSecours` affiche tout ce qu'il faut, en laid.
  La direction artistique visée est décrite dans `DIRECTION-ARTISTIQUE.md`.
- **Son** : rien n'est branché. Le bruit est pourtant la mécanique centrale —
  c'est probablement le premier chantier qui change tout.
- **Menu et boutique en jeu** : `Boutique` expose déjà l'or, les achats et les
  personnages ; il manque l'écran.
- **Multijoueur en ligne** : non traité, et ce n'est pas un oubli. Le jeu est
  pensé pour un écran partagé — la trappe qui sépare et la caméra commune
  n'ont de sens que là.

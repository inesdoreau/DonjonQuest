# Direction artistique

Ce document existe pour une raison précise : les modèles KayKit sont cohérents
entre eux, mais **tout ce qu'on ajoute autour** — menus, typographie, couleurs
d'interface, lumière — peut détruire cette cohérence en une journée. C'était le
reproche fait au prototype : *« Violet et doré plus texte en sérif, le graphisme
autour du jeu n'est pas très cohérent. »* Il avait raison. Voici les règles qui
ont corrigé ça, et pourquoi elles tiennent.

---

## 1. Le principe

Les personnages sont **cartoon, ronds, à faible contraste interne** : peu de
détails, de grands aplats, des silhouettes lisibles à trente mètres. Une
interface fine, sérif et sombre les contredit — elle promet un jeu grave alors
que le jeu est drôle et cruel.

> Tout ce qui entoure le jeu doit être **rond, épais, franc et chaud**.
> Tout ce qui est dans le donjon doit être **sombre, froid, avare de lumière**.

Le contraste entre les deux est l'identité : on descend d'un camp chaleureux
vers un endroit qui ne l'est pas.

---

## 2. Typographie

Trois familles, toutes **libres de droits** (SIL Open Font License), toutes
disponibles sur Google Fonts — donc téléchargeables en `.ttf` et intégrables
dans Unity sans question de licence.

| Emploi | Police | Pourquoi |
|---|---|---|
| Titres, nom du jeu, gros chiffres | **Luckiest Guy** | Capitales grasses, contours arrondis, légèrement irrégulières. C'est la famille des affiches de *Super Guild* / *Spenbeb Game* que vous m'aviez montrées : même énergie, licence libre. |
| Sous-titres, boutons, noms de pactes | **Titan One** | Même esprit, un peu plus sobre, meilleure lisibilité en petit. |
| Texte courant, chiffres, journal | **Nunito** (600 / 800) | Grotesque **ronde**, très lisible en petit corps, sans empattement. C'est elle qui remplace le sérif. |

Règles d'usage :

- **Jamais de sérif.** Nulle part.
- Titres en capitales, texte courant en bas-de-casse.
- Un seul niveau de gras dans un même écran.
- Chiffres importants (or, minuteur) en Luckiest Guy : ils doivent se lire d'un
  coup d'œil depuis le canapé, à deux mètres de l'écran.

Import dans Unity : `Window ▸ TextMeshPro ▸ Font Asset Creator`, puis générer un
atlas par famille. Prévoyez les accents français dans le jeu de caractères —
c'est l'oubli classique, et il ne se voit qu'au moment du premier « é ».

---

## 3. Couleurs

Six couleurs fonctionnelles, pas une de plus. Elles sont relevées **directement
sur les atlas KayKit**, donc l'interface et les modèles partagent la même
famille chromatique sans qu'on ait à retoucher les textures.

Le détail des valeurs est dans `PALETTE.md` et dans le code
(`GameTuning.cs`, classe statique `Palette`).

La règle qui compte : **une couleur = une signification**.

| Couleur | Sens, et rien d'autre |
|---|---|
| Ambre `#fab454` | l'or, la valeur, ce qu'on emporte |
| Bleu ciel `#6babd6` | l'information, la sortie, le salut |
| Vert `#64b244` | ce qui va bien, la santé haute |
| Rouge `#d93236` | le danger, la santé basse, le mortel |
| Orange flamme `#ff8a3a` | les autels, un piège armé — ce qui va se passer |
| Gris cendre `#93a1ac` | tout le reste : texte secondaire, décor |

Si une septième couleur devient nécessaire, c'est presque toujours qu'une
information a été mal hiérarchisée. Cherchez à en supprimer une avant d'en
ajouter une.

Le violet a été retiré : il n'existe nulle part dans les atlas, et il tirait
l'ensemble vers un fantastique clinquant qui n'est pas ce jeu.

---

## 4. Lumière

C'est le vrai sujet du donjon, et le plus facile à rater.

- **Ambiance très basse** (`#22262e`), brouillard exponentiel dense.
  On doit avoir envie d'une torche.
- Les torches sont **rares** : une toutes les neuf cases. Trop de torches et
  l'obscurité ne veut plus rien dire, donc le pacte de la lampe ne fait plus peur.
- La lanterne du joueur est une **lumière ponctuelle chaude** de portée ~11 m.
  C'est sa bulle, et le pacte de la lampe la double pendant que tout le reste
  s'éteint.
- **Les yeux des squelettes sont émissifs.** C'est délibéré : quand toutes les
  torches s'éteignent, ils restent visibles. Ce sont eux qui rendent le noir
  jouable — et terrifiant.
- Aucune lumière colorée qui ne signifie rien. Orange = feu, ambre = or,
  bleu = sortie, rouge = orbites. Une lumière violette dans un couloir, c'est
  une promesse que le jeu ne tient pas.

**Performance.** Le prototype web est devenu injouable avec ~90 lumières
ponctuelles. Unity gère mieux, mais la leçon vaut : limitez les lumières
temps réel par pixel (URP : *Additional Lights*, 8 max par objet), passez le
décor fixe en *baked* ou *mixed*, et gardez le temps réel pour ce qui bouge —
lanternes, autels, pièges armés.

---

## 5. Caméra

- **Inclinaison 30°**, pas 45°. À 45° on regarde des crânes ; à 30° on voit loin
  devant, et le labyrinthe redevient lisible.
- Champ de vision 52°.
- Une seule caméra pour toute l'équipe, ancrée sur le joueur 1, qui ne cadre que
  les joueurs à moins de 26 m. Un joueur éjecté par une trappe apparaît en
  **pastille sur le bord de l'écran** au lieu de faire dézoomer tout le monde.
- Distance bornée à 21 m : au-delà, les personnages deviennent trop petits pour
  qu'on lise qui est qui.

## 6. Ce que le joueur doit lire sans réfléchir

Par ordre de priorité — si un écran est chargé, c'est ce qui doit survivre :

1. **Où je suis, et où sont les autres.** Couleur par joueur, pastille hors cadre.
2. **Est-ce que ça va me tuer.** Rouge, et rien d'autre n'est rouge.
3. **Combien je porte.** La jauge de charge vire au rouge au seuil de blocage :
   c'est le moment où courir devient impossible, il doit se voir avant d'être subi.
4. **Combien de temps il reste.** Le minuteur passe au rouge sous une minute.
5. Le reste.

# Palette

Relevée sur les atlas KayKit, pour que l'interface et les modèles partagent la
même famille chromatique. Les valeurs vivent dans `Config/GameTuning.cs`,
classe statique `Palette` — utilisez-la plutôt que de retaper un hexadécimal.

## Fonds et texte

| Nom | Hex | Emploi |
|---|---|---|
| `Pierre` | `#161c23` | fond de l'écran, brouillard |
| `Panneau` | `#222a34` | panneaux d'interface |
| `Bordure` | `#313d4a` | filets, séparateurs |
| `Os` | `#eef2f4` | texte principal |
| `Cendre` | `#93a1ac` | texte secondaire |

## Couleurs qui veulent dire quelque chose

| Nom | Hex | Signification, et rien d'autre |
|---|---|---|
| `Soleil` | `#fab454` | l'or, la valeur, l'accent |
| `Bois` | `#b87556` | le bois, les caisses, le portage |
| `Ciel` | `#6babd6` | l'information, la sortie |
| `Feuille` | `#64b244` | la santé haute, ce qui va bien |
| `Rouille` | `#d93236` | le danger, la santé basse, le mortel |
| `Flamme` | `#ff8a3a` | autels, pièges armés — ce qui va arriver |

## Les quatre joueurs

| Joueur | Hex |
|---|---|
| J1 | `#fab454` ambre |
| J2 | `#6babd6` bleu |
| J3 | `#64b244` vert |
| J4 | `#ff7a6b` corail |

Choisies pour rester distinctes en vision des couleurs déficiente : la paire
critique rouge/vert est écartée par la luminance (le corail est nettement plus
clair que le vert) autant que par la teinte.

## Raccourci utile

```csharp
Palette.Vitalite(pv / pvMax)   // vert > 60 %, ambre > 30 %, rouge en dessous
```

Convention universelle : ne la réinventez pas, aucun joueur n'a besoin
d'apprendre votre code couleur.

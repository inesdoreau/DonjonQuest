# Manettes — les axes à déclarer

`PlayerInputRouter` utilise volontairement **l'ancien Input Manager**, pour que
le paquet compile sans installer le moindre paquet. Le prix à payer : il faut
déclarer les axes une fois, à la main.

**Edit ▸ Project Settings ▸ Input Manager ▸ Axes**

Le joueur 1 joue au clavier et à la souris, sans configuration. Les joueurs 2 à
4 utilisent des axes préfixés `J2_`, `J3_`, `J4_`.

## Pour chaque joueur *n* de 2 à 4

| Name | Type | Réglage | Joystick |
|---|---|---|---|
| `Jn_Horizontal` | Joystick Axis | X axis, Gravity 0, Dead 0.19, Sensitivity 1 | Joystick *n−1* |
| `Jn_Vertical` | Joystick Axis | Y axis, **Invert coché** | Joystick *n−1* |
| `Jn_Frappe` | Key or Mouse Button | Positive Button `joystick n-1 button 0` | — |
| `Jn_Esquive` | Key or Mouse Button | `joystick n-1 button 1` | — |
| `Jn_Interagir` | Key or Mouse Button | `joystick n-1 button 2` | — |
| `Jn_Lacher` | Key or Mouse Button | `joystick n-1 button 3` | — |
| `Jn_Furtif` | Key or Mouse Button | `joystick n-1 button 4` | — |

> **Pourquoi « Invert » sur l'axe vertical.** Dans le jeu, « avancer » va vers le
> fond de l'écran, donc vers les Y négatifs de la commande. C'est exactement le
> réglage qui avait été signalé à l'envers dans le prototype web : ici, la
> convention est fixée à un seul endroit — `PlayerInputRouter.LireClavier`, où
> la touche « avancer » écrit `y -= 1`. Si le sens paraît inversé, c'est **cette
> ligne et cette case Invert** qu'il faut regarder, jamais le code de
> déplacement.

## Numérotation des manettes

Unity numérote les joysticks à partir de 1. Le joueur 2 utilise donc
`joystick 1`, le joueur 3 `joystick 2`, le joueur 4 `joystick 3`.

Les appels d'axes sont enveloppés dans un `try/catch` : un axe non déclaré ne
provoque pas d'exception, la manette est simplement inerte. Vous pouvez donc
tester à deux joueurs sans déclarer J3 et J4.

`PlayerInputRouter.ManettesBranchees()` renvoie le nombre de manettes détectées :
de quoi proposer le bon nombre de joueurs dans le futur menu.

## Si vous passez au nouvel Input System

Une seule classe est à réécrire : `PlayerInputRouter`. Elle ne fait qu'écrire
`Direction`, `MaintientInteraction`, `ReglerFurtif()` et appeler `Attaquer()`,
`Esquiver()`, `Interactions.Declencher()`, `LacherLaPlusLourde()`.
Rien d'autre dans le jeu ne lit les entrées.

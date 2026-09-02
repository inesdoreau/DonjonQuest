using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    // ======================================================================== butin

    [Serializable]
    public class LootDef
    {
        public string id;          // clé du prefab
        public string nom;
        public int valeur;
        public int poids;
        public float hauteur;      // position en Y quand la pièce est au sol
        public bool relique;
        public bool fragment;      // fragment de relevé : pas de valeur, ouvre la sortie
    }

    // ======================================================================== pactes

    public enum Famille { Discorde, Rire, Suspense }

    [Serializable]
    public class PactDef
    {
        public string id;
        public Famille famille;
        public string nom;
        public string gain;        // ce que le signataire obtient
        public string prix;        // ce que ça coûte, et à qui
        public string resume;      // ligne affichée dans le bandeau d'équipe
        public bool groupe;        // n'a de sens qu'à plusieurs
        public float duree;        // 0 = permanent jusqu'à la fin de la descente
    }

    /// <summary>
    /// Tous les nombres du jeu au même endroit. Créer via
    /// Assets ▸ Create ▸ Donjon Deals ▸ Réglages, puis brancher sur le RunManager.
    ///
    /// Les valeurs par défaut sont celles du prototype web, réglées à la main :
    /// elles tiennent la promesse « une équipe qui ne ramasse rien et ne signe
    /// aucun pacte ressort en huit minutes sans difficulté ».
    /// </summary>
    [CreateAssetMenu(menuName = "Donjon Deals/Réglages", fileName = "ReglagesDonjonDeals")]
    public class GameTuning : ScriptableObject
    {
        [Header("La descente")]
        public float dureeSecondes = 480f;      // huit minutes
        public int fragmentsRequis = 4;
        public float primeGroupe = 1.25f;       // si personne n'est resté en bas
        public float primePuitsDuFond = 1.5f;

        [Header("Le porteur")]
        public float vitesseBase = 6.6f;
        public int capacite = 60;
        [Tooltip("Au-delà de ce taux de charge, on ne court plus du tout.")]
        public float seuilBlocage = 0.85f;
        public float vitesseBloquee = 2.4f;
        public float penaliteCharge = 0.52f;    // vitesse = base × (1 − penalite × charge)
        public float vitesseEsquive = 13.5f;
        public float dureeEsquive = 0.42f;
        public float recuperationEsquive = 1.1f;

        [Header("Le bruit — c'est là que l'avidité se paie")]
        public float bruitAVide = 6.5f;
        public float bruitParCharge = 9.5f;     // ajouté à pleine besace
        public float bruitFurtif = 0.55f;
        [Tooltip("Le gisement écoute de mieux en mieux : +16 % par minute, plafonné.")]
        public float eveilParMinute = 0.16f;
        public float eveilMax = 0.9f;

        [Header("Combat")]
        public float porteeAttaque = 2.9f;
        public float recuperationAttaque = 0.62f;
        public float degatsMin = 26f, degatsMax = 40f;
        public float invulnerabiliteApresCoup = 0.55f;
        public float dureeRelevage = 2.5f;
        public int pvApresRelevage = 40;

        [Header("Caméra d'équipe")]
        public float distanceBase = 11.5f;
        public float distanceMax = 21f;
        public float inclinaisonDegres = 30.4f;
        public float rayonGroupe = 26f;         // au-delà, le joueur n'est plus cadré

        [Header("Pièges")]
        public float dureeTrappeOuverte = 4f;
        public int degatsChute = 12;
        [Tooltip("Portée du saut de la trappe, en cases. Bornée à plusieurs : " +
                 "sur un écran partagé, personne ne doit se retrouver hors cadre.")]
        public int sautTrappeMin = 4, sautTrappeMaxSolo = 60, sautTrappeMaxCoop = 7;

        [Header("Butin")]
        public List<LootDef> butin = new List<LootDef>
        {
            new LootDef { id="copper_nugget", nom="Pépite de cuivre",   valeur=8,   poids=3,  hauteur=0.10f },
            new LootDef { id="iron_bar",      nom="Lingot de fer",      valeur=16,  poids=6,  hauteur=0.08f },
            new LootDef { id="silver_nugget", nom="Pépite d'argent",    valeur=26,  poids=4,  hauteur=0.10f },
            new LootDef { id="silver_stack",  nom="Pile d'argent",      valeur=64,  poids=12, hauteur=0.05f },
            new LootDef { id="gold_nugget",   nom="Pépite d'or",        valeur=58,  poids=5,  hauteur=0.10f },
            new LootDef { id="gold_bar",      nom="Lingot d'or",        valeur=126, poids=10, hauteur=0.08f },
            new LootDef { id="gold_stack",    nom="Pile de lingots",    valeur=310, poids=26, hauteur=0.05f },
            new LootDef { id="cog",           nom="Rouage ancien",      valeur=210, poids=2,  hauteur=0.22f, relique=true },
            new LootDef { id="map_rolled",    nom="Fragment de relevé", valeur=0,   poids=0,  hauteur=0.10f, fragment=true },
        };

        [Header("Pactes")]
        public List<PactDef> pactes = new List<PactDef>
        {
            new PactDef { id="ivresse", famille=Famille.Rire, nom="L'ivresse",
                gain="Tu cours 40 % plus vite.",
                prix="Tes commandes dérivent lentement sur elles-mêmes.",
                resume="+40 % de vitesse · commandes qui dérivent" },
            new PactDef { id="geant", famille=Famille.Rire, nom="Le géant",
                gain="Tu grandis de moitié, ta portée augmente d'autant.",
                prix="Tu ne passes plus les murs pivotants, et ta besace perd 40 %.",
                resume="portée ×1.6 · murs pivotants presque infranchissables" },
            new PactDef { id="lampe", famille=Famille.Discorde, nom="La lampe", duree=30f,
                gain="Trente secondes : ta lanterne porte deux fois plus loin.",
                prix="Toutes les torches du labyrinthe s'éteignent. On ne verra que les yeux.",
                resume="30 s de lumière · le labyrinthe s'éteint" },
            new PactDef { id="sursis", famille=Famille.Suspense, nom="Le sursis", duree=60f,
                gain="Tu ne peux pas mourir pendant soixante secondes.",
                prix="À la fin, tu encaisses d'un coup tout ce que tu as évité.",
                resume="invulnérable · la note tombe à zéro" },
            new PactDef { id="meche", famille=Famille.Suspense, nom="La mèche",
                gain="Tout ce que tu ramasses vaut 50 % de plus.",
                prix="Chaque pièce ramassée avance une horloge. Quand elle sonne, tout se réveille.",
                resume="butin ×1.5 · la mèche brûle" },
            new PactDef { id="aube", famille=Famille.Suspense, nom="L'aube",
                gain="Tout l'or vaut le double.",
                prix="Il ne reste que deux minutes.",
                resume="or ×2 · deux minutes" },

            new PactDef { id="bouc", famille=Famille.Discorde, nom="Le bouc", groupe=true, duree=20f,
                gain="Vingt secondes sans pouvoir être blessé.",
                prix="Un autre joueur, tiré au sort, encaisse tous les coups à ta place.",
                resume="invulnérable · quelqu'un paie" },
            new PactDef { id="fardeau", famille=Famille.Discorde, nom="Le fardeau", groupe=true,
                gain="Tu vides la moitié de ta besace sur-le-champ.",
                prix="Elle atterrit chez un autre, qui ne peut pas refuser — mais qui gardera l'or.",
                resume="moitié de charge transférée" },
            new PactDef { id="miroir", famille=Famille.Discorde, nom="Le miroir", groupe=true,
                gain="Tu prends le pacte déjà signé par quelqu'un d'autre.",
                prix="Il le perd à l'instant même.",
                resume="pacte volé" },
            new PactDef { id="serment", famille=Famille.Discorde, nom="Le serment", groupe=true,
                gain="Toi et le juré frappez 50 % plus fort.",
                prix="S'il tombe, tu tombes avec lui.",
                resume="+50 % de dégâts · vies liées" },
            new PactDef { id="silence", famille=Famille.Discorde, nom="Le silence", groupe=true,
                gain="Tu deviens presque inaudible.",
                prix="Tous les autres font deux fois plus de bruit.",
                resume="inaudible · les autres crient" },
        };

        public LootDef Loot(string id) => butin.Find(b => b.id == id);
        public PactDef Pacte(string id) => pactes.Find(p => p.id == id);

        /// <summary>La graine du jour : le même labyrinthe pour tout le monde.</summary>
        public static int GraineDuJour()
        {
            var d = DateTime.Now;
            return d.Year * 10000 + d.Month * 100 + d.Day;
        }
    }

    /// <summary>
    /// La palette, relevée directement sur les atlas KayKit du jeu.
    /// Six couleurs fonctionnelles, pas une de plus : au-delà, le joueur
    /// n'associe plus une couleur à une signification.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Pierre   = Hex("#161c23"); // fond
        public static readonly Color Panneau  = Hex("#222a34");
        public static readonly Color Bordure  = Hex("#313d4a");
        public static readonly Color Os       = Hex("#eef2f4"); // texte
        public static readonly Color Cendre   = Hex("#93a1ac"); // texte secondaire

        public static readonly Color Soleil   = Hex("#fab454"); // accent, monnaie
        public static readonly Color Bois     = Hex("#b87556");
        public static readonly Color Ciel     = Hex("#6babd6"); // sortie, information
        public static readonly Color Feuille  = Hex("#64b244"); // ce qui va bien
        public static readonly Color Rouille  = Hex("#d93236"); // danger, critique
        public static readonly Color Flamme   = Hex("#ff8a3a"); // autels, pièges armés

        // couleurs des quatre joueurs
        public static readonly Color[] Joueurs = {
            Hex("#fab454"), Hex("#6babd6"), Hex("#64b244"), Hex("#ff7a6b")
        };

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }

        /// <summary>Vert, ambre, rouge : la convention que tout joueur lit sans réfléchir.</summary>
        public static Color Vitalite(float taux) =>
            taux > 0.6f ? Feuille : taux > 0.3f ? Soleil : Rouille;
    }
}

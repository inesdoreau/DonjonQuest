using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonjonDeals
{
    [Serializable]
    public class ArticleDef
    {
        public string id;
        public string nom;
        public int prix;
        public string gain;
        public string cout;
        public Color accent = Color.white;
        public Sprite vignette;
    }

    /// <summary>
    /// La boutique du camp de surface : elle dépense l'or remonté entre deux
    /// descentes. C'est le seul endroit du jeu où l'or sert à quelque chose,
    /// et la seule progression persistante.
    ///
    /// Règle absolue, la même que pour les pactes : chaque objet a un coût qui
    /// se paie en jeu, pas seulement en pièces. Le coffre porte plus mais
    /// ralentit ; la torche éclaire mais se voit ; la lame frappe fort mais
    /// lentement. Rien ici n'est un pur bonus, sinon la boutique deviendrait un
    /// interrupteur « rendre le jeu facile ».
    ///
    /// Statique et sauvegardée par PlayerPrefs : suffisant pour un jeu de canapé
    /// où c'est la machine qui possède la partie, pas un compte en ligne.
    /// </summary>
    public static class Boutique
    {
        const string CleOr      = "dd_or";
        const string CleAchats  = "dd_achats";
        const string ClePersos  = "dd_persos";
        const string CleChoisi  = "dd_choisi";

        public static readonly List<ArticleDef> Equipements = new List<ArticleDef>
        {
            new ArticleDef { id="besace",   nom="Coffre de portage", prix=800,
                gain="+15 de capacité.",
                cout="−6 % de vitesse, même à vide.",
                accent = Palette.Bois },
            new ArticleDef { id="lanterne", nom="Torche d'huile", prix=600,
                gain="+35 % de portée de lumière.",
                cout="+20 % de rayon de détection : on vous voit aussi.",
                accent = Palette.Soleil },
            new ArticleDef { id="rouage",   nom="Rouage de marche", prix=700,
                gain="+12 % de vitesse.",
                cout="−10 de capacité, et vos pas claquent.",
                accent = Palette.Ciel },
            new ArticleDef { id="lame",     nom="Lame affûtée", prix=900,
                gain="+35 % de dégâts.",
                cout="+18 % de temps entre deux coups.",
                accent = Palette.Rouille },
            new ArticleDef { id="bouclier", nom="Bouclier bosselé", prix=1000,
                gain="−25 % de dégâts reçus.",
                cout="−8 % de vitesse : il pèse.",
                accent = Palette.Feuille },
        };

        public static readonly List<ArticleDef> Personnages = new List<ArticleDef>
        {
            new ArticleDef { id="Knight",       nom="Le chevalier",   prix=0,   accent = Palette.Soleil },
            new ArticleDef { id="Barbarian",    nom="Le barbare",     prix=400, accent = Palette.Rouille },
            new ArticleDef { id="Ranger",       nom="La rôdeuse",     prix=400, accent = Palette.Ciel },
            new ArticleDef { id="Rogue_Hooded", nom="L'encapuchonné", prix=600, accent = Palette.Feuille },
        };

        static HashSet<string> achats;
        static HashSet<string> persos;

        static void Charger()
        {
            if (achats != null) return;
            achats = Decouper(PlayerPrefs.GetString(CleAchats, ""));
            persos = Decouper(PlayerPrefs.GetString(ClePersos, "Knight"));
            persos.Add("Knight");                     // toujours offert
        }

        static HashSet<string> Decouper(string s)
        {
            var h = new HashSet<string>();
            foreach (var p in s.Split(',')) if (!string.IsNullOrEmpty(p)) h.Add(p);
            return h;
        }

        public static int Or
        {
            get => PlayerPrefs.GetInt(CleOr, 0);
            set { PlayerPrefs.SetInt(CleOr, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static void Crediter(int pieces) => Or += pieces;

        public static bool Possede(string id)
        {
            Charger();
            return achats.Contains(id);
        }

        public static bool PersonnageDebloque(string id)
        {
            Charger();
            return persos.Contains(id);
        }

        public static string PersonnageChoisi
        {
            get => PlayerPrefs.GetString(CleChoisi, "Knight");
            set { PlayerPrefs.SetString(CleChoisi, value); PlayerPrefs.Save(); }
        }

        public static bool Acheter(string id)
        {
            Charger();
            var a = Equipements.Find(e => e.id == id);
            if (a != null)
            {
                if (achats.Contains(id) || Or < a.prix) return false;
                Or -= a.prix;
                achats.Add(id);
                PlayerPrefs.SetString(CleAchats, string.Join(",", new List<string>(achats).ToArray()));
                PlayerPrefs.Save();
                return true;
            }

            var p = Personnages.Find(e => e.id == id);
            if (p == null || persos.Contains(id) || Or < p.prix) return false;
            Or -= p.prix;
            persos.Add(id);
            PlayerPrefs.SetString(ClePersos, string.Join(",", new List<string>(persos).ToArray()));
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Pour les tests et le menu « recommencer de zéro ».</summary>
        public static void ToutEffacer()
        {
            PlayerPrefs.DeleteKey(CleOr);
            PlayerPrefs.DeleteKey(CleAchats);
            PlayerPrefs.DeleteKey(ClePersos);
            PlayerPrefs.DeleteKey(CleChoisi);
            PlayerPrefs.Save();
            achats = null; persos = null;
        }
    }
}

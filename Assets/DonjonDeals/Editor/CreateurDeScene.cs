#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DonjonDeals.EditorTools
{
    /// <summary>
    /// Monte une scène jouable en un clic, avec des prefabs provisoires faits de
    /// cubes et de capsules.
    ///
    /// L'intention est précise : vous devez pouvoir appuyer sur Play et marcher
    /// dans le labyrinthe AVANT d'avoir importé le moindre FBX. On vérifie ainsi
    /// que la génération, les pièges et la caméra fonctionnent, indépendamment
    /// des modèles. Ensuite seulement, on remplace prefab par prefab dans le
    /// DungeonBuilder : chaque remplacement est un petit pas réversible, et si
    /// quelque chose casse, on sait exactement quoi.
    ///
    ///     Donjon Deals ▸ Créer la scène de départ
    /// </summary>
    public static class CreateurDeScene
    {
        const string Racine     = "Assets/DonjonDeals";
        const string DossierPre = Racine + "/Prefabs/Provisoires";
        const string DossierSce = Racine + "/Scenes";
        const string DossierCfg = Racine + "/Config";

        [MenuItem("Donjon Deals/Créer la scène de départ")]
        public static void Creer()
        {
            AssurerDossier(Racine + "/Prefabs");
            AssurerDossier(DossierPre);
            AssurerDossier(DossierSce);
            AssurerDossier(DossierCfg);

            var reglages = ChargerOuCreerReglages();

            // ---------------------------------------------------------- prefabs provisoires
            var sol      = Boite("Sol",   new Vector3(4f, 0.2f, 4f), new Color(0.35f, 0.30f, 0.26f), 0f);
            var mur      = Boite("Mur",   new Vector3(4f, 4f, 4f),   new Color(0.20f, 0.23f, 0.27f), 2f);
            var piques   = Piques();
            var rouleau  = Rouleau();
            var trappe   = Trappe();
            var pivotant = MurPivotant();
            var coffre   = Boite("Coffre", new Vector3(1.1f, 0.85f, 0.95f), Palette.Bois, 0.42f);
            var autel    = CreerAutel();
            var escalier = Marqueur("Escalier", Palette.Ciel);
            var puits    = Marqueur("PuitsDuFond", Palette.Soleil);
            var torche   = Torche();
            var joueur   = Joueur();
            var squelette= Squelette();

            // ---------------------------------------------------------- la scène
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.13f, 0.15f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.032f;
            RenderSettings.fogColor = Palette.Pierre;

            var lune = new GameObject("Lune").AddComponent<Light>();
            lune.type = LightType.Directional;
            lune.intensity = 0.16f;
            lune.color = new Color(0.62f, 0.70f, 0.82f);
            lune.transform.rotation = Quaternion.Euler(52f, -30f, 0f);

            var goCam = new GameObject("Caméra d'équipe");
            var cam = goCam.AddComponent<Camera>();
            cam.fieldOfView = 52f;
            cam.farClipPlane = 260f;
            cam.backgroundColor = Palette.Pierre;
            goCam.AddComponent<AudioListener>();
            var partyCam = goCam.AddComponent<PartyCamera>();
            partyCam.reglages = reglages;

            var goRun = new GameObject("Partie");
            var batisseur = goRun.AddComponent<DungeonBuilder>();
            var entrees   = goRun.AddComponent<PlayerInputRouter>();
            var run       = goRun.AddComponent<RunManager>();
            var hud       = goRun.AddComponent<HudDeSecours>();

            run.reglages   = reglages;
            run.batisseur  = batisseur;
            run.camera3e   = partyCam;
            run.entrees    = entrees;
            run.nombreDeJoueursSouhaite = 1;
            run.graineDuJour = true;
            hud.run = run;

            batisseur.prefabSol         = sol;
            batisseur.prefabMur         = mur;
            batisseur.prefabPlafond     = null;     // rien au plafond tant qu'on teste : on voit mieux
            batisseur.prefabTorche      = torche;
            batisseur.prefabPiques      = piques;
            batisseur.prefabRouleau     = rouleau;
            batisseur.prefabTrappe      = trappe;
            batisseur.prefabMurPivotant = pivotant;
            batisseur.prefabCoffre      = coffre;
            batisseur.prefabAutel       = autel;
            batisseur.prefabEscalier    = escalier;
            batisseur.prefabPuitsDuFond = puits;
            batisseur.squelettes.Add(squelette);
            batisseur.personnages.Add(new PrefabParId { id = "Knight",       prefab = joueur });
            batisseur.personnages.Add(new PrefabParId { id = "Barbarian",    prefab = joueur });
            batisseur.personnages.Add(new PrefabParId { id = "Ranger",       prefab = joueur });
            batisseur.personnages.Add(new PrefabParId { id = "Rogue_Hooded", prefab = joueur });

            // un prefab de butin par entrée du catalogue : de petits cubes colorés,
            // du cuivre terne à l'or vif — on lit la valeur à la couleur.
            foreach (var b in reglages.butin)
            {
                Color c = b.fragment ? Palette.Ciel
                        : b.relique  ? Palette.Flamme
                        : Color.Lerp(Palette.Bois, Palette.Soleil, Mathf.Clamp01(b.valeur / 310f));
                float t = b.fragment ? 0.34f : Mathf.Lerp(0.22f, 0.5f, Mathf.Clamp01(b.poids / 26f));
                var p = Boite("Butin_" + b.id, new Vector3(t, t * 0.7f, t), c, 0f, false);
                batisseur.butin.Add(new PrefabParId { id = b.id, prefab = p });
            }

            string chemin = DossierSce + "/Donjon.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, chemin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Donjon Deals] Scène créée : {chemin}\n" +
                      "Appuyez sur Play. Déplacements ZQSD ou flèches, E pour interagir, " +
                      "Espace pour esquiver, clic gauche pour frapper, R pour lâcher la pièce la plus lourde.");
            EditorUtility.DisplayDialog("Donjon Deals",
                "La scène de départ est créée et ouverte.\n\n" +
                "Appuyez sur Play : le labyrinthe est jouable avec des cubes.\n" +
                "Remplacez ensuite les prefabs un par un dans l'objet « Partie » " +
                "▸ DungeonBuilder par vos modèles KayKit.", "Compris");
        }

        // ================================================================== prefabs

        static GameObject Boite(string nom, Vector3 taille, Color couleur,
                                float hauteurAuSol, bool collision = true)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
            o.name = nom;
            o.transform.localScale = taille;
            o.transform.localPosition = new Vector3(0f, hauteurAuSol, 0f);
            Peindre(o, couleur);
            if (!collision) Object.DestroyImmediate(o.GetComponent<Collider>());
            return Enregistrer(o, nom);
        }

        static GameObject Piques()
        {
            var racine = new GameObject("Piques");
            var dalle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dalle.name = "dalle";
            dalle.transform.SetParent(racine.transform, false);
            dalle.transform.localScale = new Vector3(3.4f, 0.12f, 3.4f);
            dalle.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Peindre(dalle, new Color(0.24f, 0.20f, 0.18f));

            var lames = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lames.name = "lames";
            lames.transform.SetParent(racine.transform, false);
            lames.transform.localScale = new Vector3(2.6f, 1.5f, 2.6f);
            lames.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            Peindre(lames, Palette.Rouille);
            Object.DestroyImmediate(lames.GetComponent<Collider>());

            var lum = new GameObject("braise").AddComponent<Light>();
            lum.transform.SetParent(racine.transform, false);
            lum.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            lum.type = LightType.Point;
            lum.color = Palette.Flamme;
            lum.range = 5f;

            var st = racine.AddComponent<SpikeTrap>();
            st.lames = lames.transform;
            st.lueur = lum;
            return Enregistrer(racine, "Piques");
        }

        static GameObject Rouleau()
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            o.name = "Rouleau";
            o.transform.localScale = new Vector3(1.6f, 1.6f, 1.6f);
            o.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            o.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            Peindre(o, new Color(0.32f, 0.28f, 0.25f));
            o.AddComponent<Roller>();
            return Enregistrer(o, "Rouleau");
        }

        static GameObject Trappe()
        {
            var racine = new GameObject("Trappe");
            var battant = GameObject.CreatePrimitive(PrimitiveType.Cube);
            battant.name = "battant";
            battant.transform.SetParent(racine.transform, false);
            battant.transform.localScale = new Vector3(3.2f, 0.14f, 3.2f);
            Peindre(battant, Palette.Bois);
            Object.DestroyImmediate(battant.GetComponent<Collider>());

            var t = racine.AddComponent<Trapdoor>();
            t.battant = battant.transform;
            return Enregistrer(racine, "Trappe");
        }

        static GameObject MurPivotant()
        {
            var racine = new GameObject("MurPivotant");
            var pivot = new GameObject("pivot");
            pivot.transform.SetParent(racine.transform, false);

            var panneau = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panneau.name = "panneau";
            panneau.transform.SetParent(pivot.transform, false);
            panneau.transform.localScale = new Vector3(3.9f, 3.4f, 0.35f);
            panneau.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            Peindre(panneau, new Color(0.26f, 0.29f, 0.34f));

            var w = racine.AddComponent<RotatingWall>();
            w.pivot = pivot.transform;
            return Enregistrer(racine, "MurPivotant");
        }

        static GameObject CreerAutel()
        {
            var racine = new GameObject("Autel");
            var socle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            socle.name = "socle";
            socle.transform.SetParent(racine.transform, false);
            socle.transform.localScale = new Vector3(1.3f, 1.1f, 1.3f);
            socle.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            Peindre(socle, new Color(0.28f, 0.26f, 0.30f));

            var flamme = new GameObject("flamme").AddComponent<Light>();
            flamme.transform.SetParent(racine.transform, false);
            flamme.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            flamme.type = LightType.Point;
            flamme.color = Palette.Flamme;
            flamme.intensity = 2.4f;
            flamme.range = 9f;

            var a = racine.AddComponent<Autel>();
            a.flamme = flamme;
            return Enregistrer(racine, "Autel");
        }

        static GameObject Torche()
        {
            var racine = new GameObject("Torche");
            var l = new GameObject("feu").AddComponent<Light>();
            l.transform.SetParent(racine.transform, false);
            l.type = LightType.Point;
            l.color = new Color(1f, 0.72f, 0.38f);
            l.intensity = 2.1f;
            l.range = 11f;
            return Enregistrer(racine, "Torche");
        }

        static GameObject Marqueur(string nom, Color c)
        {
            var racine = new GameObject(nom);
            var o = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            o.name = "repere";
            o.transform.SetParent(racine.transform, false);
            o.transform.localScale = new Vector3(1.6f, 0.1f, 1.6f);
            Peindre(o, c);
            Object.DestroyImmediate(o.GetComponent<Collider>());

            var l = new GameObject("halo").AddComponent<Light>();
            l.transform.SetParent(racine.transform, false);
            l.transform.localPosition = new Vector3(0f, 2f, 0f);
            l.type = LightType.Point;
            l.color = c;
            l.intensity = 2.6f;
            l.range = 13f;

            racine.AddComponent<Sortie>();
            return Enregistrer(racine, nom);
        }

        static GameObject Joueur()
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            o.name = "Joueur";
            Object.DestroyImmediate(o.GetComponent<Collider>());
            o.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            Peindre(o, Palette.Os);

            var racine = new GameObject("JoueurProvisoire");
            o.transform.SetParent(racine.transform, false);
            o.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            // un museau, pour voir dans quel sens on regarde — indispensable
            // dès qu'on teste les commandes liées à la caméra.
            var nez = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nez.transform.SetParent(racine.transform, false);
            nez.transform.localScale = new Vector3(0.2f, 0.2f, 0.5f);
            nez.transform.localPosition = new Vector3(0f, 1.1f, 0.5f);
            Object.DestroyImmediate(nez.GetComponent<Collider>());
            Peindre(nez, Palette.Soleil);

            var cc = racine.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.9f, 0f);

            var lanterne = new GameObject("lanterne").AddComponent<Light>();
            lanterne.transform.SetParent(racine.transform, false);
            lanterne.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            lanterne.type = LightType.Point;
            lanterne.intensity = 2.2f;
            lanterne.range = 11.5f;

            var pa = racine.AddComponent<PlayerAgent>();
            pa.lanterne = lanterne;
            return Enregistrer(racine, "JoueurProvisoire");
        }

        static GameObject Squelette()
        {
            var racine = new GameObject("SqueletteProvisoire");
            var corps = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corps.transform.SetParent(racine.transform, false);
            corps.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
            corps.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            Object.DestroyImmediate(corps.GetComponent<Collider>());
            Peindre(corps, new Color(0.85f, 0.86f, 0.82f));

            var yeux = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            yeux.name = "yeux";
            yeux.transform.SetParent(racine.transform, false);
            yeux.transform.localScale = new Vector3(0.22f, 0.12f, 0.12f);
            yeux.transform.localPosition = new Vector3(0f, 1.5f, 0.28f);
            Object.DestroyImmediate(yeux.GetComponent<Collider>());
            // teinte volontairement distincte de Palette.Rouille : les matériaux
            // sont mutualisés par couleur, et on ne veut pas rendre les piques
            // émissives en même temps que les orbites.
            var braise = new Color(0.90f, 0.22f, 0.20f);
            var m = Peindre(yeux, braise);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", braise * 2.4f);
            EditorUtility.SetDirty(m);

            var cc = racine.AddComponent<CharacterController>();
            cc.height = 1.7f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.85f, 0f);

            var s = racine.AddComponent<SkeletonAI>();
            s.yeux = yeux.GetComponent<Renderer>();
            return Enregistrer(racine, "SqueletteProvisoire");
        }

        // ================================================================== outils

        /// <summary>
        /// Le matériau est enregistré comme asset, jamais laissé en mémoire :
        /// un prefab qui pointe vers un matériau non sauvegardé perd sa couleur
        /// au rechargement du projet, et on croit à un bug de rendu.
        /// Le nom encode la couleur, donc deux objets de même teinte partagent
        /// le même asset au lieu d'en créer un par instance.
        /// </summary>
        static Material Peindre(GameObject o, Color c)
        {
            string chemin = DossierPre + "/Mat_" +
                            Mathf.RoundToInt(c.r * 255) + "_" +
                            Mathf.RoundToInt(c.g * 255) + "_" +
                            Mathf.RoundToInt(c.b * 255) + ".mat";

            var existant = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (existant != null)
            {
                o.GetComponent<Renderer>().sharedMaterial = existant;
                return existant;
            }

            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");

            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.08f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);

            AssetDatabase.CreateAsset(m, chemin);
            o.GetComponent<Renderer>().sharedMaterial = m;
            return m;
        }

        static GameObject Enregistrer(GameObject o, string nom)
        {
            string chemin = DossierPre + "/" + nom + ".prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(o, chemin);
            Object.DestroyImmediate(o);
            return p;
        }

        static GameTuning ChargerOuCreerReglages()
        {
            string chemin = DossierCfg + "/ReglagesDonjonDeals.asset";
            var r = AssetDatabase.LoadAssetAtPath<GameTuning>(chemin);
            if (r != null) return r;
            r = ScriptableObject.CreateInstance<GameTuning>();
            AssetDatabase.CreateAsset(r, chemin);
            AssetDatabase.SaveAssets();
            return r;
        }

        static void AssurerDossier(string chemin)
        {
            if (AssetDatabase.IsValidFolder(chemin)) return;
            string parent = Path.GetDirectoryName(chemin).Replace('\\', '/');
            string nom = Path.GetFileName(chemin);
            AssurerDossier(parent);
            AssetDatabase.CreateFolder(parent, nom);
        }
    }
}
#endif

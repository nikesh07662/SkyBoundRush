#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SkyboundRush.Editor
{
    /// <summary>
    /// Programmatically generates custom stylized textures, prefabs, tags, and the main game scene.
    /// Can be invoked from the Editor menu or via headless command line.
    /// </summary>
    public static class SkyboundSceneBuilder
    {
        [MenuItem("Skybound Rush/Build Game Scene")]
        public static void BuildGameScene()
        {
            Debug.Log("[SkyboundSceneBuilder] Starting procedural asset and scene generation...");

            EnsureTags();
            CreateDirectories();
            GenerateSprites();

            GameObject obstaclePrefab = CreateObstaclePrefab();
            CreateMainScene(obstaclePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SkyboundSceneBuilder] Skybound Rush scene build completed successfully!");
        }

        private static void CreateDirectories()
        {
            string[] dirs = { "Assets/Sprites", "Assets/Prefabs", "Assets/Scenes", "Assets/Materials" };
            foreach (var dir in dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parent = Path.GetDirectoryName(dir).Replace("\\", "/");
                    string folder = Path.GetFileName(dir);
                    AssetDatabase.CreateFolder(parent, folder);
                }
            }
        }

        private static void EnsureTags()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            string[] requiredTags = { "Obstacle", "ScoreTrigger", "Ground" };
            foreach (var reqTag in requiredTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == reqTag)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = reqTag;
                }
            }
            tagManager.ApplyModifiedProperties();
        }

        #region Procedural Sprite Generation
        private static void GenerateSprites()
        {
            CreatePlayerSprite("Assets/Sprites/PlayerCreature.png");
            CreateCrystalSpireSprite("Assets/Sprites/CrystalSpire.png");
            CreateCloudSprite("Assets/Sprites/CloudLayer.png");
            CreateGroundSprite("Assets/Sprites/GroundClouds.png");
            CreateParticleSprite("Assets/Sprites/SparkleParticle.png");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureTextureImporter("Assets/Sprites/PlayerCreature.png", 100);
            ConfigureTextureImporter("Assets/Sprites/CrystalSpire.png", 100);
            ConfigureTextureImporter("Assets/Sprites/CloudLayer.png", 100);
            ConfigureTextureImporter("Assets/Sprites/GroundClouds.png", 100);
            ConfigureTextureImporter("Assets/Sprites/SparkleParticle.png", 100);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureTextureImporter(string path, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; // Must be Single for 1 sprite per texture
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (var obj in subAssets)
                {
                    if (obj is Sprite s) return s;
                }
            }
            return sprite;
        }

        private static void CreatePlayerSprite(string path)
        {
            int w = 128, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            Vector2 center = new Vector2(58, 64);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Body oval
                    float dx = (x - center.x) / 36.0f;
                    float dy = (y - center.y) / 28.0f;
                    float distSq = dx * dx + dy * dy;

                    // Wing shape (cheerful golden feather wing)
                    float wx = (x - 42) / 22.0f;
                    float wy = (y - 74) / 34.0f;
                    float wingDist = wx * wx + wy * wy;

                    // Beak / crest
                    float bx = (x - 96) / 18.0f;
                    float by = (y - 62) / 12.0f;
                    float beakDist = bx * bx + by * by;

                    if (distSq <= 1.0f)
                    {
                        // Optimistic sunny golden-cyan gradient
                        float t = (float)y / h;
                        Color bodyColor = Color.Lerp(new Color(0.2f, 0.85f, 1.0f), new Color(1.0f, 0.92f, 0.45f), t);
                        // Edge rim highlight
                        if (distSq > 0.80f) bodyColor = Color.white;
                        tex.SetPixel(x, y, bodyColor);
                    }
                    else if (wingDist <= 1.0f)
                    {
                        // Feather wing
                        Color wingColor = new Color(1.0f, 0.95f, 0.6f, 0.95f);
                        if (wingDist > 0.75f) wingColor = Color.white;
                        tex.SetPixel(x, y, wingColor);
                    }
                    else if (beakDist <= 1.0f && x >= 92)
                    {
                        // Cheerful orange beak
                        tex.SetPixel(x, y, new Color(1.0f, 0.65f, 0.1f));
                    }
                }
            }

            // Big cheerful eye with sparkle
            int eyeX = 76, eyeY = 72;
            for (int ey = -5; ey <= 5; ey++)
            {
                for (int ex = -5; ex <= 5; ex++)
                {
                    int rSq = ex * ex + ey * ey;
                    if (rSq <= 25) tex.SetPixel(eyeX + ex, eyeY + ey, Color.white);
                    if (rSq <= 12) tex.SetPixel(eyeX + ex + 1, eyeY + ey, new Color(0.12f, 0.15f, 0.25f));
                    // Pupil highlight glint
                    if (ex == 0 && ey == 2) tex.SetPixel(eyeX + ex, eyeY + ey, Color.white);
                }
            }

            // Cheerful blush cheek
            for (int cy = -3; cy <= 3; cy++)
            {
                for (int cx = -4; cx <= 4; cx++)
                {
                    if (cx * cx + cy * cy <= 16)
                    {
                        Color cur = tex.GetPixel(68 + cx, 54 + cy);
                        Color blush = Color.Lerp(cur, new Color(1f, 0.4f, 0.5f, 0.8f), 0.55f);
                        tex.SetPixel(68 + cx, 54 + cy, blush);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateCrystalSpireSprite(string path)
        {
            // 120 x 600 pixels (1.2 x 6.0 world units)
            int w = 120, h = 600;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            float cx = w * 0.5f;

            for (int y = 0; y < h; y++)
            {
                // y = 0 is the sharp crystal tip; y = 599 is the wide base
                float tHeight = (float)y / h;
                float halfWidth = Mathf.Lerp(8f, 56f, Mathf.Pow(tHeight, 0.55f));

                for (int x = 0; x < w; x++)
                {
                    float dist = Mathf.Abs(x - cx);
                    if (dist <= halfWidth)
                    {
                        float normX = dist / halfWidth;
                        // Bright optimistic amethyst-emerald crystal palette
                        Color baseColor = Color.Lerp(new Color(0.40f, 0.22f, 0.75f), new Color(0.15f, 0.35f, 0.65f), normX);

                        // Glowing center ridge
                        if (dist < 4.0f)
                        {
                            baseColor = Color.Lerp(baseColor, new Color(0.6f, 1.0f, 1.0f), 0.85f);
                        }
                        // Bright outer crystal edges
                        if (normX > 0.85f)
                        {
                            baseColor = new Color(0.4f, 0.95f, 1.0f, 1.0f);
                        }
                        // Sparkling radiant tip (first 40 pixels)
                        if (y < 40)
                        {
                            float tipGlow = 1f - (y / 40f);
                            baseColor = Color.Lerp(baseColor, new Color(0.85f, 1.0f, 1.0f), tipGlow * 0.9f);
                        }

                        tex.SetPixel(x, y, baseColor);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateCloudSprite(string path)
        {
            int w = 512, h = 256;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            Vector2[] puffs = {
                new Vector2(100, 100), new Vector2(180, 140), new Vector2(270, 155),
                new Vector2(360, 130), new Vector2(430, 95), new Vector2(250, 85)
            };
            float[] radii = { 70f, 95f, 105f, 85f, 65f, 80f };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float maxDensity = 0f;
                    for (int p = 0; p < puffs.Length; p++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), puffs[p]);
                        if (d < radii[p])
                        {
                            float density = 1f - (d / radii[p]);
                            if (density > maxDensity) maxDensity = density;
                        }
                    }

                    if (maxDensity > 0.05f)
                    {
                        float alpha = Mathf.SmoothStep(0f, 1f, maxDensity) * 0.9f;
                        // Warm golden sunlit cloud top, soft sky-blue underside
                        Color c = Color.Lerp(new Color(0.92f, 0.96f, 1f, alpha), new Color(1f, 1f, 0.95f, alpha), maxDensity);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateGroundSprite(string path)
        {
            int w = 256, h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (float)y / h;
                    Color c = Color.Lerp(new Color(0.15f, 0.40f, 0.70f, 1.0f), new Color(0.95f, 0.98f, 1.0f, 1.0f), t);
                    float wave = Mathf.Sin(x * 0.15f) * 0.04f;
                    if (t + wave > 0.90f) c = Color.white;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void CreateParticleSprite(string path)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(32, 32);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float dX = Mathf.Abs(x - 32);
                    float dY = Mathf.Abs(y - 32);
                    float star = Mathf.Min(dX, dY);
                    if (d < 30)
                    {
                        float alpha = Mathf.Clamp01((1f - d / 30f) * (1f - star / 12f));
                        tex.SetPixel(x, y, new Color(1f, 0.95f, 0.5f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        #endregion

        #region Prefab Creation
        private static GameObject CreateObstaclePrefab()
        {
            Sprite spireSprite = LoadSprite("Assets/Sprites/CrystalSpire.png");
            Debug.Log($"[SkyboundSceneBuilder] Loaded CrystalSpire sprite: {spireSprite != null}");

            GameObject root = new GameObject("ObstaclePair");
            Obstacle obstacle = root.AddComponent<Obstacle>();

            float defaultHalfGap = 1.7f; // 3.4 gap height
            float spireHalfHeight = 3.0f; // 6.0 height / 2

            // Top Spire (points downward toward gap)
            GameObject top = new GameObject("TopSpire");
            top.transform.SetParent(root.transform);
            top.transform.localPosition = new Vector3(0f, defaultHalfGap + spireHalfHeight, 0f);
            top.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // Inverted pointing down
            top.tag = "Obstacle";

            SpriteRenderer srTop = top.AddComponent<SpriteRenderer>();
            srTop.sprite = spireSprite;
            srTop.sortingOrder = 5;

            BoxCollider2D colTop = top.AddComponent<BoxCollider2D>();
            colTop.size = new Vector2(1.0f, 6.0f);
            colTop.offset = Vector2.zero;

            // Bottom Spire (points upward toward gap)
            GameObject bottom = new GameObject("BottomSpire");
            bottom.transform.SetParent(root.transform);
            bottom.transform.localPosition = new Vector3(0f, -(defaultHalfGap + spireHalfHeight), 0f);
            bottom.transform.localRotation = Quaternion.identity; // Points up
            bottom.tag = "Obstacle";

            SpriteRenderer srBottom = bottom.AddComponent<SpriteRenderer>();
            srBottom.sprite = spireSprite;
            srBottom.sortingOrder = 5;

            BoxCollider2D colBottom = bottom.AddComponent<BoxCollider2D>();
            colBottom.size = new Vector2(1.0f, 6.0f);
            colBottom.offset = Vector2.zero;

            // Score Trigger (between top and bottom in the gap)
            GameObject trigger = new GameObject("ScoreTrigger");
            trigger.transform.SetParent(root.transform);
            trigger.transform.localPosition = Vector3.zero;
            trigger.tag = "ScoreTrigger";

            BoxCollider2D colTrigger = trigger.AddComponent<BoxCollider2D>();
            colTrigger.isTrigger = true;
            colTrigger.size = new Vector2(0.5f, defaultHalfGap * 2.0f);

            // Wire SerializedProperties on Obstacle
            SerializedObject so = new SerializedObject(obstacle);
            so.FindProperty("topSpire").objectReferenceValue = top.transform;
            so.FindProperty("bottomSpire").objectReferenceValue = bottom.transform;
            so.FindProperty("scoreTrigger").objectReferenceValue = colTrigger;
            so.ApplyModifiedProperties();

            string prefabPath = "Assets/Prefabs/ObstaclePair.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            return savedPrefab;
        }
        #endregion

        #region Main Scene Setup
        private static void CreateMainScene(GameObject obstaclePrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load all sprites cleanly
            Sprite playerSprite = LoadSprite("Assets/Sprites/PlayerCreature.png");
            Sprite cloudSprite = LoadSprite("Assets/Sprites/CloudLayer.png");
            Sprite groundSprite = LoadSprite("Assets/Sprites/GroundClouds.png");
            Sprite particleSprite = LoadSprite("Assets/Sprites/SparkleParticle.png");

            Debug.Log($"[SkyboundSceneBuilder] Sprites: Player={playerSprite != null}, Cloud={cloudSprite != null}, Ground={groundSprite != null}");

            // 1. Camera - Warm bright sunlit sky
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.24f, 0.65f, 0.95f); // Bright optimistic daylight sky
            camObj.transform.position = new Vector3(0f, 0f, -10f);
            camObj.AddComponent<AudioListener>();

            // 2. Parallax Background
            GameObject parallaxObj = new GameObject("ParallaxController");
            ParallaxScroller parallax = parallaxObj.AddComponent<ParallaxScroller>();

            GameObject layer1A = CreateCloudTile("DistantClouds_1", cloudSprite, new Vector3(0f, 2.0f, 0f), new Vector3(2.5f, 2.0f, 1f), 0, 0.5f);
            GameObject layer1B = CreateCloudTile("DistantClouds_2", cloudSprite, new Vector3(20f, 2.0f, 0f), new Vector3(2.5f, 2.0f, 1f), 0, 0.5f);
            layer1A.transform.SetParent(parallaxObj.transform);
            layer1B.transform.SetParent(parallaxObj.transform);

            GameObject layer2A = CreateCloudTile("MidClouds_1", cloudSprite, new Vector3(0f, -0.8f, 0f), new Vector3(3.0f, 2.4f, 1f), 2, 0.8f);
            GameObject layer2B = CreateCloudTile("MidClouds_2", cloudSprite, new Vector3(20f, -0.8f, 0f), new Vector3(3.0f, 2.4f, 1f), 2, 0.8f);
            layer2A.transform.SetParent(parallaxObj.transform);
            layer2B.transform.SetParent(parallaxObj.transform);

            SerializedObject soParallax = new SerializedObject(parallax);
            SerializedProperty layersProp = soParallax.FindProperty("layers");
            layersProp.arraySize = 2;

            SerializedProperty p0 = layersProp.GetArrayElementAtIndex(0);
            p0.FindPropertyRelative("transform1").objectReferenceValue = layer1A.transform;
            p0.FindPropertyRelative("transform2").objectReferenceValue = layer1B.transform;
            p0.FindPropertyRelative("speed").floatValue = 0.8f;
            p0.FindPropertyRelative("width").floatValue = 20.0f;

            SerializedProperty p1 = layersProp.GetArrayElementAtIndex(1);
            p1.FindPropertyRelative("transform1").objectReferenceValue = layer2A.transform;
            p1.FindPropertyRelative("transform2").objectReferenceValue = layer2B.transform;
            p1.FindPropertyRelative("speed").floatValue = 1.6f;
            p1.FindPropertyRelative("width").floatValue = 20.0f;

            soParallax.ApplyModifiedProperties();

            // 3. Ground & Ceiling Colliders
            GameObject ground = new GameObject("GroundBoundary");
            ground.tag = "Ground";
            ground.transform.position = new Vector3(0f, -5.2f, 0f);
            BoxCollider2D groundCol = ground.AddComponent<BoxCollider2D>();
            groundCol.size = new Vector2(30f, 1.5f);

            GameObject groundVisual = new GameObject("GroundVisual");
            groundVisual.transform.SetParent(ground.transform);
            groundVisual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            SpriteRenderer srGround = groundVisual.AddComponent<SpriteRenderer>();
            srGround.sprite = groundSprite;
            srGround.drawMode = SpriteDrawMode.Tiled;
            srGround.size = new Vector2(30f, 2.0f);
            srGround.sortingOrder = 8;

            GameObject ceiling = new GameObject("CeilingBoundary");
            ceiling.transform.position = new Vector3(0f, 5.6f, 0f);
            BoxCollider2D ceilingCol = ceiling.AddComponent<BoxCollider2D>();
            ceilingCol.size = new Vector2(30f, 1.0f);

            // 4. Player
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(-2.2f, 0.5f, 0f);

            SpriteRenderer srPlayer = playerObj.AddComponent<SpriteRenderer>();
            srPlayer.sprite = playerSprite;
            srPlayer.sortingOrder = 10;

            CircleCollider2D playerCol = playerObj.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.42f;

            Rigidbody2D rb = playerObj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Wing Trail
            TrailRenderer trail = playerObj.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.startWidth = 0.4f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            Gradient trailGrad = new Gradient();
            trailGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.88f, 0.3f), 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = trailGrad;
            trail.sortingOrder = 9;
            trail.emitting = false;

            // Death Particle System
            GameObject deathFxObj = new GameObject("DeathFX");
            deathFxObj.transform.SetParent(playerObj.transform);
            deathFxObj.transform.localPosition = Vector3.zero;
            ParticleSystem deathPs = deathFxObj.AddComponent<ParticleSystem>();
            var psMain = deathPs.main;
            psMain.playOnAwake = false;
            psMain.loop = false;
            psMain.startLifetime = 0.6f;
            psMain.startSpeed = 6.0f;
            psMain.startSize = 0.35f;
            psMain.startColor = new Color(1f, 0.85f, 0.2f);
            var psEmission = deathPs.emission;
            psEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });
            var psShape = deathPs.shape;
            psShape.shapeType = ParticleSystemShapeType.Sphere;
            psShape.radius = 0.25f;

            PlayerController playerCtrl = playerObj.AddComponent<PlayerController>();
            SerializedObject soPlayer = new SerializedObject(playerCtrl);
            soPlayer.FindProperty("wingTrail").objectReferenceValue = trail;
            soPlayer.FindProperty("deathParticles").objectReferenceValue = deathPs;
            soPlayer.ApplyModifiedProperties();

            // 5. Obstacle Spawner - Re-load prefab from asset path
            GameObject actualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ObstaclePair.prefab");
            GameObject spawnerObj = new GameObject("ObstacleSpawner");
            ObstacleSpawner spawner = spawnerObj.AddComponent<ObstacleSpawner>();
            SerializedObject soSpawner = new SerializedObject(spawner);
            soSpawner.FindProperty("obstaclePrefab").objectReferenceValue = actualPrefab;
            soSpawner.FindProperty("spawnX").floatValue = 9.5f;
            soSpawner.FindProperty("minGapY").floatValue = -1.5f;
            soSpawner.FindProperty("maxGapY").floatValue = 1.5f;
            soSpawner.FindProperty("initialGapHeight").floatValue = 3.4f;
            soSpawner.FindProperty("minGapHeight").floatValue = 2.6f;
            soSpawner.ApplyModifiedProperties();

            // 6. Managers
            GameObject smObj = new GameObject("ScoreManager");
            ScoreManager scoreMgr = smObj.AddComponent<ScoreManager>();

            GameObject gmObj = new GameObject("GameManager");
            GameManager gameMgr = gmObj.AddComponent<GameManager>();

            // 7. UI Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // HUD Score Text
            GameObject scoreTextObj = CreateTextElement("ScoreHUD", canvasObj.transform, "0", defaultFont, 92, FontStyle.Bold, Color.white);
            RectTransform rtScore = scoreTextObj.GetComponent<RectTransform>();
            rtScore.anchorMin = new Vector2(0.5f, 1.0f);
            rtScore.anchorMax = new Vector2(0.5f, 1.0f);
            rtScore.anchoredPosition = new Vector2(0f, -80f);
            rtScore.sizeDelta = new Vector2(400f, 130f);
            AddShadowToText(scoreTextObj, new Color(0f, 0f, 0f, 0.7f), new Vector2(3f, -3f));

            // Ready Panel
            GameObject readyPanel = new GameObject("ReadyPanel");
            readyPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtReady = readyPanel.AddComponent<RectTransform>();
            rtReady.anchorMin = Vector2.zero;
            rtReady.anchorMax = Vector2.one;
            rtReady.sizeDelta = Vector2.zero;

            GameObject titleTextObj = CreateTextElement("GameTitle", readyPanel.transform, "SKYBOUND RUSH", defaultFont, 100, FontStyle.Bold, new Color(1f, 0.90f, 0.25f));
            RectTransform rtTitle = titleTextObj.GetComponent<RectTransform>();
            rtTitle.anchorMin = new Vector2(0.5f, 0.5f);
            rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
            rtTitle.anchoredPosition = new Vector2(0f, 160f);
            rtTitle.sizeDelta = new Vector2(1100f, 150f);
            AddShadowToText(titleTextObj, new Color(0f, 0.15f, 0.35f, 0.85f), new Vector2(4f, -4f));

            GameObject readyHintObj = CreateTextElement("ReadyHint", readyPanel.transform, "PRESS SPACE OR CLICK TO FLY!", defaultFont, 44, FontStyle.Bold, Color.white);
            RectTransform rtReadyHint = readyHintObj.GetComponent<RectTransform>();
            rtReadyHint.anchorMin = new Vector2(0.5f, 0.5f);
            rtReadyHint.anchorMax = new Vector2(0.5f, 0.5f);
            rtReadyHint.anchoredPosition = new Vector2(0f, -60f);
            rtReadyHint.sizeDelta = new Vector2(850f, 85f);
            AddShadowToText(readyHintObj, new Color(0f, 0.15f, 0.35f, 0.85f), new Vector2(3f, -3f));

            // Game Over Panel
            GameObject gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rtGameOver = gameOverPanel.AddComponent<RectTransform>();
            rtGameOver.anchorMin = Vector2.zero;
            rtGameOver.anchorMax = Vector2.one;
            rtGameOver.sizeDelta = Vector2.zero;
            gameOverPanel.SetActive(false);

            GameObject cardBg = new GameObject("CardBG");
            cardBg.transform.SetParent(gameOverPanel.transform, false);
            Image imgCard = cardBg.AddComponent<Image>();
            imgCard.color = new Color(0.08f, 0.14f, 0.28f, 0.92f);
            RectTransform rtCard = cardBg.GetComponent<RectTransform>();
            rtCard.anchorMin = new Vector2(0.5f, 0.5f);
            rtCard.anchorMax = new Vector2(0.5f, 0.5f);
            rtCard.sizeDelta = new Vector2(640f, 520f);

            GameObject overTitle = CreateTextElement("OverTitle", gameOverPanel.transform, "GREAT EFFORT!", defaultFont, 72, FontStyle.Bold, new Color(1f, 0.85f, 0.25f));
            RectTransform rtOverTitle = overTitle.GetComponent<RectTransform>();
            rtOverTitle.anchoredPosition = new Vector2(0f, 160f);
            rtOverTitle.sizeDelta = new Vector2(600f, 90f);
            AddShadowToText(overTitle, Color.black, new Vector2(3f, -3f));

            GameObject finalScoreObj = CreateTextElement("FinalScore", gameOverPanel.transform, "SCORE: 0", defaultFont, 48, FontStyle.Bold, Color.white);
            RectTransform rtFinalScore = finalScoreObj.GetComponent<RectTransform>();
            rtFinalScore.anchoredPosition = new Vector2(0f, 65f);
            rtFinalScore.sizeDelta = new Vector2(500f, 70f);

            GameObject bestScoreObj = CreateTextElement("BestScore", gameOverPanel.transform, "BEST: 0", defaultFont, 40, FontStyle.Normal, new Color(0.85f, 0.90f, 1.0f));
            RectTransform rtBestScore = bestScoreObj.GetComponent<RectTransform>();
            rtBestScore.anchoredPosition = new Vector2(0f, 5f);
            rtBestScore.sizeDelta = new Vector2(500f, 70f);

            GameObject badgeObj = CreateTextElement("NewBestBadge", gameOverPanel.transform, "★ NEW RECORD! ★", defaultFont, 34, FontStyle.Bold, new Color(1f, 0.90f, 0.2f));
            RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
            rtBadge.anchoredPosition = new Vector2(0f, -50f);
            rtBadge.sizeDelta = new Vector2(500f, 60f);
            badgeObj.SetActive(false);

            GameObject restartHintObj = CreateTextElement("RestartHint", gameOverPanel.transform, "PRESS SPACE OR R TO FLY AGAIN", defaultFont, 32, FontStyle.Bold, new Color(0.95f, 1.0f, 0.6f));
            RectTransform rtRestartHint = restartHintObj.GetComponent<RectTransform>();
            rtRestartHint.anchoredPosition = new Vector2(0f, -145f);
            rtRestartHint.sizeDelta = new Vector2(600f, 70f);

            // Wire UIManager
            UIManager uiMgr = canvasObj.AddComponent<UIManager>();
            SerializedObject soUI = new SerializedObject(uiMgr);
            soUI.FindProperty("scoreText").objectReferenceValue = scoreTextObj.GetComponent<Text>();
            soUI.FindProperty("readyPanel").objectReferenceValue = readyPanel;
            soUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            soUI.FindProperty("finalScoreText").objectReferenceValue = finalScoreObj.GetComponent<Text>();
            soUI.FindProperty("bestScoreText").objectReferenceValue = bestScoreObj.GetComponent<Text>();
            soUI.FindProperty("newBestBadge").objectReferenceValue = badgeObj.GetComponent<Text>();
            soUI.ApplyModifiedProperties();

            // Wire GameManager references
            SerializedObject soGM = new SerializedObject(gameMgr);
            soGM.FindProperty("player").objectReferenceValue = playerCtrl;
            soGM.FindProperty("spawner").objectReferenceValue = spawner;
            soGM.FindProperty("parallax").objectReferenceValue = parallax;
            soGM.ApplyModifiedProperties();

            // Save Scene
            string scenePath = "Assets/Scenes/MainGame.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[] {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;
        }

        private static GameObject CreateCloudTile(string name, Sprite sprite, Vector3 pos, Vector3 scale, int order, float alpha)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(1f, 1f, 1f, alpha);
            sr.sortingOrder = order;
            return obj;
        }

        private static GameObject CreateTextElement(string name, Transform parent, string text, Font font, int size, FontStyle style, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            return obj;
        }

        private static void AddShadowToText(GameObject textObj, Color shadowColor, Vector2 effectDistance)
        {
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = effectDistance;
        }
        #endregion
    }
}
#endif

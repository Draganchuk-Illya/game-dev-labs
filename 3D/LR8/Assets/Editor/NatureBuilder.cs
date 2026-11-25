using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Лабораторна 8: локація природи на Terrain (гори, пагорби, долина річки, озеро), дерева, трава, скелі, небо.
public static class NatureBuilder
{
    const string ScenePath = "Assets/Scenes/Nature.unity";
    const float Size = 800f, Height = 160f, Water = 0.2f;
    const int HRes = 513, ARes = 512, DRes = 1024;

    // ---------- допоміжне ----------
    static float Fbm(float x, float z, int oct, float seed)
    {
        float a = 1f, f = 1f, sum = 0f, norm = 0f;
        for (int i = 0; i < oct; i++) { sum += a * Mathf.PerlinNoise(x * f + seed, z * f + seed * 1.7f); norm += a; a *= 0.5f; f *= 2f; }
        return sum / norm;
    }

    static float HeightAt(float x, float z)
    {
        float n = Fbm(x * 3f, z * 3f, 5, 11.3f);
        float ridge = 1f - Mathf.Abs(2f * Fbm(x * 2.2f, z * 2.2f, 4, 47.1f) - 1f);
        float d = Vector2.Distance(new Vector2(x, z), new Vector2(0.5f, 0.5f));
        float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.52f, d));
        float h = 0.24f + 0.2f * n + 0.5f * ridge * ridge * edge + 0.06f * edge;
        // озеро
        float lx = x - 0.47f, lz = z - 0.52f;
        h -= 0.2f * Mathf.Exp(-(lx * lx + lz * lz) / 0.011f);
        // долина річки від озера на південь
        if (z < 0.55f)
        {
            float rx = x - (0.47f + 0.07f * Mathf.Sin(z * 9f));
            h -= 0.11f * Mathf.Exp(-(rx * rx) / 0.0009f) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.4f, z));
        }
        return Mathf.Clamp01(h);
    }

    static void SetF(object o, string field, object value)
    {
        var t = o.GetType();
        while (t != null)
        {
            var f = t.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (f != null) { f.SetValue(o, value); return; }
            t = t.BaseType;
        }
        Debug.LogWarning("LAB8 no field " + field);
    }

    static object GetF(object o, string field)
    {
        var t = o.GetType();
        while (t != null)
        {
            var f = t.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (f != null) return f.GetValue(o);
            t = t.BaseType;
        }
        return null;
    }

    static Texture2D Tex(string name, bool normal = false, bool alpha = false)
    {
        string path = "Assets/Textures/" + name;
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        imp.maxTextureSize = name.StartsWith("Sky") ? 4096 : 1024;
        imp.wrapMode = TextureWrapMode.Repeat;
        if (alpha) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; }
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static TerrainLayer Layer(string name, float tile)
    {
        var l = new TerrainLayer
        {
            diffuseTexture = Tex(name + "_Diffuse.jpg"),
            normalMapTexture = Tex(name + "_Normal.png", true),
            tileSize = new Vector2(tile, tile),
            smoothness = 0f, metallic = 0f
        };
        AssetDatabase.CreateAsset(l, "Assets/TerrainLayers/" + name + ".terrainlayer");
        return l;
    }

    // ---------- дерево в Tree Editor ----------
    static GameObject MakeTree(Material bark, Material leaf)
    {
        EditorApplication.ExecuteMenuItem("GameObject/3D Object/Tree");
        var tree = UnityEngine.Object.FindFirstObjectByType<Tree>();
        var data = tree.data;
        var dt = data.GetType();
        var asm = dt.Assembly;
        var branchType = asm.GetType("TreeEditor.TreeGroupBranch");
        var leafType = asm.GetType("TreeEditor.TreeGroupLeaf");
        var addGroup = dt.GetMethod("AddGroup");
        var updFreq = dt.GetMethod("UpdateFrequency");

        var root = GetF(data, "root");
        SetF(root, "groundOffset", 0.2f);
        SetF(root, "enableAmbientOcclusion", true);

        // стовбур
        var trunk = ((Array)GetF(data, "branchGroups")).GetValue(0);
        SetF(trunk, "height", new Vector2(9f, 12f));
        SetF(trunk, "radius", 0.5f);
        SetF(trunk, "materialBranch", bark);
        SetF(trunk, "crinklyness", 0.12f);
        SetF(trunk, "flareSize", 0.9f);
        SetF(trunk, "flareHeight", 0.25f);
        SetF(trunk, "seed", 1234);

        // великі гілки на стовбурі
        var big = addGroup.Invoke(data, new[] { trunk, (object)branchType });
        SetF(big, "distributionFrequency", 16);
        SetF(big, "distributionCurve", new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 0.2f), new Keyframe(1f, 1f)));
        SetF(big, "distributionScaleCurve", new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f)));
        SetF(big, "distributionPitch", 0.35f);
        SetF(big, "height", new Vector2(3.5f, 5f));
        SetF(big, "radius", 0.16f);
        SetF(big, "materialBranch", bark);
        SetF(big, "crinklyness", 0.25f);
        SetF(big, "seekBlend", 0.25f);
        updFreq.Invoke(data, new[] { GetF(big, "_uniqueID") });

        // тонкі гілочки
        var small = addGroup.Invoke(data, new[] { big, (object)branchType });
        SetF(small, "distributionFrequency", 7);
        SetF(small, "height", new Vector2(1.3f, 2.1f));
        SetF(small, "radius", 0.05f);
        SetF(small, "materialBranch", bark);
        SetF(small, "crinklyness", 0.3f);
        updFreq.Invoke(data, new[] { GetF(small, "_uniqueID") });

        // листя на тонких гілочках і на великих гілках
        foreach (var parent in new[] { small, big })
        {
            var leaves = addGroup.Invoke(data, new[] { parent, (object)leafType });
            SetF(leaves, "distributionFrequency", parent == small ? 12 : 18);
            SetF(leaves, "size", new Vector2(0.9f, 1.3f));
            SetF(leaves, "materialLeaf", leaf);
            SetF(leaves, "geometryMode", 1);
            SetF(leaves, "perpendicularAlign", 0.3f);
            updFreq.Invoke(data, new[] { GetF(leaves, "_uniqueID") });
        }

        var args = new object[] { tree.transform.worldToLocalMatrix, null };
        dt.GetMethod("UpdateMesh", new[] { typeof(Matrix4x4), typeof(Material[]).MakeByRefType() }).Invoke(data, args);
        var mats = (Material[])args[1];
        tree.GetComponent<MeshFilter>().sharedMesh = (Mesh)GetF(data, "mesh");
        tree.GetComponent<Renderer>().sharedMaterials = mats;
        EditorUtility.SetDirty(data);
        PrefabUtility.ApplyPrefabInstance(tree.gameObject, InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        string path = AssetDatabase.GetAssetPath(data);
        var mesh = tree.GetComponent<MeshFilter>().sharedMesh;
        Debug.Log("LAB8 tree: asset=" + path + " verts=" + (mesh ? mesh.vertexCount : -1) + " mats=" + (mats == null ? -1 : mats.Length));
        UnityEngine.Object.DestroyImmediate(tree.gameObject);
        AssetDatabase.RenameAsset(path, "MyTree");
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MyTree.prefab");
    }

    // ---------- проста ялинка з конусів (для різноманіття лісу) ----------
    static GameObject MakePine(Material bark, Material needles)
    {
        var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>();
        var trunkTris = new List<int>(); var leafTris = new List<int>();
        Action<float, float, float, float, List<int>> cone = (y0, y1, r0, r1, tris) =>
        {
            const int seg = 10; int start = verts.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                verts.Add(dir * r0 + Vector3.up * y0); verts.Add(dir * r1 + Vector3.up * y1);
                var n = (dir + Vector3.up * (r0 - r1) / Mathf.Max(0.01f, y1 - y0)).normalized; norms.Add(n); norms.Add(n);
                uvs.Add(new Vector2(i / (float)seg * 2f, 0)); uvs.Add(new Vector2(i / (float)seg * 2f, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int b = start + i * 2;
                tris.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 });
            }
        };
        cone(0f, 3f, 0.28f, 0.2f, trunkTris);
        cone(2.2f, 5.2f, 2.3f, 0.5f, leafTris);
        cone(4.2f, 7.4f, 1.8f, 0.35f, leafTris);
        cone(6.4f, 9.6f, 1.25f, 0.02f, leafTris);
        var mesh = new Mesh { name = "Pine" };
        mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2; mesh.SetTriangles(trunkTris, 0); mesh.SetTriangles(leafTris, 1);
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, "Assets/Models/Pine.asset");
        var go = new GameObject("Pine");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, needles };
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Models/Pine.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    // ---------- скеля: сфера, деформована шумом ----------
    static Mesh MakeRock(int index)
    {
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var src = tmp.GetComponent<MeshFilter>().sharedMesh;
        var v = src.vertices;
        for (int i = 0; i < v.Length; i++)
        {
            var p = v[i].normalized;
            float n = Fbm(p.x * 1.6f + index * 9f, p.y * 1.6f + p.z * 1.6f, 3, 5f + index);
            float m = Mathf.Round(n * 5f) / 5f * 0.5f + n * 0.5f;       // грані
            v[i] = Vector3.Scale(p * (0.35f + 0.5f * m), new Vector3(1.3f, 0.8f, 1f));
        }
        var mesh = new Mesh { name = "Rock" + index, vertices = v, triangles = src.triangles, uv = src.uv };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        UnityEngine.Object.DestroyImmediate(tmp);
        AssetDatabase.CreateAsset(mesh, "Assets/Models/Rock" + index + ".asset");
        return mesh;
    }

    static Material Standard(string name, Texture2D diffuse, Texture2D normal, Color color, float smooth, float tile)
    {
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.SetFloat("_Glossiness", smooth);
        if (diffuse) { m.mainTexture = diffuse; m.mainTextureScale = new Vector2(tile, tile); }
        if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        AssetDatabase.CreateAsset(m, "Assets/Materials/" + name + ".mat");
        return m;
    }

    [MenuItem("Lab/Build Lab 8")]
    public static void Build()
    {
        foreach (var d in new[] { "Scenes", "TerrainLayers", "Materials", "Models", "Terrain" })
        {
            AssetDatabase.DeleteAsset("Assets/" + d);
            Directory.CreateDirectory("Assets/" + d);
        }
        AssetDatabase.DeleteAsset("Assets/Tree.prefab"); AssetDatabase.DeleteAsset("Assets/MyTree.prefab");
        AssetDatabase.DeleteAsset("Assets/Tree_Textures"); AssetDatabase.DeleteAsset("Assets/MyTree_Textures");
        AssetDatabase.Refresh();
        PlayerSettings.productName = "Nature3D"; PlayerSettings.companyName = "Drahanchuk";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(8);

        // ---- матеріали дерев ----
        var barkTex = Tex("Bark.png"); var leafTex = Tex("Leaf.png", false, true);
        var bark = new Material(Shader.Find("Nature/Tree Creator Bark")) { mainTexture = barkTex };
        AssetDatabase.CreateAsset(bark, "Assets/Materials/Bark.mat");
        var leaf = new Material(Shader.Find("Nature/Tree Creator Leaves")) { mainTexture = leafTex };
        leaf.SetFloat("_Cutoff", 0.35f);
        AssetDatabase.CreateAsset(leaf, "Assets/Materials/Leaf.mat");
        var pineBark = Standard("PineBark", barkTex, null, Color.white, 0.05f, 1f);
        var needles = Standard("Needles", null, null, new Color(0.13f, 0.33f, 0.16f), 0.1f, 1f);
        var rockMat = Standard("Rock", Tex("Mossy_Rock_Diffuse.jpg"), Tex("Mossy_Rock_Normal.png", true), Color.white, 0.15f, 2f);

        GameObject myTree = null;
        try { myTree = MakeTree(bark, leaf); }
        catch (Exception e) { Debug.LogWarning("LAB8 tree editor failed: " + e); }
        var pine = MakePine(pineBark, needles);

        // ---- рельєф ----
        var td = new TerrainData { heightmapResolution = HRes, alphamapResolution = ARes };
        td.SetDetailResolution(DRes, 32);
        td.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);   // значення в карті трави = кількість кущиків у клітинці
        td.size = new Vector3(Size, Height, Size);
        var heights = new float[HRes, HRes];
        for (int z = 0; z < HRes; z++)
            for (int x = 0; x < HRes; x++)
                heights[z, x] = HeightAt(x / (float)(HRes - 1), z / (float)(HRes - 1));
        td.SetHeights(0, 0, heights);
        AssetDatabase.CreateAsset(td, "Assets/Terrain/NatureTerrain.asset");

        // ---- шари текстур: пісок, трава, ґрунт, скеля ----
        td.terrainLayers = new[] { Layer("Sand", 12f), Layer("Grass_1", 10f), Layer("Dirt_1", 12f), Layer("Cliff_1", 22f) };
        var alpha = new float[ARes, ARes, 4];
        var grassMask = new float[ARes, ARes];
        for (int z = 0; z < ARes; z++)
            for (int x = 0; x < ARes; x++)
            {
                float u = x / (float)(ARes - 1), v = z / (float)(ARes - 1);
                float h = td.GetInterpolatedHeight(u, v) / Height;
                float steep = td.GetSteepness(u, v);
                float sand = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Water + 0.005f, Water + 0.03f, h));
                float cliff = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(27f, 40f, steep));
                float high = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.68f, h));
                float patch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.7f, Fbm(u * 14f, v * 14f, 3, 90f)));
                float dirt = Mathf.Clamp01(0.6f * patch + 0.7f * high);
                float rest = (1f - sand) * (1f - cliff);
                float wSand = sand * (1f - cliff), wCliff = cliff, wDirt = rest * dirt, wGrass = rest * (1f - dirt);
                float sum = wSand + wCliff + wDirt + wGrass;
                alpha[z, x, 0] = wSand / sum; alpha[z, x, 1] = wGrass / sum; alpha[z, x, 2] = wDirt / sum; alpha[z, x, 3] = wCliff / sum;
                grassMask[z, x] = wGrass / sum;
            }
        td.SetAlphamaps(0, 0, alpha);

        // ---- дерева ----
        var protos = new List<TreePrototype>();
        if (myTree != null) protos.Add(new TreePrototype { prefab = myTree });
        protos.Add(new TreePrototype { prefab = pine });
        td.treePrototypes = protos.ToArray();
        var trees = new List<TreeInstance>();
        for (int i = 0; i < 9000 && trees.Count < 1100; i++)
        {
            float u = (float)rnd.NextDouble(), v = (float)rnd.NextDouble();
            float h = td.GetInterpolatedHeight(u, v) / Height;
            if (h < Water + 0.035f || h > 0.62f || td.GetSteepness(u, v) > 24f) continue;
            float forest = Fbm(u * 6f, v * 6f, 3, 200f);
            if (forest < 0.46f) continue;
            int proto = protos.Count == 2 ? ((rnd.NextDouble() < (h > 0.42f ? 0.2 : 0.65)) ? 0 : 1) : 0;
            float s = 0.8f + (float)rnd.NextDouble() * 0.6f;
            trees.Add(new TreeInstance
            {
                position = new Vector3(u, h, v), prototypeIndex = proto, widthScale = s, heightScale = s * (0.9f + (float)rnd.NextDouble() * 0.25f),
                rotation = (float)rnd.NextDouble() * Mathf.PI * 2f, color = Color.white, lightmapColor = Color.white
            });
        }
        td.SetTreeInstances(trees.ToArray(), true);

        // ---- трава ----
        var details = new List<DetailPrototype>();
        foreach (var g in new[] { "Grass_A.png", "Grass_B.png" })
            details.Add(new DetailPrototype
            {
                prototypeTexture = Tex(g, false, true), renderMode = DetailRenderMode.GrassBillboard, usePrototypeMesh = false,
                minWidth = 1.1f, maxWidth = 2.0f, minHeight = 0.7f, maxHeight = 1.4f, noiseSpread = 0.4f,
                healthyColor = new Color(0.55f, 0.75f, 0.35f), dryColor = new Color(0.75f, 0.72f, 0.4f)
            });
        td.detailPrototypes = details.ToArray();
        for (int layer = 0; layer < 2; layer++)
        {
            var map = new int[DRes, DRes];
            for (int z = 0; z < DRes; z++)
                for (int x = 0; x < DRes; x++)
                {
                    float u = x / (float)(DRes - 1), v = z / (float)(DRes - 1);
                    float gm = grassMask[Mathf.Min(ARes - 1, z * ARes / DRes), Mathf.Min(ARes - 1, x * ARes / DRes)];
                    if (gm < 0.6f) continue;
                    float n = Mathf.PerlinNoise(u * 60f + layer * 31f, v * 60f);
                    if (n > 0.42f) map[z, x] = n > 0.62f ? 2 : 1;
                }
            td.SetDetailLayer(0, 0, layer, map);
        }

        var terrainGo = Terrain.CreateTerrainGameObject(td);
        terrainGo.name = "Terrain";
        terrainGo.transform.position = Vector3.zero;
        var terrain = terrainGo.GetComponent<Terrain>();
        terrain.treeDistance = 1200f; terrain.treeBillboardDistance = 160f; terrain.treeCrossFadeLength = 25f;
        terrain.detailObjectDistance = 140f; terrain.detailObjectDensity = 1f;
        terrain.heightmapPixelError = 4f; terrain.basemapDistance = 400f;

        // ---- вода ----
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
        water.transform.position = new Vector3(Size / 2f, Water * Height, Size / 2f);
        water.transform.localScale = new Vector3(Size / 10f, 1f, Size / 10f);
        var wm = new Material(Shader.Find("Standard")) { color = new Color(0.10f, 0.36f, 0.48f, 0.78f) };
        wm.SetFloat("_Mode", 3f); wm.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        wm.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); wm.SetInt("_ZWrite", 0);
        wm.EnableKeyword("_ALPHAPREMULTIPLY_ON"); wm.renderQueue = 3000; wm.SetFloat("_Glossiness", 0.93f);
        AssetDatabase.CreateAsset(wm, "Assets/Materials/Water.mat");
        water.GetComponent<Renderer>().sharedMaterial = wm;

        // ---- скелі ----
        var rockMeshes = new[] { MakeRock(0), MakeRock(1), MakeRock(2) };
        var rocks = new GameObject("Rocks");
        int rockCount = 0;
        for (int i = 0; i < 4000 && rockCount < 90; i++)
        {
            float u = (float)rnd.NextDouble(), v = (float)rnd.NextDouble();
            float h = td.GetInterpolatedHeight(u, v) / Height; float steep = td.GetSteepness(u, v);
            bool shore = h > Water - 0.005f && h < Water + 0.04f;
            bool slope = steep > 18f && steep < 38f && h < 0.6f;
            if (!shore && !slope) continue;
            var r = new GameObject("Rock");
            r.transform.SetParent(rocks.transform);
            r.AddComponent<MeshFilter>().sharedMesh = rockMeshes[rnd.Next(3)];
            r.AddComponent<MeshRenderer>().sharedMaterial = rockMat;
            float s = 2.5f + (float)rnd.NextDouble() * (shore ? 4f : 9f);
            r.transform.position = new Vector3(u * Size, h * Height - s * 0.12f, v * Size);
            r.transform.localScale = Vector3.one * s;
            r.transform.rotation = Quaternion.Euler(rnd.Next(-15, 15), rnd.Next(360), rnd.Next(-15, 15));
            r.isStatic = true;
            rockCount++;
        }

        // ---- небо, світло, туман ----
        var sky = new Material(Shader.Find("Skybox/Panoramic"));
        sky.SetTexture("_MainTex", Tex("Sky.jpg"));
        sky.SetFloat("_Exposure", 1.05f);
        AssetDatabase.CreateAsset(sky, "Assets/Materials/Sky.mat");
        RenderSettings.skybox = sky;
        var sunGo = new GameObject("Directional Light");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.95f, 0.84f); sun.intensity = 1.25f; sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
        RenderSettings.ambientEquatorColor = new Color(0.5f, 0.55f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.2f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.72f, 0.8f, 0.88f); RenderSettings.fogDensity = 0.0016f;
        QualitySettings.shadowDistance = 260f;

        // ---- камера з вільним керуванням ----
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.farClipPlane = 2500f; cam.fieldOfView = 60f;
        PlaceCamera(camGo.transform, td, 0.5f, 0.2f, 22f, 0.47f, 0.52f);
        camGo.AddComponent<CameraController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("LAB8 BUILD OK trees=" + trees.Count + " (my tree " + (myTree != null) + ") rocks=" + rockCount);
    }

    static void PlaceCamera(Transform cam, TerrainData td, float u, float v, float above, float lookU, float lookV)
    {
        cam.position = new Vector3(u * Size, td.GetInterpolatedHeight(u, v) + above, v * Size);
        var target = new Vector3(lookU * Size, td.GetInterpolatedHeight(lookU, lookV) + 4f, lookV * Size);
        cam.rotation = Quaternion.LookRotation(target - cam.position);
    }

    [MenuItem("Lab/Screenshot Lab 8")]
    public static void Screenshot()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var cam = Camera.main; var td = Terrain.activeTerrain.terrainData;
        Directory.CreateDirectory("Screenshots");
        var saved = cam.transform.position; var savedRot = cam.transform.rotation;
        Shot(cam, "Screenshots/lab8_lake.png");
        PlaceCamera(cam.transform, td, 0.5f, 0.5f, 260f, 0.5f, 0.56f); Shot(cam, "Screenshots/lab8_top.png");
        PlaceCamera(cam.transform, td, 0.40f, 0.36f, 3.2f, 0.47f, 0.47f); Shot(cam, "Screenshots/lab8_forest.png");
        PlaceCamera(cam.transform, td, 0.60f, 0.62f, 9f, 0.47f, 0.52f); Shot(cam, "Screenshots/lab8_shore.png");
        // моє дерево зблизька
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MyTree.prefab");
        if (prefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            float u = 0.56f, v = 0.44f;
            inst.transform.position = new Vector3(u * Size, td.GetInterpolatedHeight(u, v), v * Size);
            cam.transform.position = inst.transform.position + new Vector3(-13f, 7f, -15f);
            cam.transform.LookAt(inst.transform.position + Vector3.up * 7f);
            Shot(cam, "Screenshots/lab8_mytree.png");
            UnityEngine.Object.DestroyImmediate(inst);
        }
        cam.transform.position = saved; cam.transform.rotation = savedRot;
        Debug.Log("LAB8 SCREENSHOT OK");
    }

    static void Shot(Camera cam, string file)
    {
        var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
        cam.targetTexture = null; RenderTexture.active = null;
        File.WriteAllBytes(file, tex.EncodeToPNG());
    }

    [MenuItem("Lab/Build Windows")]
    public static void BuildWindows()
    {
        string dir = Environment.GetEnvironmentVariable("LAB_OUT") ?? "Build";
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, dir + "/Nature3D.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("LAB8 PLAYER BUILD: " + report.summary.result);
    }
}

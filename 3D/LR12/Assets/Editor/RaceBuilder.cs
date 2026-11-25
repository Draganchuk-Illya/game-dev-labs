using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Utility;
using UnityStandardAssets.Vehicles.Car;

// Лабораторна 12: гра "Гонки". Будує трасу-кільце, вейпоінти, машини гравця і суперників, інтерфейс.
public static class RaceBuilder
{
    const string ScenePath = "Assets/Scenes/Race.unity";
    const float HalfWidth = 7f;         // половина ширини дороги
    const float WaypointStep = 12f;     // відстань між вейпоінтами

    static Texture2D Tex(string name, bool normal = false)
    {
        string path = "Assets/Textures/" + name;
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 8;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material Mat(string name, Texture2D tex, Color color, float smooth, Vector2 tile, Texture2D normal = null)
    {
        var m = new Material(Shader.Find("Standard")) { color = color };
        m.SetFloat("_Glossiness", smooth);
        if (tex) { m.mainTexture = tex; m.mainTextureScale = tile; }
        if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); m.SetTextureScale("_BumpMap", tile); }
        AssetDatabase.CreateAsset(m, "Assets/Materials/" + name + ".mat");
        return m;
    }

    // Замкнена крива траси: опорні точки на деформованому овалі, між ними сплайн Catmull-Rom
    static List<Vector3> TrackPoints()
    {
        var ctrl = new List<Vector3>();
        const int n = 16;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            float r = 150f + 42f * Mathf.Sin(2f * a + 0.6f) + 26f * Mathf.Sin(3f * a + 2.1f);
            ctrl.Add(new Vector3(1.3f * r * Mathf.Cos(a), 0f, r * Mathf.Sin(a)));
        }
        var pts = new List<Vector3>();
        const int sub = 30;
        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = ctrl[(i + n - 1) % n], p1 = ctrl[i], p2 = ctrl[(i + 1) % n], p3 = ctrl[(i + 2) % n];
            for (int k = 0; k < sub; k++)
            {
                float t = k / (float)sub, t2 = t * t, t3 = t2 * t;
                pts.Add(0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        return pts;
    }

    static Vector3 Dir(List<Vector3> p, int i) { return (p[(i + 1) % p.Count] - p[(i + p.Count - 1) % p.Count]).normalized; }
    static Vector3 Left(List<Vector3> p, int i) { return Vector3.Cross(Dir(p, i), Vector3.up).normalized; }

    // Смуга вздовж траси між двома зсувами від осі (для дороги і бар'єрів)
    static Mesh Strip(string name, List<Vector3> p, float offA, float yA, float offB, float yB, float uvLen, bool wall)
    {
        int n = p.Count;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var norm = new List<Vector3>(); var tris = new List<int>();
        // для стіни роблю два набори вершин (лицьовий і зворотний бік), для дороги один з нормаллю вгору
        int sides = wall ? 2 : 1;
        for (int side = 0; side < sides; side++)
        {
            int start = v.Count; float dist = 0f;
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                if (i > 0) dist += Vector3.Distance(p[k], p[(i - 1) % n]);
                Vector3 l = Left(p, k);
                v.Add(p[k] + l * offA + Vector3.up * yA); v.Add(p[k] + l * offB + Vector3.up * yB);
                uv.Add(new Vector2(0f, dist / uvLen)); uv.Add(new Vector2(1f, dist / uvLen));
                Vector3 nn = wall ? (side == 0 ? l : -l) : Vector3.up;
                norm.Add(nn); norm.Add(nn);
            }
            for (int i = 0; i < n; i++)
            {
                int b = start + i * 2;
                // порядок вершин підбираю так, щоб трикутник дивився в бік нормалі
                Vector3 face = Vector3.Cross(v[b + 2] - v[b], v[b + 1] - v[b]);
                bool ok = Vector3.Dot(face, norm[b]) > 0f;
                if (ok) tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                else tris.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetNormals(norm); mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, "Assets/Models/" + name + ".asset");
        return mesh;
    }

    static GameObject MeshObject(string name, Mesh mesh, Material mat, Transform parent, bool collider)
    {
        var go = new GameObject(name);
        if (parent) go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        go.isStatic = true;
        return go;
    }

    // Проста ялинка зі зрізаних конусів
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
                var nn = (dir + Vector3.up * (r0 - r1) / Mathf.Max(0.01f, y1 - y0)).normalized; norms.Add(nn); norms.Add(nn);
                uvs.Add(new Vector2(i / (float)seg * 2f, 0)); uvs.Add(new Vector2(i / (float)seg * 2f, 1));
            }
            for (int i = 0; i < seg; i++) { int b = start + i * 2; tris.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 }); }
        };
        cone(0f, 3f, 0.28f, 0.2f, trunkTris); cone(2.2f, 5.2f, 2.3f, 0.5f, leafTris); cone(4.2f, 7.4f, 1.8f, 0.35f, leafTris); cone(6.4f, 9.6f, 1.25f, 0.02f, leafTris);
        var mesh = new Mesh { name = "Pine" };
        mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2; mesh.SetTriangles(trunkTris, 0); mesh.SetTriangles(leafTris, 1); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, "Assets/Models/Pine.asset");
        var go = new GameObject("Pine");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, needles };
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Models/Pine.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static float DistToTrack(List<Vector3> p, Vector3 pos)
    {
        float best = float.MaxValue;
        for (int i = 0; i < p.Count; i += 3) best = Mathf.Min(best, (p[i] - pos).sqrMagnitude);
        return Mathf.Sqrt(best);
    }

    static GameObject Car(string prefabName, string name, Vector3 pos, Quaternion rot, Color color, float topSpeed, Material bodySource)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Standard Assets/Vehicles/Car/Prefabs/" + prefabName + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name; go.transform.position = pos; go.transform.rotation = rot;
        // свій колір кузова для кожної машини
        var body = new Material(bodySource) { color = color };
        AssetDatabase.CreateAsset(body, "Assets/Materials/CarBody_" + name + ".mat");
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.StartsWith("SkyCarBody")) { mats[i] = body; changed = true; }
            if (changed) r.sharedMaterials = mats;
        }
        var so = new SerializedObject(go.GetComponent<CarController>());
        so.FindProperty("m_Topspeed").floatValue = topSpeed;
        so.ApplyModifiedPropertiesWithoutUndo();
        // інтерполяція згладжує рух між кроками фізики, без неї машина і камера смикаються
        var rb = go.GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        return go;
    }

    static Text MakeText(Transform canvas, Font font, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fs, TextAnchor align, Color color)
    {
        var go = new GameObject(name); go.transform.SetParent(canvas, false);
        var t = go.AddComponent<Text>(); t.font = font; t.fontSize = fs; t.alignment = align; t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = go.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, 0.85f); o.effectDistance = new Vector2(1.5f, -1.5f);
        var rt = t.rectTransform; rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
        return t;
    }

    [MenuItem("Lab/Build Lab 12")]
    public static void Build()
    {
        foreach (var d in new[] { "Scenes", "Materials", "Models" }) { AssetDatabase.DeleteAsset("Assets/" + d); Directory.CreateDirectory("Assets/" + d); }
        AssetDatabase.Refresh();
        PlayerSettings.productName = "Racing3D"; PlayerSettings.companyName = "Drahanchuk";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;

        // старі текстури Standard Assets імпортуються без альфа-каналу
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Standard Assets" }))
        {
            var ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (ti == null || !ti.DoesSourceTextureHaveAlpha() || ti.textureType == TextureImporterType.NormalMap) continue;
            if (ti.alphaSource == TextureImporterAlphaSource.FromInput && ti.alphaIsTransparency) continue;
            ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = true; ti.SaveAndReimport();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var rnd = new System.Random(12);

        var grass = Mat("Grass", Tex("Grass_1_Diffuse.jpg"), Color.white, 0.02f, new Vector2(90, 90), Tex("Grass_1_Normal.png", true));
        var road = Mat("Road", Tex("Road.png"), Color.white, 0.2f, Vector2.one);
        var barrier = Mat("Barrier", Tex("Barrier.png"), Color.white, 0.25f, Vector2.one);
        var checker = Mat("Checker", Tex("Checker.png"), Color.white, 0.3f, new Vector2(7, 1));
        var building = Mat("Building", Tex("Building.png"), Color.white, 0.3f, Vector2.one);
        var dirt = Mat("Dirt", Tex("Dirt_1_Diffuse.jpg"), Color.white, 0.02f, new Vector2(1, 1), Tex("Dirt_1_Normal.png", true));
        var bark = Mat("Bark", Tex("Bark.png"), Color.white, 0.05f, Vector2.one);
        var needles = Mat("Needles", null, new Color(0.13f, 0.33f, 0.16f), 0.1f, Vector2.one);
        var pine = MakePine(bark, needles);

        var pts = TrackPoints();
        int n = pts.Count;
        float length = 0f; for (int i = 0; i < n; i++) length += Vector3.Distance(pts[i], pts[(i + 1) % n]);

        // ---- земля ----
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground"; ground.transform.localScale = new Vector3(90f, 1f, 90f); ground.isStatic = true;
        ground.GetComponent<Renderer>().sharedMaterial = grass;

        // ---- траса: дорога, узбіччя, бар'єри, лінія старту ----
        var track = new GameObject("Track");
        MeshObject("Road", Strip("Road", pts, HalfWidth, 0.04f, -HalfWidth, 0.04f, 14f, false), road, track.transform, false);
        MeshObject("ShoulderLeft", Strip("ShoulderLeft", pts, HalfWidth + 2.6f, 0.02f, HalfWidth, 0.03f, 6f, false), dirt, track.transform, false);
        MeshObject("ShoulderRight", Strip("ShoulderRight", pts, -HalfWidth, 0.03f, -HalfWidth - 2.6f, 0.02f, 6f, false), dirt, track.transform, false);
        MeshObject("BarrierLeft", Strip("BarrierLeft", pts, HalfWidth + 2.8f, 0f, HalfWidth + 2.8f, 1.1f, 4f, true), barrier, track.transform, true);
        MeshObject("BarrierRight", Strip("BarrierRight", pts, -HalfWidth - 2.8f, 0f, -HalfWidth - 2.8f, 1.1f, 4f, true), barrier, track.transform, true);

        var line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = "StartLine"; UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
        line.transform.SetParent(track.transform);
        line.transform.position = pts[0] + Vector3.up * 0.06f;
        line.transform.rotation = Quaternion.LookRotation(Vector3.down, Dir(pts, 0));
        line.transform.localScale = new Vector3(HalfWidth * 2f, 2f, 1f);
        line.GetComponent<Renderer>().sharedMaterial = checker;

        // арка над стартом
        var arch = new GameObject("StartArch"); arch.transform.SetParent(track.transform);
        Vector3 l0 = Left(pts, 0);
        foreach (float side in new[] { -1f, 1f })
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube); post.transform.SetParent(arch.transform);
            post.transform.position = pts[0] + l0 * side * (HalfWidth + 2f) + Vector3.up * 3.5f;
            post.transform.localScale = new Vector3(0.7f, 7f, 0.7f); post.GetComponent<Renderer>().sharedMaterial = barrier;
        }
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube); beam.transform.SetParent(arch.transform);
        beam.transform.position = pts[0] + Vector3.up * 7.2f;
        beam.transform.rotation = Quaternion.LookRotation(Dir(pts, 0), Vector3.up);
        beam.transform.localScale = new Vector3((HalfWidth + 2f) * 2f + 0.7f, 1.2f, 0.7f); beam.GetComponent<Renderer>().sharedMaterial = checker;
        UnityEngine.Object.DestroyImmediate(beam.GetComponent<Collider>());

        // ---- вейпоінти ----
        var wpRoot = new GameObject("Waypoints");
        var circuit = wpRoot.AddComponent<WaypointCircuit>();
        var wps = new List<Transform>();
        float acc = 0f;
        for (int i = 0; i < n; i++)
        {
            if (i == 0 || acc >= WaypointStep)
            {
                var w = new GameObject("Waypoint " + wps.Count.ToString("000"));
                w.transform.SetParent(wpRoot.transform);
                w.transform.position = pts[i] + Vector3.up * 0.2f;
                w.transform.rotation = Quaternion.LookRotation(Dir(pts, i));
                wps.Add(w.transform); acc = 0f;
            }
            acc += Vector3.Distance(pts[i], pts[(i + 1) % n]);
        }
        circuit.waypointList.circuit = circuit;
        circuit.waypointList.items = wps.ToArray();

        // ---- оточення: дерева і будинки ----
        var decor = new GameObject("Decor");
        int trees = 0, houses = 0;
        for (int i = 0; i < 6000 && trees < 260; i++)
        {
            var pos = new Vector3(rnd.Next(-330, 330), 0f, rnd.Next(-260, 260));
            float d = DistToTrack(pts, pos);
            if (d < HalfWidth + 9f || (d > 70f && rnd.Next(3) > 0)) continue;
            var t = (GameObject)PrefabUtility.InstantiatePrefab(pine);
            t.transform.SetParent(decor.transform); t.transform.position = pos;
            float s = 0.9f + (float)rnd.NextDouble() * 0.9f; t.transform.localScale = Vector3.one * s;
            t.transform.rotation = Quaternion.Euler(0, rnd.Next(360), 0); t.isStatic = true; trees++;
        }
        for (int i = 0; i < 4000 && houses < 34; i++)
        {
            var pos = new Vector3(rnd.Next(-300, 300), 0f, rnd.Next(-230, 230));
            float d = DistToTrack(pts, pos);
            if (d < HalfWidth + 22f || d > 90f) continue;
            float w = 10f + rnd.Next(0, 10), h = 8f + rnd.Next(0, 22), dd = 10f + rnd.Next(0, 10);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "Building"; b.transform.SetParent(decor.transform);
            b.transform.position = pos + Vector3.up * h / 2f; b.transform.localScale = new Vector3(w, h, dd);
            b.transform.rotation = Quaternion.Euler(0, rnd.Next(4) * 90 + rnd.Next(-10, 10), 0);
            var bm = new Material(building); bm.mainTextureScale = new Vector2(Mathf.Round(w / 4f), Mathf.Round(h / 4f));
            bm.color = Color.Lerp(new Color(0.85f, 0.8f, 0.72f), new Color(0.7f, 0.78f, 0.88f), (float)rnd.NextDouble());
            AssetDatabase.CreateAsset(bm, "Assets/Materials/Building_" + houses + ".mat");
            b.GetComponent<Renderer>().sharedMaterial = bm; b.isStatic = true; houses++;
        }

        // ---- небо і світло ----
        var sky = new Material(Shader.Find("Skybox/Panoramic")); sky.SetTexture("_MainTex", Tex("Sky.jpg"));
        AssetDatabase.CreateAsset(sky, "Assets/Materials/Sky.mat"); RenderSettings.skybox = sky;
        var sunGo = new GameObject("Directional Light"); var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, 0.96f, 0.86f); sun.intensity = 1.2f; sun.shadows = LightShadows.Soft;
        sunGo.transform.rotation = Quaternion.Euler(46f, -40f, 0f); RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f); RenderSettings.ambientEquatorColor = new Color(0.52f, 0.56f, 0.52f);
        RenderSettings.ambientGroundColor = new Color(0.27f, 0.26f, 0.22f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.9f); RenderSettings.fogDensity = 0.0012f;
        QualitySettings.shadowDistance = 160f;

        // ---- машини: стартова решітка за лінією старту ----
        var bodySource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Standard Assets/Vehicles/Car/Materials/SkyCarBodyGrey.mat");
        Debug.Log("LAB12 car body shader=" + bodySource.shader.name + " supported=" + bodySource.shader.isSupported);
        Func<float, float, Vector3> grid = (back, side) =>
        {
            int idx = n - Mathf.RoundToInt(back / (length / n));
            return pts[idx] + Left(pts, idx) * side + Vector3.up * 0.6f;
        };
        Quaternion startRot = Quaternion.LookRotation(Dir(pts, n - 8));
        var carObjects = new List<GameObject>();
        string[] names = { "Червоний", "Синій", "Жовтий" };
        Color[] colors = { new Color(0.8f, 0.12f, 0.1f), new Color(0.12f, 0.3f, 0.85f), new Color(0.95f, 0.75f, 0.1f) };
        float[] tops = { 100f, 108f, 116f };
        Vector2[] slots = { new Vector2(8f, 3f), new Vector2(8f, -3f), new Vector2(17f, 3f) };
        for (int i = 0; i < 3; i++)
        {
            var ai = Car("CarWaypointBased", "Car_" + i, grid(slots[i].x, slots[i].y), startRot, colors[i], tops[i], bodySource);
            var so = new SerializedObject(ai.GetComponent<WaypointProgressTracker>());
            so.FindProperty("circuit").objectReferenceValue = circuit; so.ApplyModifiedPropertiesWithoutUndo();
            // суперники менше скидають швидкість перед поворотами, кожен трохи по-різному
            var aiSo = new SerializedObject(ai.GetComponent<CarAIControl>());
            aiSo.FindProperty("m_CautiousSpeedFactor").floatValue = 0.28f + 0.07f * i;
            aiSo.ApplyModifiedPropertiesWithoutUndo();
            var rc = ai.AddComponent<RaceCar>(); rc.driverName = names[i]; rc.isPlayer = false;
            carObjects.Add(ai);
        }
        var playerCar = Car("Car", "PlayerCar", grid(17f, -3f), startRot, new Color(0.15f, 0.7f, 0.25f), 125f, bodySource);
        // стандартне керування замінюю своїм плавним, машині гравця додаю стійкості на дорозі
        UnityEngine.Object.DestroyImmediate(playerCar.GetComponent<CarUserControl>());
        playerCar.AddComponent<PlayerDrive>();
        var pso = new SerializedObject(playerCar.GetComponent<CarController>());
        pso.FindProperty("m_MaximumSteerAngle").floatValue = 22f;
        pso.FindProperty("m_SteerHelper").floatValue = 0.85f;
        pso.ApplyModifiedPropertiesWithoutUndo();
        var prc = playerCar.AddComponent<RaceCar>(); prc.driverName = "Гравець"; prc.isPlayer = true;
        carObjects.Add(playerCar);

        // ---- камера ----
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
        cam.farClipPlane = 1500f; cam.fieldOfView = 62f;
        camGo.transform.position = playerCar.transform.position - playerCar.transform.forward * 7.5f + Vector3.up * 3f;
        camGo.transform.LookAt(playerCar.transform.position);
        camGo.AddComponent<CarCamera>().target = playerCar.transform;
        foreach (var extra in carObjects.SelectMany(c => c.GetComponentsInChildren<AudioListener>(true))) UnityEngine.Object.DestroyImmediate(extra);

        // ---- інтерфейс ----
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
        canvasGo.AddComponent<GraphicRaycaster>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var info = MakeText(canvasGo.transform, font, "InfoText", new Vector2(0, 1), new Vector2(24, -24), new Vector2(520, 150), 30, TextAnchor.UpperLeft, Color.white);
        info.text = "Коло: 1 / 3";
        var center = MakeText(canvasGo.transform, font, "CenterText", new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(900, 240), 56, TextAnchor.MiddleCenter, new Color(1f, 0.87f, 0.3f));
        center.text = "";
        var hint = MakeText(canvasGo.transform, font, "Hint", new Vector2(0, 0), new Vector2(24, 20), new Vector2(900, 40), 20, TextAnchor.LowerLeft, Color.white);
        hint.text = "W / S - газ і гальмо, A / D - кермо, пробіл - ручне гальмо, Backspace - повернутись на трасу, R - почати заново";

        var mgrGo = new GameObject("RaceManager");
        var mgr = mgrGo.AddComponent<RaceManager>();
        mgr.circuit = circuit; mgr.cars = carObjects.Select(c => c.GetComponent<RaceCar>()).ToArray(); mgr.totalLaps = 3;
        mgr.infoText = info; mgr.centerText = center;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("LAB12 BUILD OK track length=" + length.ToString("F0") + " m, waypoints=" + wps.Count + " trees=" + trees + " buildings=" + houses + " cars=" + carObjects.Count);
    }

    [MenuItem("Lab/Build Windows")]
    public static void BuildWindows()
    {
        string dir = Environment.GetEnvironmentVariable("LAB_OUT") ?? "Build";
        var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, dir + "/Racing3D.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log("LAB12 PLAYER BUILD: " + report.summary.result);
    }
}

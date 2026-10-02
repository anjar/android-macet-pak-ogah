using System;
using System.IO;
using Macet;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Macet.Editor
{
    [InitializeOnLoad] public static class ProjectSetup
    {
        const string Root = "Assets/_Game/";
        const string ScenePath = Root + "Scenes/Game.unity";
        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode) Setup();
            };
        }
        [MenuItem("Macet/Setup Phase 1 Project")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before setup.");
            ConfigurePlayer();
            ConfigureTextShaders();
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "ScriptableObjects/MobileRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Root + "ScriptableObjects/MobileRenderer.asset");
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "ScriptableObjects/MobileURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, Root + "ScriptableObjects/MobileURP.asset");
            }
            pipeline.renderScale = 1; pipeline.msaaSampleCount = 2;
            pipeline.supportsHDR = false; pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.shadowDistance = 0;
            var pipelineSettings = new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            pipelineSettings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline = pipeline; QualitySettings.vSyncCount = 0; }
            var carMaterial = Material("Car", new Color(1, 0.76f, 0.2f));
            var glass = Material("Glass", new Color(0.1f, 0.25f, 0.32f));
            var road = Material("Road", new Color(0.29f, 0.34f, 0.35f));
            var route = Material("Route", Color.white);
            var prefab = AssetDatabase.LoadAssetAtPath<Vehicle>(Root + "Prefabs/Car.prefab");
            if (prefab == null)
            {
                var car = new GameObject("Car");
                var box = car.AddComponent<BoxCollider>(); box.size = new Vector3(0.85f, 0.8f, 1.4f);
                box.isTrigger = true;
                var rb = car.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                car.AddComponent<Vehicle>();
                Part(car.transform, "Body", new Vector3(0, -0.12f, 0), new Vector3(0.85f, 0.45f, 1.4f), carMaterial);
                Part(car.transform, "Cabin", new Vector3(0, 0.23f, -0.12f), new Vector3(0.65f, 0.3f, 0.65f), glass);
                Part(car.transform, "Front marker", new Vector3(0, 0.13f, 0.56f), new Vector3(0.6f, 0.08f, 0.16f), glass);
                prefab = PrefabUtility.SaveAsPrefabAsset(car, Root + "Prefabs/Car.prefab").GetComponent<Vehicle>();
                UnityEngine.Object.DestroyImmediate(car);
            }
            if (File.Exists(ScenePath))
            {
                AssetDatabase.SaveAssets(); Debug.Log("Macet settings refreshed; existing scene preserved."); return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.backgroundColor = new Color(0.75f, 0.84f, 0.68f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.nearClipPlane = 0.1f; camera.farClipPlane = 100;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.2f; light.shadows = LightShadows.None; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(0.7f, 0.7f, 0.7f);
            var system = new GameObject("Systems");
            var manager = system.AddComponent<GameManager>();
            manager.collisions = system.AddComponent<CollisionManager>();
            manager.traffic = system.AddComponent<TrafficManager>();
            manager.loader = system.AddComponent<LevelLoader>();
            manager.loader.carPrefab = prefab; manager.loader.roadMaterial = road; manager.loader.routeMaterial = route;
            manager.view = camera;
            manager.levels = new LevelData[3];
            for (int i = 0; i < 3; i++) manager.levels[i] = AssetDatabase.LoadAssetAtPath<LevelData>(Root + "ScriptableObjects/Levels/Level" + (i + 1) + ".asset");
            manager.ui = new GameObject("UI").AddComponent<UIManager>();
            var tap = system.AddComponent<TapInput>(); tap.view = camera; tap.traffic = manager.traffic;
            var events = new GameObject("EventSystem"); events.AddComponent<EventSystem>();
            var input = events.AddComponent<InputSystemUIInputModule>(); input.AssignDefaultActions();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorUtility.SetDirty(pipeline); AssetDatabase.SaveAssets();
            Debug.Log("Macet Phase 1 ready. Open Game scene and press Play.");
        }
        static void ConfigureTextShaders()
        {
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMPro.TMP_FontAsset).Assembly);
                string essentials = Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage");
                if (!File.Exists(essentials)) throw new FileNotFoundException("TMP Essential Resources missing. Import through Window > TextMeshPro.", essentials);
                AssetDatabase.ImportPackage(essentials, false);
            }
            // Runtime-created TMP font assets need shaders retained in Android builds.
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            var settings = new SerializedObject(assets[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in new[] { "TextMeshPro/Mobile/Distance Field", "TextMeshPro/Distance Field" })
            {
                var shader = Shader.Find(name);
                if (shader == null) throw new InvalidOperationException("Missing TMP shader: " + name);
                bool exists = false;
                for (int i = 0; i < shaders.arraySize; i++) if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
                if (!exists) { int index = shaders.arraySize; shaders.InsertArrayElementAtIndex(index); shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader; }
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "MacetStudio"; PlayerSettings.productName = "Macet!";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.macetstudio.macet");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.runInBackground = false;
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings.Length > 0)
            {
                var serialized = new SerializedObject(settings[0]);
                var property = serialized.FindProperty("activeInputHandler");
                if (property != null) { property.intValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }
        static Material Material(string name, Color color)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(name == "Route" ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Simple Lit")); material.color = color;
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
        [MenuItem("Macet/Build Android APK")]
        public static void BuildApk() { Build(false); }
        [MenuItem("Macet/Build Android AAB")]
        public static void BuildAab() { Build(true); }
        static void Build(bool bundle)
        {
            Setup();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, SDK, NDK and OpenJDK via Unity Hub.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Could not switch to Android.");
            Directory.CreateDirectory("Builds");
            bool previous = EditorUserBuildSettings.buildAppBundle;
            try
            {
                EditorUserBuildSettings.buildAppBundle = bundle;
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, target = BuildTarget.Android,
                    locationPathName = bundle ? "Builds/Macet.aab" : "Builds/Macet.apk",
                    options = bundle ? BuildOptions.None : BuildOptions.Development
                });
                if (result.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed: " + result.summary.result);
            }
            finally { EditorUserBuildSettings.buildAppBundle = previous; }
        }
    }
}

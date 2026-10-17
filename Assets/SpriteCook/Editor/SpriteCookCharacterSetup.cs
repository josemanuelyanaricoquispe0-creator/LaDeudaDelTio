using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public static class SpriteCookCharacterSetup
{
    private const string RootFolder = "Assets/SpriteCook";
    private const string MetadataPath = "Assets/SpriteCook/character_export.spritecook.json";
    private const string CharacterControllerPath = "Assets/SpriteCook/Controllers/Character.controller";
    private const string PlayerPrefabPath = "Assets/SpriteCook/Prefabs/Player.prefab";
    private const string TopdownDemoScenePath = "Assets/SpriteCook/Scenes/SpriteCookTopdownDemo.unity";
    private const string PlatformerDemoScenePath = "Assets/SpriteCook/Scenes/SpriteCookPlatformerDemo.unity";
    private static bool buildQueued;

    [Serializable]
    private class SpriteCookCharacterExportMetadata
    {
        public int version;
        public string sourceCharacterName;
        public string perspective;
        public string controllerMode;
        public string idleClipPath;
        public string idleTexturePath;
        public string walkClipPath;
        public string runClipPath;
        public string jumpClipPath;
        public string fallClipPath;
        public DirectionalAnimation[] directionalAnimations;
    }

    [Serializable]
    private class DirectionalAnimation
    {
        public string state;
        public string clipPath;
        public string texturePath;
    }

    static SpriteCookCharacterSetup()
    {
        QueueBuildIfNeeded();
    }

    public class ImportWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool relevantChange = importedAssets
                .Concat(movedAssets)
                .Any(IsTriggerAssetPath);
            if (relevantChange)
            {
                QueueBuildIfNeeded();
            }
        }
    }

    private static void QueueBuildIfNeeded()
    {
        if (!File.Exists(MetadataPath) || buildQueued)
        {
            return;
        }

        buildQueued = true;
        EditorApplication.delayCall += TryBuildCharacterExport;
    }

    private static void TryBuildCharacterExport()
    {
        buildQueued = false;

        if (!File.Exists(MetadataPath))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueBuildIfNeeded();
            return;
        }

        string metadataJson = ReadMetadataJson();
        if (string.IsNullOrEmpty(metadataJson))
        {
            return;
        }

        SpriteCookCharacterExportMetadata metadata = LoadMetadata(metadataJson);
        if (metadata == null || string.IsNullOrEmpty(metadata.idleClipPath))
        {
            return;
        }

        if (!NeedsRebuild(metadataJson))
        {
            return;
        }

        if (!AreRequiredClipsReady(metadata))
        {
            QueueBuildIfNeeded();
            return;
        }

        EnsureFolder(RootFolder, "Controllers");
        EnsureFolder(RootFolder, "Prefabs");
        EnsureFolder(RootFolder, "Scenes");

        AnimatorController controller = CreateOrUpdateController(metadata);
        if (controller == null)
        {
            return;
        }

        CreateOrUpdatePrefab(metadata, controller);
        if (metadata.controllerMode == "topdown3d")
        {
            CreateOrUpdateTopdownScene();
        }
        else
        {
            CreateOrUpdatePlatformerScene();
        }
        AssetDatabase.SaveAssets();
        PersistBuildSignature(metadataJson);
    }

    private static string ReadMetadataJson()
    {
        try
        {
            return File.ReadAllText(MetadataPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SpriteCook] Failed to read character export metadata: " + ex.Message);
            return string.Empty;
        }
    }

    private static SpriteCookCharacterExportMetadata LoadMetadata(string json)
    {
        try
        {
            return JsonUtility.FromJson<SpriteCookCharacterExportMetadata>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SpriteCook] Failed to parse character export metadata: " + ex.Message);
            return null;
        }
    }

    private static AnimatorController CreateOrUpdateController(SpriteCookCharacterExportMetadata metadata)
    {
        string controllerPath = RootFolder + "/Controllers/Character.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
        {
            stateMachine.RemoveState(childState.state);
        }

        AnimatorState idleState = AddState(stateMachine, "Idle", LoadClip(metadata.idleClipPath));
        AddState(stateMachine, "Walk", LoadClip(metadata.walkClipPath));
        AddState(stateMachine, "Run", LoadClip(metadata.runClipPath));
        AddState(stateMachine, "Jump", LoadClip(metadata.jumpClipPath));
        AddState(stateMachine, "Fall", LoadClip(metadata.fallClipPath));
        if (metadata.directionalAnimations != null)
        {
            foreach (DirectionalAnimation directional in metadata.directionalAnimations)
            {
                if (directional != null && !string.IsNullOrEmpty(directional.state))
                {
                    AddState(stateMachine, directional.state, LoadClip(directional.clipPath));
                }
            }
        }

        if (idleState != null)
        {
            stateMachine.defaultState = idleState;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
    {
        if (clip == null)
        {
            return null;
        }

        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = clip;
        return state;
    }

    private static AnimationClip LoadClip(string clipPath)
    {
        if (string.IsNullOrEmpty(clipPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    }

    private static void CreateOrUpdatePrefab(SpriteCookCharacterExportMetadata metadata, AnimatorController controller)
    {
        string prefabPath = RootFolder + "/Prefabs/Player.prefab";
        GameObject root = new GameObject("Player");

        try
        {
            SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
            Animator animator = root.AddComponent<Animator>();
            root.AddComponent<SpriteCookAnimationController>();

            animator.runtimeAnimatorController = controller;

            Sprite idleSprite = GetIdleSprite(metadata);
            if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }

            if (metadata.controllerMode == "topdown3d")
            {
                Rigidbody rigidbody = root.AddComponent<Rigidbody>();
                CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
                root.AddComponent<SpriteCookTopdownMovement>();
                rigidbody.useGravity = false;
                rigidbody.freezeRotation = true;
                Bounds bounds = idleSprite != null ? idleSprite.bounds : new Bounds(Vector3.zero, new Vector3(0.8f, 1.4f, 0.2f));
                capsuleCollider.radius = Mathf.Max(0.18f, bounds.size.x * 0.28f);
                capsuleCollider.height = Mathf.Max(0.8f, bounds.size.y * 0.82f);
                capsuleCollider.center = new Vector3(0f, capsuleCollider.height * 0.5f, 0f);
            }
            else
            {
                Rigidbody2D rigidbody2D = root.AddComponent<Rigidbody2D>();
                CapsuleCollider2D capsuleCollider = root.AddComponent<CapsuleCollider2D>();
                root.AddComponent<SpriteCookPlayerMovement>();
                rigidbody2D.gravityScale = 3f;
                rigidbody2D.freezeRotation = true;

                if (idleSprite != null)
                {
                    Bounds bounds = idleSprite.bounds;
                    float colliderWidth = Mathf.Max(0.2f, bounds.size.x * 0.55f);
                    float colliderHeight = Mathf.Max(0.4f, bounds.size.y * 0.82f);
                    capsuleCollider.size = new Vector2(colliderWidth, colliderHeight);
                    capsuleCollider.offset = new Vector2(0f, colliderHeight * 0.5f);
                }
                else
                {
                    capsuleCollider.size = new Vector2(0.6f, 1.2f);
                    capsuleCollider.offset = new Vector2(0f, 0.6f);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateOrUpdateTopdownScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "SpriteCook Demo Floor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(14f, 0.1f, 10f);
        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer != null)
        {
            Material floorMaterial = new Material(Shader.Find("Standard"));
            floorMaterial.color = new Color(0.22f, 0.25f, 0.30f, 1f);
            floorRenderer.sharedMaterial = floorMaterial;
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (player != null)
            {
                player.name = "SpriteCook Player";
                player.transform.position = new Vector3(0f, 0f, 0f);
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.50f, 0.54f, 1f);
        cameraObject.transform.position = new Vector3(0f, 8f, -7f);
        cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        EditorSceneManager.SaveScene(scene, TopdownDemoScenePath);
    }

    private static void CreateOrUpdatePlatformerScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject ground = new GameObject("SpriteCook Demo Ground");
        ground.name = "SpriteCook Demo Ground";
        ground.transform.position = new Vector3(0f, -0.35f, 0f);
        BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(12f, 0.5f);
        SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = CreateSolidSprite(new Color(0.22f, 0.25f, 0.30f, 1f));
        groundRenderer.drawMode = SpriteDrawMode.Sliced;
        groundRenderer.size = new Vector2(12f, 0.5f);
        groundRenderer.sortingOrder = -10;

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (player != null)
            {
                player.name = "SpriteCook Player";
                player.transform.position = new Vector3(0f, 0.4f, 0f);
            }
        }

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 2f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.50f, 0.54f, 1f);
        cameraObject.transform.position = new Vector3(0f, 1.5f, -10f);

        EditorSceneManager.SaveScene(scene, PlatformerDemoScenePath);
    }

    private static Sprite CreateSolidSprite(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "SpriteCook Demo Solid Pixel";
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite GetFirstSprite(AnimationClip clip)
    {
        if (clip == null)
        {
            return null;
        }

        EditorCurveBinding binding = AnimationUtility
            .GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(candidate => candidate.propertyName == "m_Sprite");
        if (string.IsNullOrEmpty(binding.propertyName))
        {
            return null;
        }

        ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        if (keyframes == null || keyframes.Length == 0)
        {
            return null;
        }

        return keyframes[0].value as Sprite;
    }

    private static Sprite GetIdleSprite(SpriteCookCharacterExportMetadata metadata)
    {
        Sprite textureSprite = LoadFirstSpriteFromTexture(metadata.idleTexturePath);
        if (textureSprite != null)
        {
            return textureSprite;
        }

        return GetFirstSprite(LoadClip(metadata.idleClipPath));
    }

    private static Sprite LoadFirstSpriteFromTexture(string texturePath)
    {
        if (string.IsNullOrEmpty(texturePath))
        {
            return null;
        }

        return AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static void EnsureFolder(string parentFolder, string childFolder)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + childFolder))
        {
            AssetDatabase.CreateFolder(parentFolder, childFolder);
        }
    }

    private static bool IsTriggerAssetPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        if (string.Equals(assetPath, MetadataPath, StringComparison.Ordinal))
        {
            return true;
        }

        return assetPath.StartsWith("Assets/SpriteCook/Animations/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/SpriteCook/Scripts/", StringComparison.Ordinal)
            || assetPath.StartsWith("Assets/SpriteCook/Editor/", StringComparison.Ordinal);
    }

    private static bool NeedsRebuild(string metadataJson)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterControllerPath);
        if (controller == null)
        {
            return true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
        {
            return true;
        }

        if (!ControllerHasExpectedStates(controller))
        {
            return true;
        }

        return EditorPrefs.GetString(GetSignatureKey(), string.Empty) != metadataJson;
    }

    private static void PersistBuildSignature(string metadataJson)
    {
        EditorPrefs.SetString(GetSignatureKey(), metadataJson);
    }

    private static string GetSignatureKey()
    {
        return "SpriteCook.CharacterExportSignature." + Application.dataPath;
    }

    private static bool AreRequiredClipsReady(SpriteCookCharacterExportMetadata metadata)
    {
        return LoadClip(metadata.idleClipPath) != null
            && LoadClip(metadata.walkClipPath) != null
            && LoadClip(metadata.runClipPath) != null
            && LoadClip(metadata.jumpClipPath) != null
            && LoadClip(metadata.fallClipPath) != null;
    }

    private static bool ControllerHasExpectedStates(AnimatorController controller)
    {
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
        {
            return false;
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        if (stateMachine == null || stateMachine.defaultState == null)
        {
            return false;
        }

        string[] expectedStates = new[] { "Idle", "Walk", "Run", "Jump", "Fall" };
        return expectedStates.All(expected =>
            stateMachine.states.Any(state => state.state != null && state.state.name == expected && state.state.motion != null)
        );
    }
}

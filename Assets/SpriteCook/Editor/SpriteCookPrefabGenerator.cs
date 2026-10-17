using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public static class SpriteCookPrefabGenerator
{
    private const string AnimationsRoot = "Assets/SpriteCook/Animations";
    private const string ControllersRoot = "Assets/SpriteCook/Controllers";
    private const string PrefabsRoot = "Assets/SpriteCook/Prefabs";
    private const string ExamplePrefabPath = "Assets/SpriteCook/Prefabs/SpriteCookExample.prefab";
    private static bool buildQueued;

    static SpriteCookPrefabGenerator()
    {
        QueueExampleBuild();
    }

    public class ImportWatcher : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool animationChanged = importedAssets
                .Concat(movedAssets)
                .Any(path => path.StartsWith(AnimationsRoot + "/", StringComparison.Ordinal) && path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase));
            if (animationChanged)
            {
                QueueExampleBuild();
            }
        }
    }

    [MenuItem("Assets/SpriteCook/Generate Prefab", true)]
    private static bool ValidateGeneratePrefab()
    {
        return GetSelectedAnimationClips().Length > 0;
    }

    [MenuItem("Assets/SpriteCook/Generate Prefab")]
    private static void GeneratePrefabFromSelection()
    {
        AnimationClip[] clips = GetSelectedAnimationClips();
        if (clips.Length == 0)
        {
            EditorUtility.DisplayDialog("SpriteCook", "Select one or more SpriteCook animation clips first.", "OK");
            return;
        }

        foreach (AnimationClip clip in clips)
        {
            CreateOrUpdatePrefabForClip(clip, SanitizeFileName(clip.name), false);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void QueueExampleBuild()
    {
        if (buildQueued)
        {
            return;
        }

        buildQueued = true;
        EditorApplication.delayCall += TryCreateExamplePrefab;
    }

    private static void TryCreateExamplePrefab()
    {
        buildQueued = false;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueExampleBuild();
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ExamplePrefabPath) != null)
        {
            return;
        }

        AnimationClip clip = FindFirstSpriteCookAnimationClip();
        if (clip == null)
        {
            return;
        }

        CreateOrUpdatePrefabForClip(clip, "SpriteCookExample", true);
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip[] GetSelectedAnimationClips()
    {
        return Selection.GetFiltered<AnimationClip>(SelectionMode.Assets)
            .Where(clip =>
            {
                string path = AssetDatabase.GetAssetPath(clip);
                return path.StartsWith(AnimationsRoot + "/", StringComparison.Ordinal) && path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(clip => AssetDatabase.GetAssetPath(clip), StringComparer.Ordinal)
            .ToArray();
    }

    private static AnimationClip FindFirstSpriteCookAnimationClip()
    {
        if (!AssetDatabase.IsValidFolder(AnimationsRoot))
        {
            return null;
        }

        return AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimationsRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
            .FirstOrDefault(clip => clip != null);
    }

    private static void CreateOrUpdatePrefabForClip(AnimationClip clip, string baseName, bool useExamplePath)
    {
        EnsureFolderPath(ControllersRoot);
        EnsureFolderPath(PrefabsRoot);

        string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(baseName) ? clip.name : baseName);
        string controllerPath = ControllersRoot + "/" + safeName + ".controller";
        string prefabPath = useExamplePath ? ExamplePrefabPath : PrefabsRoot + "/" + safeName + ".prefab";

        AnimatorController controller = CreateOrUpdateController(controllerPath, clip);
        GameObject root = new GameObject(safeName);

        try
        {
            SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
            Animator animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            Sprite firstSprite = GetFirstSprite(clip);
            if (firstSprite != null)
            {
                spriteRenderer.sprite = firstSprite;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[SpriteCook] Prefab ready: " + prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static AnimatorController CreateOrUpdateController(string controllerPath, AnimationClip clip)
    {
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

        AnimatorState state = stateMachine.AddState(clip.name);
        state.motion = clip;
        stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Sprite GetFirstSprite(AnimationClip clip)
    {
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

    private static void EnsureFolderPath(string folderPath)
    {
        string[] segments = folderPath.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }
            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitized = new string((value ?? "SpriteCookPrefab").Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrEmpty(sanitized) ? "SpriteCookPrefab" : sanitized;
    }
}

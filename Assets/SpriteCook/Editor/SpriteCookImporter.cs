using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// SpriteCook Auto-Importer for Unity 6+
/// Automatically applies appropriate import settings based on art type.
/// 
/// Pixel Art (SpriteCook/Textures/PixelArt/):
/// - Filter Mode: Point (crisp pixels)
/// - Mipmaps: Disabled
/// - Compression: None
/// 
/// Smooth/Regular Art (SpriteCook/Textures/Smooth/):
/// - Filter Mode: Bilinear (smooth scaling)
/// - Mipmaps: Enabled
/// - Compression: None (for quality)
/// </summary>
public class SpriteCookImporter : AssetPostprocessor
{
    private const string TEXTURES_ROOT = "Assets/SpriteCook/Textures/";
    private static readonly HashSet<string> PendingAnimationAssets = new HashSet<string>();
    private static bool animationBuildQueued = false;

    [Serializable]
    private class SpriteCookAnimationMetadata
    {
        public string assetKind;
        public int frameWidth;
        public int frameHeight;
        public int frameCount;
        public int fps;
        public string clipName;
        public string pivotMode;
    }

    private static string GetMetadataPath(string textureAssetPath)
    {
        return Path.ChangeExtension(textureAssetPath, ".spritecook.json");
    }

    private static SpriteCookAnimationMetadata TryReadAnimationMetadata(string textureAssetPath)
    {
        string metadataPath = GetMetadataPath(textureAssetPath);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(metadataPath);
            var metadata = JsonUtility.FromJson<SpriteCookAnimationMetadata>(json);
            if (metadata == null || metadata.assetKind != "animation")
            {
                return null;
            }

            if (metadata.frameWidth <= 0 || metadata.frameHeight <= 0 || metadata.frameCount <= 0)
            {
                return null;
            }

            metadata.fps = Mathf.Max(1, metadata.fps);
            if (string.IsNullOrWhiteSpace(metadata.clipName))
            {
                metadata.clipName = Path.GetFileNameWithoutExtension(textureAssetPath);
            }
            return metadata;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SpriteCook] Failed to read animation metadata for " + textureAssetPath + ": " + ex.Message);
            return null;
        }
    }

    void OnPreprocessTexture()
    {
        // Only process textures in the SpriteCook/Textures folder
        if (!assetPath.StartsWith(TEXTURES_ROOT, StringComparison.Ordinal)) return;
        
        TextureImporter importer = (TextureImporter)assetImporter;
        var animationMetadata = TryReadAnimationMetadata(assetPath);
        
        // Determine if this is pixel art based on subfolder
        bool isPixelArt = assetPath.StartsWith(TEXTURES_ROOT + "PixelArt/", StringComparison.Ordinal);
        
        // Set texture type to Sprite
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = animationMetadata != null ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        
        // Use Full Rect mesh type to avoid edge artifacts in atlas
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        
        if (isPixelArt)
        {
            // Pixel art specific settings
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            Debug.Log($"[SpriteCook] Applied PIXEL ART settings to: {assetPath}");
        }
        else
        {
            // Smooth/regular art settings
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            Debug.Log($"[SpriteCook] Applied SMOOTH ART settings to: {assetPath}");
        }
        
        // Common settings for both types
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.spritePixelsPerUnit = 100;
        bool useBottomCenterPivot = animationMetadata != null && animationMetadata.pivotMode == "bottom_center";
        importer.spritePivot = useBottomCenterPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);

        if (animationMetadata != null)
        {
#pragma warning disable CS0618
            var spritesheet = new List<SpriteMetaData>();
            for (int index = 0; index < animationMetadata.frameCount; index++)
            {
                spritesheet.Add(new SpriteMetaData
                {
                    name = animationMetadata.clipName + "_" + index.ToString("D2"),
                    rect = new Rect(
                        index * animationMetadata.frameWidth,
                        0,
                        animationMetadata.frameWidth,
                        animationMetadata.frameHeight
                    ),
                    alignment = (int)(useBottomCenterPivot ? SpriteAlignment.BottomCenter : SpriteAlignment.Center),
                    pivot = useBottomCenterPivot ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                });
            }
            importer.spritesheet = spritesheet.ToArray();
#pragma warning restore CS0618
            Debug.Log("[SpriteCook] Configured animation slicing for: " + assetPath);
        }
    }

    void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
    {
        if (!assetPath.StartsWith(TEXTURES_ROOT, StringComparison.Ordinal)) return;

        var animationMetadata = TryReadAnimationMetadata(assetPath);
        if (animationMetadata == null) return;

        PendingAnimationAssets.Add(assetPath);
        if (!animationBuildQueued)
        {
            animationBuildQueued = true;
            EditorApplication.delayCall += ProcessPendingAnimationAssets;
        }
    }

    private static void ProcessPendingAnimationAssets()
    {
        animationBuildQueued = false;

        foreach (string pendingAssetPath in PendingAnimationAssets.ToArray())
        {
            if (!pendingAssetPath.StartsWith(TEXTURES_ROOT, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                BuildAnimationAssets(pendingAssetPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SpriteCook] Failed to build animation assets for " + pendingAssetPath + ": " + ex.Message);
            }
        }

        PendingAnimationAssets.Clear();
    }

    private static void BuildAnimationAssets(string textureAssetPath)
    {
        var animationMetadata = TryReadAnimationMetadata(textureAssetPath);
        if (animationMetadata == null) return;

        string texturesRoot = "Assets/SpriteCook/Textures";
        string animationsRoot = "Assets/SpriteCook/Animations";
        EnsureFolder("Assets/SpriteCook", "Animations");

        string relativeFolder = Path.GetDirectoryName(textureAssetPath)?.Replace("\\", "/") ?? texturesRoot;
        if (relativeFolder.StartsWith(texturesRoot, StringComparison.OrdinalIgnoreCase))
        {
            relativeFolder = relativeFolder.Substring(texturesRoot.Length).TrimStart('/');
        }

        string targetFolder = animationsRoot;
        if (!string.IsNullOrEmpty(relativeFolder))
        {
            foreach (string segment in relativeFolder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                EnsureFolder(targetFolder, segment);
                targetFolder = targetFolder + "/" + segment;
            }
        }

        var orderedSprites = AssetDatabase.LoadAllAssetsAtPath(textureAssetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();

        if (orderedSprites.Length == 0)
        {
            Debug.LogWarning("[SpriteCook] No sliced sprites found for animation sheet: " + textureAssetPath);
            return;
        }

        string clipFileName = SanitizeFileName(animationMetadata.clipName) + ".anim";
        string clipPath = targetFolder + "/" + clipFileName;
        CreateOrUpdateAnimationClip(clipPath, orderedSprites, animationMetadata.fps);
    }

    private static void EnsureFolder(string parentFolder, string childFolder)
    {
        if (!AssetDatabase.IsValidFolder(parentFolder + "/" + childFolder))
        {
            AssetDatabase.CreateFolder(parentFolder, childFolder);
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
    }

    private static void CreateOrUpdateAnimationClip(string clipPath, Sprite[] sprites, int fps)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNewClip = clip == null;
        if (clip == null)
        {
            clip = new AnimationClip();
        }

        clip.frameRate = Mathf.Max(1, fps);

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int index = 0; index < sprites.Length; index++)
        {
            keyframes[index] = new ObjectReferenceKeyframe
            {
                time = index / clip.frameRate,
                value = sprites[index]
            };
        }

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        clip.wrapMode = WrapMode.Loop;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNewClip)
        {
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SpriteCook] Animation clip ready: " + clipPath);
        SpriteCookPrefabGenerator.QueueExampleBuild();
    }
}

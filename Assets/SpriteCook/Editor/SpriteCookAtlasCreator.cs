using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// SpriteCook Atlas Creator for Unity 6+
/// Automatically creates and updates Sprite Atlases for imported textures.
/// 
/// Creates two atlases:
/// - SpriteCookAtlas_PixelArt.spriteatlas (Point filter, no mipmaps)
/// - SpriteCookAtlas_Smooth.spriteatlas (Bilinear filter, mipmaps)
/// 
/// If all textures are the same type, only one atlas is created.
/// </summary>
public class SpriteCookAtlasCreator : AssetPostprocessor
{
    [Serializable]
    private class SpriteCookAnimationMetadata
    {
        public string assetKind;
    }

    private const string TEXTURES_FOLDER = "Assets/SpriteCook/Textures";
    private const string PIXEL_ART_FOLDER = "Assets/SpriteCook/Textures/PixelArt";
    private const string SMOOTH_FOLDER = "Assets/SpriteCook/Textures/Smooth";
    private const string PIXEL_ATLAS_PATH = "Assets/SpriteCook/SpriteCookAtlas_PixelArt.spriteatlas";
    private const string SMOOTH_ATLAS_PATH = "Assets/SpriteCook/SpriteCookAtlas_Smooth.spriteatlas";

    private static bool IsAnimationSheet(string textureAssetPath)
    {
        string metadataPath = Path.ChangeExtension(textureAssetPath, ".spritecook.json");
        if (!File.Exists(metadataPath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(metadataPath);
            var metadata = JsonUtility.FromJson<SpriteCookAnimationMetadata>(json);
            return metadata != null && metadata.assetKind == "animation";
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SpriteCook] Failed to inspect atlas metadata for " + textureAssetPath + ": " + ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// Called after all assets are imported. Creates/updates the Sprite Atlases.
    /// </summary>
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        // Check if any SpriteCook textures were imported or deleted
        bool spriteCookChanged = importedAssets
            .Concat(deletedAssets)
            .Any(path => path.StartsWith(TEXTURES_FOLDER + "/", StringComparison.Ordinal));
        
        if (!spriteCookChanged) return;
        
        // Delay execution to ensure all imports are complete
        EditorApplication.delayCall += CreateOrUpdateAtlases;
    }
    
    /// <summary>
    /// Creates or updates atlases based on what textures exist.
    /// </summary>
    private static void CreateOrUpdateAtlases()
    {
        bool hasPixelArt = GetAtlasTexturePaths(PIXEL_ART_FOLDER).Length > 0;
        bool hasSmooth = GetAtlasTexturePaths(SMOOTH_FOLDER).Length > 0;
        
        if (hasPixelArt)
        {
            CreateOrUpdateAtlas(PIXEL_ART_FOLDER, PIXEL_ATLAS_PATH, true);
        }
        
        if (hasSmooth)
        {
            CreateOrUpdateAtlas(SMOOTH_FOLDER, SMOOTH_ATLAS_PATH, false);
        }
        
        if (!hasPixelArt && !hasSmooth)
        {
            Debug.Log("[SpriteCook] No textures found in PixelArt or Smooth folders.");
        }
    }
    
    /// <summary>
    /// Creates or updates a single Sprite Atlas.
    /// </summary>
    private static void CreateOrUpdateAtlas(string texturesFolder, string atlasPath, bool isPixelArt)
    {
        string[] texturePaths = GetAtlasTexturePaths(texturesFolder);
        
        if (texturePaths.Length == 0)
        {
            // Delete atlas if it exists but no textures remain
            if (File.Exists(atlasPath))
            {
                AssetDatabase.DeleteAsset(atlasPath);
                Debug.Log($"[SpriteCook] Removed empty atlas: {atlasPath}");
            }
            return;
        }
        
        // Load or create the Sprite Atlas
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        bool isNewAtlas = atlas == null;
        
        if (isNewAtlas)
        {
            atlas = new SpriteAtlas();
            
            // Configure packing settings
            // Higher padding (4px) prevents edge bleeding artifacts
            SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = 4,  // More padding to prevent edge artifacts
                enableRotation = false,  // Don't rotate - keeps sprites predictable
                enableTightPacking = false  // Use full rectangles, not polygon bounds
            };
            atlas.SetPackingSettings(packingSettings);
            
            // Configure texture settings based on art type
            SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = !isPixelArt,  // Mipmaps only for smooth art
                sRGB = true,
                filterMode = isPixelArt ? FilterMode.Point : FilterMode.Bilinear
            };
            atlas.SetTextureSettings(textureSettings);
            
            // Configure platform settings (no compression for quality)
            TextureImporterPlatformSettings platformSettings = new TextureImporterPlatformSettings
            {
                name = "DefaultTexturePlatform",
                maxTextureSize = 4096,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
                overridden = true
            };
            atlas.SetPlatformSettings(platformSettings);
            
            string artType = isPixelArt ? "Pixel Art" : "Smooth";
            Debug.Log($"[SpriteCook] Creating new {artType} Sprite Atlas.");
        }
        
        // Clear existing objects and add fresh references
        UnityEngine.Object[] currentObjects = atlas.GetPackables();
        if (currentObjects != null && currentObjects.Length > 0)
        {
            atlas.Remove(currentObjects);
        }
        
        var packables = texturePaths
            .Select(path => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path))
            .Where(obj => obj != null)
            .ToArray();
        if (packables.Length > 0)
        {
            atlas.Add(packables);
        }
        
        // Save the atlas
        if (isNewAtlas)
        {
            string parentDir = Path.GetDirectoryName(atlasPath);
            if (!AssetDatabase.IsValidFolder(parentDir))
            {
                Directory.CreateDirectory(parentDir);
                AssetDatabase.Refresh();
            }
            
            AssetDatabase.CreateAsset(atlas, atlasPath);
            Debug.Log($"[SpriteCook] Created Sprite Atlas at: {atlasPath}");
        }
        else
        {
            EditorUtility.SetDirty(atlas);
        }
        
        AssetDatabase.SaveAssets();
        
        // Pack the atlas immediately
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        
        string type = isPixelArt ? "Pixel Art" : "Smooth";
        Debug.Log($"[SpriteCook] {type} Atlas ready with {texturePaths.Length} textures at: {atlasPath}");
    }

    private static string[] GetAtlasTexturePaths(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return Array.Empty<string>();
        }

        return AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !IsAnimationSheet(path))
            .ToArray();
    }
    
    /// <summary>
    /// Menu item to manually rebuild atlases if needed.
    /// </summary>
    [MenuItem("Assets/SpriteCook/Rebuild Sprite Atlases")]
    private static void ManualRebuildAtlases()
    {
        CreateOrUpdateAtlases();
    }
}

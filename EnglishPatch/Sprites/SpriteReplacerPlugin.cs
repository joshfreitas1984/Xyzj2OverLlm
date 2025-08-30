using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using XUnity.ResourceRedirector;

namespace EnglishPatch.Sprites
{
    [BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.SpriteReplacer", "SpriteReplacer", MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gravydevsupreme.xunity.resourceredirector")]
    public class SpriteReplacerPlugin : BaseUnityPlugin
    {
        // Internal variables
        internal static new ManualLogSource Logger;
        private Dictionary<string, byte[]> _cachedReplacements = [];
        private List<string> _cachedSpriteNames = [];
        private string _spritesPath;
        public static bool Enabled = !SpriteReplacerV2Plugin.Enabled;
        //public static bool Enabled = false;

        private void Awake()
        {
            Logger = base.Logger;

            if (!Enabled)
                return;

            _spritesPath = Path.Combine(Paths.BepInExRootPath, "sprites");

            // Cache all textures from the replacement folder
            CacheReplacementTextures();

            // Register our resource redirector
            ResourceRedirection.EnableSyncOverAsyncAssetLoads();

            ResourceRedirection.RegisterAssetLoadedHook(
                behaviour: HookBehaviour.OneCallbackPerResourceLoaded,
                priority: 1000,
                action: OnAssetLoaded);

            Logger.LogWarning("Sprite Replacer plugin patching complete!");
            Logger.LogMessage($"Watching for replacement sprites in: {_spritesPath}");
        }

        private void CacheReplacementTextures()
        {
            if (!Directory.Exists(_spritesPath))
            {
                Logger.LogWarning($"Replacement directory does not exist: {_spritesPath}");
                return;
            }

            // Get all PNG and JPG files in the replacement directory
            var imageFiles = Directory.GetFiles(_spritesPath, "*.*", SearchOption.AllDirectories)
                .ToArray();

            Logger.LogMessage($"Found {imageFiles.Length} potential replacement sprites");

            foreach (var imagePath in imageFiles)
            {
                try
                {
                    // Get relative path for key
                    var relativePath = imagePath.Substring(_spritesPath.Length + 1);

                    // Cache completed sprite names so we can log when they match
                    var spriteName = Path.GetFileNameWithoutExtension(relativePath);
                    if (!_cachedSpriteNames.Contains(spriteName))
                        _cachedSpriteNames.Add(spriteName);

                    // Load texture
                    var fileData = File.ReadAllBytes(imagePath);

                    // Remove file extension for easier matching
                    var keyName = FilePathToKey(relativePath);
                    _cachedReplacements.Add(keyName, fileData);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error caching texture {imagePath}: {ex.Message}");
                }
            }

            Logger.LogMessage($"Successfully cached {_cachedReplacements.Count} replacement sprites");
        }

        private string FilePathToKey(string filePath)
        {
            //We use the format: <LastDirectory>/<SpriteName>
            //This is because we don't know the full path of a gameobject and sprite names can be duped
            var directory = Path.GetFileName(Path.GetDirectoryName(filePath));
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            var result = string.Empty;

            result = !string.IsNullOrEmpty(directory) ? Path.Combine(directory, fileName) : fileName;

            result = result.Replace("\\", "/")
                .ToLower();

            return result;
        }

        private string PrepareSpriteKey(string assetName, string spriteName)
        {
            var output = string.Empty;

            output = $"{assetName}/{spriteName}";

            output = output
                .Replace("\\", "/")
                .ToLower();

            return output;
        }

        public void OnAssetLoaded(AssetLoadedContext context)
        {
            string parentAssetName = context.Asset?.name;
            string assetPath = context.Parameters?.Name ?? string.Empty;
            string type = context.Parameters?.Type.ToString();
            string loadType = context.Parameters?.LoadType.ToString();

            //if (_cachedSpriteNames.Contains(parentAssetName))
            //    Logger.LogError($"Loaded Matched Asset with no replacer: {parentAssetName} Path: {assetPath} Type: {type} LoadType: {loadType}");

            if (context.Asset is GameObject prefab)
            {
                var allChildren = prefab.GetComponentsInChildren<UnityEngine.UI.Image>();

                // Log game objects to make it easier to find
                //if (allChildren.Length > 0)
                //    Logger.LogMessage($"Loaded Game Object: {parentAssetName} Path: {assetPath} Type: {type} LoadType: {loadType}");

                foreach (var child in allChildren)
                {
                    ReplaceSpriteInAsset(parentAssetName, child);
                }
            }
            // Texture2D might need to come back into play for non-prefab textures but its unlikely they are used in game 
            // Needs more testing
            //else if ((context.Asset is Texture2D))
            //    Logger.LogError($"{fullPath} is a Texture2D but we disabled replacement.");
        }

        private void ReplaceSpriteInAsset(string parentAssetName, UnityEngine.UI.Image child)
        {
            if (child == null)
                return;

            if (child.sprite != null && child.sprite.name != null)
            {
                var shouldMatch = _cachedSpriteNames.Contains(child?.name) ||
                                  _cachedSpriteNames.Contains(child.sprite?.name);

                var spriteKey = PrepareSpriteKey(parentAssetName, child.sprite.name);

                if (_cachedReplacements.TryGetValue(spriteKey, out var replacementBytes))
                {
                    var originalTexture = child.sprite.texture;

                    // We use an uncompressed format to avoid issues with compression requiring specific sizes (eg. DXT1, DXT5, BC7, BC6H)
                    // Texture size doesn't matter, will be replaced by Unity in LoadImage to match texture
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false); 
                    texture.LoadImage(replacementBytes, false);


                    // Ensure preserve aspect is on to avoid stretching issues where replacement sprite dimensions differ
                    child.preserveAspect = true;

                    // Rect handling
                    var rect = child.sprite.rect;
                    if (rect.width > texture.width || rect.height > texture.height)
                    {
                        Logger.LogWarning($"{spriteKey}: Texture dimensions are smaller than sprite rect, resizing");
                        rect.width = Mathf.Min(rect.width, texture.width);
                        rect.height = Mathf.Min(rect.height, texture.height);
                    }

                    // Replace sprite
                    child.sprite = Sprite.Create(texture, rect, child.sprite.pivot, child.sprite.pixelsPerUnit);
                }
            }
        }

        //private void ProcessTexture(AssetLoadedContext context, string fullPath)
        //{
        //    if (cachedReplacements.TryGetValue(fullPath, out Texture2D replacementTexture))
        //    {
        //        Logger.LogWarning($"Replacing in place sprite: {fullPath}");

        //        // Create a new Sprite from our replacement texture
        //        if (context.Asset is Sprite originalSprite)
        //        {
        //            // For sprites, we need to create a new sprite with the same properties
        //            Sprite newSprite = Sprite.Create(
        //                replacementTexture,
        //                originalSprite.rect,
        //                originalSprite.pivot,
        //                originalSprite.pixelsPerUnit,
        //                0,
        //                SpriteMeshType.FullRect);

        //            context.Asset = newSprite;
        //        }
        //        else
        //        {
        //            // For textures, just replace with our cached texture
        //            context.Asset = replacementTexture;
        //        }
        //    }
        //}

        public void OnDestroy()
        {
            // Unregister our hook when the plugin is unloaded
            ResourceRedirection.UnregisterAssetLoadedHook(OnAssetLoaded);
        }
    }
}

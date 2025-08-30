using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EnglishPatch.Support;
using HarmonyLib;
using SharedAssembly.TextResizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.StyleSheets;

namespace EnglishPatch.Sprites;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.SpriteReplacerV2", "SpriteReplacerV2", MyPluginInfo.PLUGIN_VERSION)]
public class SpriteReplacerV2Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static bool Enabled = false;
    public static bool ContractsLoaded = false;
    public static Dictionary<string, SpriteReplacerContract> Contracts = [];
    public static Dictionary<string, SpriteReplacerContract> CachedMatchesContracts = [];
    private static string _folder;

    private ConfigEntry<bool> _devMode;
    private KeyCode _addAtCursorHotKey = KeyCode.F1;
    private KeyCode _addAllHotKey = KeyCode.F2;
    private KeyCode _reloadHotkey = KeyCode.F3;

    private void Awake()
    {
        Logger = base.Logger;

        if (!Enabled)
            return;

        _devMode = Config.Bind("General",
            "DevMode",
            false,
            "Use F1 to Dump under cursor, F2 to dump all, F3 to reload sprites");

        Harmony.CreateAndPatchAll(typeof(SpriteReplacerV2Plugin));
        Logger.LogWarning($"SpriteReplacerV2 Plugin should be patched!");

        _folder = Path.Combine(Paths.BepInExRootPath, "sprites2");
        if (!Directory.Exists(_folder))
            Directory.CreateDirectory(_folder);

        LoadContracts();

        Harmony.CreateAndPatchAll(typeof(TextResizerPlugin));
        Logger.LogWarning($"SpriteReplacerV2 Plugin Loaded!");
    }

    internal void Update()
    {
        if (!Enabled || !_devMode.Value)
            return;

        if (UnityInput.Current.GetKeyDown(_reloadHotkey))
        {
            LoadContracts();
            ApplyAllContracts();
            Logger.LogWarning("Sprite Contracts Reloaded");
        }

        if (UnityInput.Current.GetKeyDown(_addAtCursorHotKey))
        {
            Logger.LogWarning("Adding Sprite Contracts at Cursor");
            AddElementsToContracts(FindElementsAtCursor());
        }

        if (UnityInput.Current.GetKeyDown(_addAllHotKey))
        {
            Logger.LogWarning("Adding Sprite Contracts on Scene");
            AddElementsToContracts(FindAllElements());
        }
    }

    public static void ApplyAllContracts()
    {
        foreach (var element in FindAllElements())
            ReplaceSpriteInAsset(element);
    }

    public void LoadContracts()
    {
        ContractsLoaded = false;

        var deserializer = Yaml.CreateDeserializer();
        Contracts.Clear();
        CachedMatchesContracts.Clear();

        var contractFiles = Directory.EnumerateFiles(_folder, "*.yaml");
        foreach (var file in contractFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                var newContracts = deserializer.Deserialize<List<SpriteReplacerContract>>(content);
                AddFoundContracts(newContracts);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error Loading sprite contract '{file}': {ex}");
            }
        }

        ContractsLoaded = true;
    }

    private void AddFoundContracts(List<SpriteReplacerContract> newContracts)
    {
        foreach (var newContract in newContracts)
        {
            if (!Contracts.ContainsKey(newContract.Path))
            {
                Contracts.Add(newContract.Path, newContract);
                if (CachedMatchesContracts.ContainsKey(newContract.Path))
                    CachedMatchesContracts[newContract.Path] = newContract;
            }
        }
    }

    public static Image[] FindAllElements()
    {
        // Find all TextMeshProUGUI components in the scene
        return FindObjectsOfType<Image>();
    }


    public Image[] FindElementsAtCursor()
    {
        // Get the current mouse position
        var mousePosition = UnityInput.Current.mousePosition;

        // Create a 10x10 pixel area around the cursor (20 pixel buffer on each side)
        var cursorArea = new Rect(mousePosition.x - 10, mousePosition.y - 10, 20, 20);

        // Find all elements in the scene
        var elements = FindObjectsOfType<Image>();

        var responseElements = new List<Image>();

        foreach (var element in elements)
        {
            // Get the RectTransform to check if it contains the cursor position
            var rectTransform = element.rectTransform;
            if (rectTransform == null)
                continue;

            // Check if the text element's screen rect overlaps with our cursor area
            var canvas = element.canvas;
            if (canvas == null)
                continue;

            // Get the screen rect of the text element
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var screenRect = RectTransformUtility.PixelAdjustRect(rectTransform, canvas);

            // Convert the rect to screen coordinates if not in overlay mode
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && camera != null)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                // Convert world corners to screen points
                var min = camera.WorldToScreenPoint(corners[0]);
                var max = camera.WorldToScreenPoint(corners[2]);
                screenRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
            }

            // Check if the cursor area overlaps with the text element's screen rect
            if (screenRect.Overlaps(cursorArea))
                responseElements.Add(element);
        }

        return responseElements.ToArray();
    }

    public void AddElementsToContracts(Image[] elements, bool addUnderCursor = false, bool copyUnderCursor = false)
    {
        var foundContracts = new List<SpriteReplacerContract>();

        foreach (var element in elements)
        {
            //Logger.LogWarning($"Found Image Element: {element.name}");

            if (element.sprite == null)
                continue;

            //Logger.LogWarning($"Found Image Sprite: {element.sprite.name}");

            // Log information about the text element
            var path = ObjectHelper.GetGameObjectPath(element.gameObject);
            var spriteName = element.sprite.name;

            if (!Contracts.ContainsKey(path))
            {
                // Create a new resizer contract for this text element
                var newContract = new SpriteReplacerContract()
                {
                    Path = path,
                    ReplacementSprite = CalculateReplacement(spriteName, path),
                };

                var spritePath = $"{_folder}/dumped/{newContract.ReplacementSprite}";
                //Logger.LogWarning($"Found Sprite Path: {spritePath}");

                if (!File.Exists(spritePath))
                {
                    var texture = element.sprite.texture;
                    byte[] bytes;

                    if (texture.isReadable)
                    {
                        bytes = texture.GetRawTextureData();
                    }
                    else
                    {
                        // Create a temporary readable texture
                        var readableTexture = new Texture2D(texture.width, texture.height, texture.format, false);
                        var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);

                        Graphics.Blit(texture, renderTexture);
                        RenderTexture.active = renderTexture;
                        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                        readableTexture.Apply();

                        RenderTexture.active = null;
                        RenderTexture.ReleaseTemporary(renderTexture);

                        bytes = readableTexture.EncodeToPNG();
                        Destroy(readableTexture);
                    }

                    if (bytes == null || bytes.Length == 0)
                    {
                        //Logger.LogError($"Empty or null sprite data for: {newContract.ReplacementSprite}");
                        continue;
                    }

                    File.WriteAllBytes($"{spritePath}.png", bytes);

                }

                foundContracts.Add(newContract);
            }
        }

        if (foundContracts.Count > 0)
        {
            var serializer = Yaml.CreateSerializer();

            var addedContractsFile = $"{_folder}/zzAdded.yaml";
            var newText = serializer.Serialize(foundContracts);

            Logger.LogWarning($"Writing to {addedContractsFile}");

            if (!File.Exists(addedContractsFile))
                File.WriteAllText(addedContractsFile, newText);
            else
                File.AppendAllText(addedContractsFile, newText);

            AddFoundContracts(foundContracts);
        }
        else
            Logger.LogMessage("No new sprite elements found in scene");
    }

    public string CalculateReplacement(string spriteName, string objectPath)
    {
        if (Contracts.Any(c => c.Value.ReplacementSprite == spriteName))
        {
            var segments = objectPath.Split('/');
            var prefix = string.Empty;
            if (segments.Length < 3)
                prefix = objectPath;
            else
                prefix = string.Join("/", segments.Skip(segments.Length - 3));

            prefix = prefix
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(":", "")
                .Replace("/", "_")
                .Replace("\\", "_"); ;

            spriteName = $"{prefix}_{spriteName}";
        }

        return spriteName;
    }

    public static SpriteReplacerContract FindAppropriateContract(string path)
    {
        if (Contracts.TryGetValue(path, out var tryContract))
            return tryContract;

        // Check cache first
        if (CachedMatchesContracts.TryGetValue(path, out var cachedContract))
            return cachedContract;

        // Try wildcard matching for the remaining resizers
        foreach (var contractPair in Contracts)
        {
            var contract = contractPair.Value;

            if (contract.Path.Contains("*"))
            {
                // Convert to Regex
                var pattern = contract.Path
                    .Replace("/", @"\/")
                    .Replace("(", @"\(")
                    .Replace(")", @"\)")
                    .Replace("*", ".*");

                if (Regex.IsMatch(path, pattern))
                    return contract;
            }
        }

        return null;
    }

    public static void ReplaceSpriteInAsset(Image image)
    {
        if (image == null)
            return;

        if (image.sprite == null)
            return;

        //Logger.LogWarning($"Checking Image: {child.name}");

        var path = image.GetObjectPath();
        var contract = FindAppropriateContract(path);

        //Logger.LogWarning($"Checking Image Path: {path} Contract: {contract}");

        // Cache matches so we only have to match once
        // We cache nulls so we don't loop multiple times over the same path
        if (!CachedMatchesContracts.ContainsKey(path))
            CachedMatchesContracts.Add(path, contract);

        if (contract == null)
            return;

        // Replace the sprite
        var spriteReplacementPath = $"{_folder}/dumped/{contract.ReplacementSprite}.png";

        if (!File.Exists(spriteReplacementPath))
        {
            Logger.LogError($"Sprite not found at path: {spriteReplacementPath}");
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(spriteReplacementPath);
            var originalTexture = image.sprite.texture;

            // We use an uncompressed format to avoid issues with compression requiring specific sizes (eg. DXT1, DXT5, BC7, BC6H)
            // Texture size doesn't matter, will be replaced by Unity in LoadImage to match texture
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes);

            // Ensure preserve aspect is on to avoid stretching issues where replacement sprite dimensions differ
            image.preserveAspect = true;

            // Ensure the rect fits within the new texture dimensions 
            var rect = image.sprite.rect;
            if (rect.width > texture.width || rect.height > texture.height)
            {
                Logger.LogWarning($"{spriteReplacementPath}: Texture dimensions are smaller than sprite rect, resizing");
                rect.width = Mathf.Min(rect.width, texture.width);
                rect.height = Mathf.Min(rect.height, texture.height);
            }

            image.sprite = Sprite.Create(texture, rect, image.sprite.pivot, image.sprite.pixelsPerUnit); ;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error replacing sprite: {ex}");
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive), [typeof(bool)])]
    public static void Postfix_GameObject_SetActive(GameObject __instance)
    {
        //Logger.LogWarning($"Checking SetActive: {__instance.name}");

        if (!ContractsLoaded)
            return;

        //TODO: This should be most efficient but we could use Object.Instantiate
        //to get it at as the objects created. But maybe there is post processing occuring after.
        var items = __instance.GetComponentsInChildren<Image>();
        foreach (var item in items)
            ReplaceSpriteInAsset(item);
    }

    //[HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.ResourceManager), "GetAssetObjectSprite")]
    //static bool Prefix(string path, ref Sprite __result)
    //{
    //    if (CheckPath)


    //    // Load your custom sprite
    //    string modAssetPath = Path.Combine(Paths.PluginPath, "MyModAssets", path.Replace("MyMod/", "") + ".png");
    //    if (File.Exists(modAssetPath))
    //    {
    //        byte[] fileData = File.ReadAllBytes(modAssetPath);
    //        Texture2D texture = new Texture2D(2, 2);
    //        texture.LoadImage(fileData);
    //        __result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    //        return false; // Skip the original method
    //    }

    //    // Fall back to the original method
    //    return true;
    //}
}

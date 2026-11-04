// Pawtisserie placeholder sprites: import settings applied automatically.
// Put this file in any "Editor" folder. When the PNGs are imported for the first time,
// it reads sprites.json (next to the sprite folders) and sets:
//   Sprite (Single), Pixels Per Unit (200 = one tray cell per world unit), custom pivot,
//   9-slice border where listed, no mipmaps, Full Rect mesh.
// It only touches a texture on its first import, so your own changes in the Inspector stay.
using System.IO;
using UnityEditor;
using UnityEngine;

public class PawtisserieSpriteImporter : AssetPostprocessor
{
    [System.Serializable]
    class Entry
    {
        public string name;
        public float pivotX = 0.5f, pivotY = 0.5f;
        public int borderL, borderB, borderR, borderT;
    }

    [System.Serializable]
    class Manifest
    {
        public float pixelsPerUnit = 200f;
        public Entry[] sprites;
    }

    static bool s_force;

    // If the PNGs were imported before this script existed, run this once.
    [MenuItem("Tools/Pawtisserie/Reapply Placeholder Sprite Settings")]
    static void Reapply()
    {
        s_force = true;
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("sprites t:TextAsset"))
            {
                string json = AssetDatabase.GUIDToAssetPath(guid);
                if (!json.EndsWith("/sprites.json")) continue;
                string folder = Path.GetDirectoryName(json).Replace('\\', '/');
                foreach (string tg in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(tg), ImportAssetOptions.ForceUpdate);
            }
        }
        finally { s_force = false; }
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith(".png")) return;
        var importer = (TextureImporter)assetImporter;
        if (!importer.importSettingsMissing && !s_force) return;   // first import only (or the menu below)

        // find sprites.json in this folder or up to three folders above it
        string root = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string manifestPath = null;
        for (int i = 0; i < 4 && !string.IsNullOrEmpty(root); i++)
        {
            string candidate = root + "/sprites.json";
            if (File.Exists(candidate)) { manifestPath = candidate; break; }
            root = Path.GetDirectoryName(root)?.Replace('\\', '/');
        }
        if (manifestPath == null) return;

        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
        if (manifest?.sprites == null) return;

        string rel = assetPath.Substring(root.Length + 1);
        rel = rel.Substring(0, rel.Length - 4);   // drop ".png"
        Entry entry = System.Array.Find(manifest.sprites, s => s.name == rel);
        if (entry == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = manifest.pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = rel.EndsWith("stripe_tile") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(entry.pivotX, entry.pivotY);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = new Vector4(entry.borderL, entry.borderB, entry.borderR, entry.borderT);
        importer.SetTextureSettings(settings);
    }
}

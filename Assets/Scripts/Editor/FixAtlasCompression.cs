using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public static class FixAtlasCompression
{
    [MenuItem("仙喵五子棋/修复Atlas压缩")]
    public static void Fix()
    {
        string[] guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets/Resources/CatAtlases" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null) continue;

            var so = new SerializedObject(atlas);
            var texSettings = so.FindProperty("m_EditorData.textureSettings");

            // Set compression to ASTC (value 4)
            texSettings.FindPropertyRelative("textureCompression").intValue = 4;
            texSettings.FindPropertyRelative("maxTextureSize").intValue = 1024;
            texSettings.FindPropertyRelative("compressionQuality").intValue = 50;

            so.ApplyModifiedProperties();
            Debug.Log($"Fixed: {path}");
        }

        // Force repack
        var atlases = new System.Collections.Generic.List<UnityEngine.U2D.SpriteAtlas>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var a = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(path);
            if (a != null) atlases.Add(a);
        }
        UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(atlases.ToArray(), EditorUserBuildSettings.activeBuildTarget);
        Debug.Log("Atlas repack done");
    }
}

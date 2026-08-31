using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Wuziqi.Editor
{
    /// <summary>一键为每只猫的帧动画打 SpriteAtlas 图集。</summary>
    public static class CatAtlasBuilder
    {
        private const string FramesRoot = "Assets/Resources/CatFrames";
        private const string AtlasOutput = "Assets/Resources/CatAtlases";

        [MenuItem("五子棋/打包猫猫图集")]
        public static void BuildAll()
        {
            if (!Directory.Exists(AtlasOutput))
                Directory.CreateDirectory(AtlasOutput);

            string[] cats = Directory.GetDirectories(FramesRoot);
            int built = 0;

            foreach (string catDir in cats)
            {
                string catName = Path.GetFileName(catDir);
                string atlasPath = AtlasOutput + "/Atlas_" + catName + ".spriteatlas";

                // 创建或复用图集
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (atlas == null)
                {
                    atlas = new SpriteAtlas();
                    AssetDatabase.CreateAsset(atlas, atlasPath);
                }

                // 清空旧引用
                var old = atlas.GetPackables();
                if (old.Length > 0)
                    atlas.Remove(old);

                // 设置图集参数
                var settings = new SpriteAtlasPackingSettings
                {
                    blockOffset = 1,
                    enableRotation = false,   // 帧动画不要旋转
                    enableTightPacking = false,
                    padding = 2
                };
                atlas.SetPackingSettings(settings);

                var textureSettings = new SpriteAtlasTextureSettings
                {
                    readable = false,
                    generateMipMaps = false,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear
                };
                atlas.SetTextureSettings(textureSettings);

                // 收集该猫所有子文件夹的 PNG
                string[] pngs = Directory.GetFiles(catDir, "*.png", SearchOption.AllDirectories);
                foreach (string png in pngs)
                {
                    string assetPath = png.Replace('\\', '/');
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (tex != null)
                        atlas.Add(new Object[] { tex });
                }

                // 设置打包
                atlas.SetIncludeInBuild(true);
                SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { atlas, }, EditorUserBuildSettings.activeBuildTarget);

                built++;
                Debug.Log($"[AtlasBuilder] {catName}: {pngs.Length} 帧 → {atlasPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AtlasBuilder] 完成，共打包 {built} 个图集");
        }
    }
}

using UnityEngine;
using UnityEditor;

namespace Wuziqi.EditorTools
{
    /// <summary>把头像挂到猫的 CatProfile（批量菜单工具）。</summary>
    public static class PortraitAssigner
    {
        private static readonly (string profile, string portrait)[] Map =
        {
            ("Cat_1_小白", "Portrait_XiaoBai"),
            ("Cat_2_橘座", "Portrait_橘座"),
            ("Cat_3_黑炭", "Portrait_黑炭"),
            ("Cat_4_花斑", "Portrait_花斑"),
            ("Cat_5_银渐层", "Portrait_银渐层"),
            ("Cat_6_玄猫", "Portrait_玄猫"),
            ("Cat_7_仙喵长老", "Portrait_仙喵长老"),
        };

        [MenuItem("仙喵五子棋/挂全部猫头像")]
        private static void AssignAll()
        {
            int ok = 0;
            foreach (var (profileName, portraitName) in Map)
            {
                var profile = AssetDatabase.LoadAssetAtPath<Wuziqi.Game.CatProfile>($"Assets/ScriptableObjects/Cats/{profileName}.asset");
                if (profile == null) { Debug.LogError($"profile not found: {profileName}"); continue; }
                var portraitPath = $"Assets/Art/Cat/Portraits/{portraitName}.png";
                var imp = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.SaveAndReimport();
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                if (sprite == null) { Debug.LogError($"sprite not found: {portraitName}"); continue; }
                profile.portrait = sprite;
                EditorUtility.SetDirty(profile);
                ok++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"assigned {ok}/{Map.Length} portraits");
        }
    }
}

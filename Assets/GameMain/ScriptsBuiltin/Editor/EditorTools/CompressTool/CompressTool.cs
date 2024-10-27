using UnityEditor.U2D;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GameFramework;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UGF.EditorTools
{
    public class AtlasSettings : IReference
    {
        public bool? includeInBuild = null;
        public bool? allowRotation = null;
        public bool? tightPacking = null;
        public bool? alphaDilation = null;
        public int? padding = null;
        public bool? readWrite = null;
        public bool? mipMaps = null;
        public bool? sRGB = null;
        public FilterMode? filterMode = null;
        public int? maxTexSize = null;
        public TextureImporterFormat? texFormat = null;
        public int? compressQuality = null;
        public virtual void Clear()
        {
            includeInBuild = null;
            allowRotation = null;
            tightPacking = null;
            alphaDilation = null;
            padding = null;
            readWrite = null;
            mipMaps = null;
            sRGB = null;
            filterMode = null;
            maxTexSize = null;
            texFormat = null;
            compressQuality = null;
        }
    }
    public class AtlasVariantSettings : AtlasSettings
    {
        public float variantScale = 0.5f;
        public override void Clear()
        {
            base.Clear();
            variantScale = 0.5f;
        }
        public static AtlasVariantSettings CreateFrom(AtlasSettings atlasSettings, float scale = 1f)
        {
            var settings = ReferencePool.Acquire<AtlasVariantSettings>();
            settings.includeInBuild = atlasSettings.includeInBuild;
            settings.allowRotation = atlasSettings.allowRotation;
            settings.tightPacking = atlasSettings.tightPacking;
            settings.alphaDilation = atlasSettings.alphaDilation;
            settings.padding = atlasSettings.padding;
            settings.readWrite = atlasSettings.readWrite;
            settings.mipMaps = atlasSettings.mipMaps;
            settings.sRGB = atlasSettings.sRGB;
            settings.filterMode = atlasSettings.filterMode;
            settings.maxTexSize = atlasSettings.maxTexSize;
            settings.texFormat = atlasSettings.texFormat;
            settings.compressQuality = atlasSettings.compressQuality;
            settings.variantScale = scale;
            return settings;
        }
    }
    public class CompressTool
    {
#if UNITY_EDITOR_WIN
        const string pngquantTool = "Tools/CompressImageTools/pngquant_win/pngquant.exe";
#elif UNITY_EDITOR_OSX
        const string pngquantTool = "Tools/CompressImageTools/pngquant_mac/pngquant";
#endif

        /// <summary>
        /// 创建图集
        /// </summary>
        /// <param name="atlasFilePath"></param>
        /// <param name="settings"></param>
        /// <param name="objectsForPack"></param>
        /// <param name="createAtlasVariant"></param>
        /// <param name="atlasVariantScale"></param>
        /// <returns></returns>
        public static SpriteAtlas CreateAtlas(string atlasName, AtlasSettings settings, UnityEngine.Object[] objectsForPack, bool createAtlasVariant = false, float atlasVariantScale = 1f)
        {
            CreateEmptySpriteAtlas(atlasName);
            SpriteAtlas result;
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                var atlas = SpriteAtlasAsset.Load(atlasName);
#if UNITY_2022_1_OR_NEWER
                var atlasImpt = AssetImporter.GetAtPath(atlasName) as SpriteAtlasImporter;
                atlasImpt.includeInBuild = settings.includeInBuild ?? true;
#else 
                atlas.SetIncludeInBuild(settings.includeInBuild ?? true);
#endif
                atlas.Add(objectsForPack);
#if UNITY_2022_1_OR_NEWER
                var packSettings = atlasImpt.packingSettings;
                var texSettings = atlasImpt.textureSettings;
                var platformSettings = atlasImpt.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                var packSettings = atlas.GetPackingSettings();
                var texSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#endif
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                atlasImpt.packingSettings = packSettings;
                atlasImpt.textureSettings = texSettings;
                atlasImpt.SetPlatformSettings(platformSettings);
#else
                atlas.SetPackingSettings(packSettings);
                atlas.SetTextureSettings(texSettings);
                atlas.SetPlatformSettings(platformSettings);
#endif
                SpriteAtlasAsset.Save(atlas, atlasName);

                result = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasName);
            }
            else
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasName);
                atlas.SetIncludeInBuild(settings.includeInBuild ?? true);
                atlas.Add(objectsForPack);
                var packSettings = atlas.GetPackingSettings();
                var texSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
                atlas.SetPackingSettings(packSettings);
                atlas.SetTextureSettings(texSettings);
                atlas.SetPlatformSettings(platformSettings);
                result = atlas;
                AssetDatabase.SaveAssets();
            }

            if (createAtlasVariant)
            {
                var atlasVarSets = new AtlasVariantSettings()
                {
                    variantScale = atlasVariantScale,
                    readWrite = settings.readWrite,
                    mipMaps = settings.mipMaps,
                    sRGB = settings.sRGB,
                    filterMode = settings.filterMode,
                    texFormat = settings.texFormat,
                    compressQuality = settings.compressQuality
                };
                CreateAtlasVariant(result, atlasVarSets);
            }
            return result;
        }
        private static void ModifySpriteAtlasSettings(AtlasSettings input, ref SpriteAtlasPackingSettings packSets, ref SpriteAtlasTextureSettings texSets, ref TextureImporterPlatformSettings platSets)
        {
            packSets.enableRotation = input.allowRotation ?? packSets.enableRotation;
            packSets.enableTightPacking = input.tightPacking ?? packSets.enableTightPacking;
            packSets.enableAlphaDilation = input.alphaDilation ?? packSets.enableAlphaDilation;
            packSets.padding = input.padding ?? packSets.padding;
            texSets.readable = input.readWrite ?? texSets.readable;
            texSets.generateMipMaps = input.mipMaps ?? texSets.generateMipMaps;
            texSets.sRGB = input.sRGB ?? texSets.sRGB;
            texSets.filterMode = input.filterMode ?? texSets.filterMode;
            platSets.overridden = null != input.maxTexSize || null != input.texFormat || null != input.compressQuality;
            platSets.maxTextureSize = input.maxTexSize ?? platSets.maxTextureSize;
            platSets.format = input.texFormat ?? platSets.format;
            platSets.compressionQuality = input.compressQuality ?? platSets.compressionQuality;
        }
        /// <summary>
        /// 根据文件夹名字返回一个图集名
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static string GetAtlasExtensionV1V2()
        {
            return EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2 ? ".spriteatlasv2" : ".spriteatlas";
        }
        public static void CreateEmptySpriteAtlas(string atlasAssetName)
        {
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                SpriteAtlasAsset.Save(new SpriteAtlasAsset(), atlasAssetName);
            }
            else
            {
                AssetDatabase.CreateAsset(new SpriteAtlas(), atlasAssetName);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        /// <summary>
        /// 根据图集对象生成图集变体
        /// </summary>
        /// <param name="atlas"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SpriteAtlas CreateAtlasVariant(SpriteAtlas atlasMaster, AtlasVariantSettings settings)
        {
            if (atlasMaster == null || atlasMaster.isVariant) return atlasMaster;
            var atlasFileName = AssetDatabase.GetAssetPath(atlasMaster);
            if (string.IsNullOrEmpty(atlasFileName))
            {
                Debug.LogError($"atlas '{atlasMaster.name}' is not a asset file.");
                return null;
            }
            
            var atlasVariantName = UtilityBuiltin.AssetsPath.GetCombinePath(Path.GetDirectoryName(atlasFileName), $"{Path.GetFileNameWithoutExtension(atlasFileName)}_Variant{Path.GetExtension(atlasFileName)}");

            SpriteAtlas varAtlas;
            if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2)
            {
                var atlas = SpriteAtlasAsset.Load(atlasFileName);
#if UNITY_2022_1_OR_NEWER
                var atlasImpt = AssetImporter.GetAtPath(atlasFileName) as SpriteAtlasImporter;
                atlasImpt.includeInBuild = settings.includeInBuild ?? true;
                var packSettings = atlasImpt.packingSettings;
                var texSettings = atlasImpt.textureSettings;
                var platformSettings = atlasImpt.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                atlas.SetIncludeInBuild(false);
                var packSettings = atlas.GetPackingSettings();
                var texSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());

#endif
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                atlasImpt.packingSettings = packSettings;
                atlasImpt.textureSettings = texSettings;
                atlasImpt.SetPlatformSettings(platformSettings);
                atlasImpt.SaveAndReimport();
#else
                atlas.SetPackingSettings(packSettings);
                atlas.SetTextureSettings(texSettings);
                atlas.SetPlatformSettings(platformSettings);
#endif
                SpriteAtlasAsset.Save(atlas, atlasFileName);

                CreateEmptySpriteAtlas(atlasVariantName);
                var tmpVarAtlas = SpriteAtlasAsset.Load(atlasVariantName);
#if UNITY_2022_1_OR_NEWER
                var tmpVarAtlasImpt = AssetImporter.GetAtPath(atlasVariantName) as SpriteAtlasImporter;
                tmpVarAtlasImpt.includeInBuild = settings.includeInBuild ?? true;
                packSettings = tmpVarAtlasImpt.packingSettings;
                texSettings = tmpVarAtlasImpt.textureSettings;
                platformSettings = tmpVarAtlasImpt.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
#else
                tmpVarAtlas.SetIncludeInBuild(true);
                packSettings = tmpVarAtlas.GetPackingSettings();
                texSettings = tmpVarAtlas.GetTextureSettings();
                platformSettings = tmpVarAtlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());

#endif
                tmpVarAtlas.SetIsVariant(true);
                
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
#if UNITY_2022_1_OR_NEWER
                tmpVarAtlasImpt.packingSettings = packSettings;
                tmpVarAtlasImpt.textureSettings = texSettings;
                tmpVarAtlasImpt.variantScale = settings.variantScale;
                tmpVarAtlasImpt.SetPlatformSettings(platformSettings);
                tmpVarAtlasImpt.SaveAndReimport();
#else
                tmpVarAtlas.SetPackingSettings(packSettings);
                tmpVarAtlas.SetTextureSettings(texSettings);
                tmpVarAtlas.SetVariantScale(settings.variantScale);
                tmpVarAtlas.SetPlatformSettings(platformSettings);
#endif
                tmpVarAtlas.SetMasterAtlas(atlasMaster);
                SpriteAtlasAsset.Save(tmpVarAtlas, atlasVariantName);

                varAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasVariantName);
            }
            else
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasFileName);
                atlas.SetIncludeInBuild(false);
                var packSettings = atlas.GetPackingSettings();
                var texSettings = atlas.GetTextureSettings();
                var platformSettings = atlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
                atlas.SetPackingSettings(packSettings);
                atlas.SetTextureSettings(texSettings);
                atlas.SetPlatformSettings(platformSettings);

                CreateEmptySpriteAtlas(atlasVariantName);
                var tmpVarAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasVariantName);
                tmpVarAtlas.SetIncludeInBuild(true);
                tmpVarAtlas.SetIsVariant(true);
                packSettings = tmpVarAtlas.GetPackingSettings();
                texSettings = tmpVarAtlas.GetTextureSettings();
                platformSettings = tmpVarAtlas.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString());
                ModifySpriteAtlasSettings(settings, ref packSettings, ref texSettings, ref platformSettings);
                tmpVarAtlas.SetPackingSettings(packSettings);
                tmpVarAtlas.SetTextureSettings(texSettings);
                tmpVarAtlas.SetPlatformSettings(platformSettings);
                tmpVarAtlas.SetMasterAtlas(atlasMaster);
                tmpVarAtlas.SetVariantScale(settings.variantScale);
                AssetDatabase.SaveAssets();

                varAtlas = tmpVarAtlas;
            }

            return varAtlas;
        }
        /// <summary>
        /// 根据Atlas文件名为Atlas生成Atlas变体(Atlas Variant)
        /// </summary>
        /// <param name="atlasFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static SpriteAtlas CreateAtlasVariant(string atlasFile, AtlasVariantSettings settings)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasFile);

            return CreateAtlasVariant(atlas, settings);
        }

        /// <summary>
        /// 批量重新打包图集
        /// </summary>
        /// <param name="spriteAtlas"></param>
        public static void PackAtlases(SpriteAtlas[] spriteAtlas)
        {
            SpriteAtlasUtility.PackAtlases(spriteAtlas, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void OptimizeAnimationClips(List<string> list, int precision)
        {
            string pattern = $"(\\d+\\.[\\d]{{{precision},}})";

            int totalCount = list.Count;
            int finishCount = 0;
            foreach (var itmName in list)
            {
                if (File.GetAttributes(itmName) != FileAttributes.ReadOnly)
                {
                    if (Path.GetExtension(itmName).ToLower().CompareTo(".anim") == 0)
                    {
                        finishCount++;
                        if (EditorUtility.DisplayCancelableProgressBar(string.Format("压缩浮点精度({0}/{1})", finishCount, totalCount), itmName, finishCount / (float)totalCount))
                        {
                            break;
                        }
                        var allTxt = File.ReadAllText(itmName);
                        // 将匹配到的浮点型数字替换为精确到3位小数的浮点型数字
                        string outputString = Regex.Replace(allTxt, pattern, match =>
                        float.Parse(match.Value).ToString($"F{precision}"));
                        File.WriteAllText(itmName, outputString);
                        Debug.LogFormat("----->压缩动画浮点精度:{0}", itmName);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}


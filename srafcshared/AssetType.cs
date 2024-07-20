using isogame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace srafcshared
{
    public enum AssetType
    {
        unknown,
        ab,
        ai,
        ambi,
        blib,
        ch_inst,
        ch_sht,
        convo,
        cpack,
        credits,
#if !SRR
        cvf,
#endif
        eq_sht,
        hiring,
        item,
        mf,
        mode,
        pb,
        pcode,
        pl,
        srm,
        srt,
        story,
        submix,
        tml,
        topic,
    }

    public abstract class BaseTypeHandler
    {
        public AssetType AssetType { get; private set; }
        public string Extension { get; private set; }
        public Type Type { get; private set; }

        public BaseTypeHandler(AssetType assetType, string extension, Type type)
        {
            this.AssetType = assetType;
            this.Extension = extension;
            this.Type = type;
        }

        public abstract void ProcessAssetFile(string infile, AssetFormat informat, string outfile, AssetFormat outformat);
    }

    public class AssetTypeHandler : BaseTypeHandler
    {
        public AssetTypeHandler(AssetType assetType, string extension, Type type)
            : base(assetType, extension, type)
        {
        }

        public override void ProcessAssetFile(string infile, AssetFormat informat, string outfile, AssetFormat outformat)
        {
            typeof(AssetTypeHandler)
                .GetMethod("StaticProcessAssetFile")
                .MakeGenericMethod(this.Type)
                .Invoke(this, [infile, informat, outfile, outformat]);
        }

        public static void StaticProcessAssetFile<T>(string infile, AssetFormat informat, string outfile, AssetFormat outformat) where T : class
        {
            T obj = informat.Handler().Deserialize<T>(infile);

            if (obj == null)
            {
                throw new InvalidOperationException($"Failed to deserialize input file: {infile}");
            }

            outformat.Handler().Serialize(outfile, obj);
        }
    }

    public static class AssetTypeExtension
    {
        public static Dictionary<AssetType, AssetTypeHandler> Handlers = new Dictionary<AssetType, AssetTypeHandler>();

        public static AssetTypeHandler Handler(this AssetType assetType)
        {
            if (!Handlers.ContainsKey(assetType))
            {
                Handlers[assetType] = assetType switch
                {
                    AssetType.ab => new AssetTypeHandler(assetType, ".ab", typeof(AbilityDef)),
                    AssetType.ai => new AssetTypeHandler(assetType, ".ai", typeof(ObjectiveArchetype)),
                    AssetType.ambi => new AssetTypeHandler(assetType, ".ambi", typeof(AmbienceTemplate)),
                    AssetType.blib => new AssetTypeHandler(assetType, ".blib", typeof(BackerPCLibrary)),
                    AssetType.ch_inst => new AssetTypeHandler(assetType, ".ch_inst", typeof(CharacterInstance)),
                    AssetType.ch_sht => new AssetTypeHandler(assetType, ".ch_sht", typeof(Character)),
                    AssetType.convo => new AssetTypeHandler(assetType, ".convo", typeof(Conversation)),
                    AssetType.cpack => new AssetTypeHandler(assetType, ".cpack", typeof(ProjectDef)),
                    AssetType.credits => new AssetTypeHandler(assetType, ".credits", typeof(string)),
#if !SRR
                    AssetType.cvf => new AssetTypeHandler(assetType, ".cvf", typeof(CharacterVariant)),
#endif
                    AssetType.eq_sht => new AssetTypeHandler(assetType, ".eq_sht", typeof(EquipmentSheet)),
                    AssetType.hiring => new AssetTypeHandler(assetType, ".hiring", typeof(HiringSet)),
                    AssetType.item => new AssetTypeHandler(assetType, ".item", typeof(ItemDef)),
                    AssetType.mf => new AssetTypeHandler(assetType, ".mf", typeof(Manifest)),
                    AssetType.mode => new AssetTypeHandler(assetType, ".mode", typeof(ModeDef)),
                    AssetType.pb => new AssetTypeHandler(assetType, ".pb", typeof(PropDef)),
                    AssetType.pcode => new AssetTypeHandler(assetType, ".pcode", typeof(PortraitCodeList)),
                    AssetType.pl => new AssetTypeHandler(assetType, ".pl", typeof(PortraitList)),
                    AssetType.srm => new AssetTypeHandler(assetType, ".srm", typeof(MapDef)),
                    AssetType.srt => new AssetTypeHandler(assetType, ".srt", typeof(SceneDef)),
                    AssetType.story => new AssetTypeHandler(assetType, ".story", typeof(StoryDef)),
                    AssetType.submix => new AssetTypeHandler(assetType, ".submix", typeof(SubMixGroup)),
                    AssetType.tml => new AssetTypeHandler(assetType, ".tml", typeof(TotemList)),
                    AssetType.topic => new AssetTypeHandler(assetType, ".topic", typeof(Topic)),
                    _ => throw new ArgumentException($"Unsupported asset type: {assetType}")
                };
            }
            return Handlers[assetType];
        }

        public static void Validate()
        {
            foreach (var assetType in Enum.GetValues(typeof(AssetType)).Cast<AssetType>())
            {
                if (assetType == AssetType.unknown)
                {
                    continue;
                }
                var handler = assetType.Handler();
                if (handler == null)
                {
                    throw new ArgumentException($"Unsupported asset type: {assetType}, has null handler?");
                }
            }
        }

        public static IEnumerable<AssetType> ValidOptions()
        {
            foreach (AssetType assetType in Enum.GetValues(typeof(AssetType)))
            {
                if (assetType != AssetType.unknown)
                {
                    yield return assetType;
                }
            }
            yield break;
        }

        public static string ValidOptionsString()
        {
            return string.Join(", ", ValidOptions().Select(t => t.ToString()).ToArray());
        }

        public static AssetType FromFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return AssetType.unknown;
            }
            return FromFilename(new FileInfo(filename));
        }

        public static AssetType FromFilename(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return AssetType.unknown;
            }
            string fullFilename = fileInfo.FullName;
            var noExtension = Path.GetFileNameWithoutExtension(fullFilename);
            var extension = Path.GetExtension(noExtension);
            string extensionToTest;
            if (string.IsNullOrEmpty(extension) || extension.Length < 2)
            {
                // this would handle cases where the filename is <type>.<extension>
                // for example, apparently (according to code), credits could be found at
                // data/misc/credits.bytes
                // and that's awesome
                extensionToTest = noExtension;
            }
            else
            {
                extensionToTest = extension.Substring(1);
            }

            try
            {
                return (AssetType)Enum.Parse(typeof(AssetType), extensionToTest, true);
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to parse asset type from filename:{fileInfo.FullName}: fullFilename;{fullFilename}: noExtension:{noExtension}: extension:{extension}:");
                return AssetType.unknown;
            }
        }
    }
}

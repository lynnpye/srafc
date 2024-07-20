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
        item,
        pl,
        pcode,
        cpack,
        mf,
    }

    public class AssetTypeHandler
    {
        public AssetType AssetType {  get; private set; }
        public string Extension { get; private set; }
        public Type Type { get; private set; }

        public AssetTypeHandler(AssetType assetType, string extension, Type type)
        {
            this.AssetType = assetType;
            this.Extension = extension;
            this.Type = type;
        }

        public void ProcessAssetFile(string infile, AssetFormat informat, string outfile, AssetFormat outformat)
        {
            var funcy = Expression.GetFuncType([typeof(AssetFormat), typeof(string), this.Type]);

            typeof(AssetTypeHandler)
                .GetMethod("StaticProcessAssetFile")
                .MakeGenericMethod(this.Type)
                .Invoke(this, [infile, informat, outfile, outformat]);
        }

        public static void StaticProcessAssetFile<T>(string infile, AssetFormat informat, string outfile, AssetFormat outformat) where T : class
        {
            T obj = informat.Deserialize<T>(infile);

            if (obj == null)
            {
                throw new InvalidOperationException($"Failed to deserialize input file: {infile}");
            }

            outformat.Serialize(outfile, obj);
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
                    AssetType.item => new AssetTypeHandler(assetType, ".item", typeof(ItemDef)),
                    AssetType.pl => new AssetTypeHandler(assetType, ".pl", typeof(PortraitList)),
                    AssetType.pcode => new AssetTypeHandler(assetType, ".pcode", typeof(PortraitCodeList)),
                    AssetType.cpack => new AssetTypeHandler(assetType, ".cpack", typeof(ProjectDef)),
                    AssetType.mf => new AssetTypeHandler(assetType, ".mf", typeof(Manifest)),
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
            if (string.IsNullOrEmpty(noExtension))
            {
                return AssetType.unknown;
            }
            var extension = Path.GetExtension(noExtension);
            if (string.IsNullOrEmpty(extension) || extension.Length < 2)
            {
                return AssetType.unknown;
            }
            return (AssetType)Enum.Parse(typeof(AssetType), extension.Substring(1), true);
        }
    }
}

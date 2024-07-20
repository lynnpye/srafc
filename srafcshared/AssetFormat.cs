using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace srafcshared
{
    public enum AssetFormat
    {
        unknown,
        json,
        bytes
    }

    public class AssetFormatHandler
    {
        public AssetFormat AssetFormat { get; private set; }
        public string Extension { get; private set; }

        public AssetFormatHandler(AssetFormat assetFormat, string extension)
        {
            this.AssetFormat = assetFormat;
            this.Extension = extension;
        }
    }

    public static class AssetFormatExtension
    {
        public static Dictionary<AssetFormat, AssetFormatHandler> Handlers = new Dictionary<AssetFormat, AssetFormatHandler>();

        public static AssetFormatHandler Handler(this AssetFormat assetFormat)
        {
            if (!Handlers.ContainsKey(assetFormat))
            {
                Handlers[assetFormat] = assetFormat switch
                {
                    AssetFormat.json => new AssetFormatHandler(assetFormat, ".json"),
                    AssetFormat.bytes => new AssetFormatHandler(assetFormat, ".bytes"),
                    _ => throw new ArgumentException($"Unsupported format: {assetFormat}")
                };
            }
            return Handlers[assetFormat];
        }

        public static void Validate()
        {
            foreach(var assetFormat in Enum.GetValues(typeof(AssetFormat)).Cast<AssetFormat>())
            {
                if (assetFormat == AssetFormat.unknown)
                {
                    continue;
                }
                var handler = assetFormat.Handler();
                if (handler == null)
                {
                    throw new ArgumentException($"Unsupported asset format: {assetFormat}, has null handler?");
                }
            }
        }

        public static IEnumerable<AssetFormat> ValidOptions()
        {
            foreach (AssetFormat assetFormat in Enum.GetValues(typeof(AssetFormat)))
            {
                if (assetFormat != AssetFormat.unknown)
                {
                    yield return assetFormat;
                }
            }
            yield break;
        }

        public static string ValidOptionsString()
        {
            return string.Join(", ", ValidOptions().Select(f => f.ToString()).ToArray());
        }

        public static AssetFormat FromFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return AssetFormat.unknown;
            }
            return FromFilename(new FileInfo(filename));
        }

        public static AssetFormat FromFilename(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                return AssetFormat.unknown;
            }
            string fullFileName = fileInfo.FullName;
            var extension = Path.GetExtension(fullFileName);
            if (string.IsNullOrEmpty(extension) || extension.Length < 2)
            {
                return AssetFormat.unknown;
            }
            return (AssetFormat) Enum.Parse(typeof(AssetFormat), extension.Substring(1), true);
        }

        public static T Deserialize<T>(this AssetFormat format, string filename) where T : class
        {
            switch (format)
            {
                case AssetFormat.json:
                    {
                        string jsonContent = File.ReadAllText(filename);
                        return SRFileConverter.ConvertFromJson<T>(jsonContent);
                    }
                case AssetFormat.bytes:
                    return SRFileConverter.Deserialize<T>(filename);
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
        }

        public static void Serialize<T>(this AssetFormat format, string filename, T obj) where T : class
        {
            switch (format)
            {
                case AssetFormat.json:
                    {
                        string jsonContent = SRFileConverter.ConvertToJson(obj);
                        File.WriteAllText(filename, jsonContent);
                        break;
                    }
                case AssetFormat.bytes:
                    SRFileConverter.Serialize(filename, obj);
                    break;
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
        }
    }
}

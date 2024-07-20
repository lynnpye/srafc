using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace srafcshared
{
    public enum AssetFormat
    {
        unknown,
        json,
        bytes
    }

    public abstract class BaseFormatHandler
    {
        public AssetFormat AssetFormat { get; private set; }
        public string Extension { get; private set; }

        public BaseFormatHandler(AssetFormat assetFormat, string extension)
        {
            this.AssetFormat = assetFormat;
            this.Extension = extension;
        }

        public abstract T Deserialize<T>(string filename) where T : class;

        public abstract void Serialize<T>(string filename, T obj) where T : class;
    }

    public class JsonFormatHandler : BaseFormatHandler
    {
        public JsonFormatHandler(AssetFormat assetFormat, string extension)
            : base(assetFormat, extension)
        {
        }

        public override T Deserialize<T>(string filename) where T : class
        {
            string jsonContent = File.ReadAllText(filename);
            return SRFileConverter.ConvertFromJson<T>(jsonContent);
        }

        public override void Serialize<T>(string filename, T obj) where T : class
        {
            string jsonContent = SRFileConverter.ConvertToJson(obj);
            File.WriteAllText(filename, jsonContent);
        }
    }

    public class BytesFormatHandler : BaseFormatHandler
    {
        public BytesFormatHandler(AssetFormat assetFormat, string extension)
            : base(assetFormat, extension)
        {
        }

        public override T Deserialize<T>(string filename)
        {
            // handle some special cases
            if (typeof(T) == typeof(string))
            {
                var bytes = SRFileConverter.LoadFileToBytes(filename);
                return (T)(object)Encoding.UTF8.GetString(bytes);
            }

            // handle the standard cases
            return SRFileConverter.Deserialize<T>(filename);
        }

        public override void Serialize<T>(string filename, T obj)
        {
            // handle some special cases
            if (typeof(T) == typeof(string))
            {
                var bytes = Encoding.UTF8.GetBytes(obj as string);
                File.WriteAllBytes(filename, bytes);

                return;
            }

            // handle the standard cases
            SRFileConverter.Serialize(filename, obj);
        }
    }

    public static class AssetFormatExtension
    {
        public static Dictionary<AssetFormat, BaseFormatHandler> Handlers = new Dictionary<AssetFormat, BaseFormatHandler>();

        public static BaseFormatHandler Handler(this AssetFormat assetFormat)
        {
            if (!Handlers.ContainsKey(assetFormat))
            {
                Handlers[assetFormat] = assetFormat switch
                {
                    AssetFormat.json => new JsonFormatHandler(assetFormat, ".json"),
                    AssetFormat.bytes => new BytesFormatHandler(assetFormat, ".bytes"),
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
            try
            {
                return (AssetFormat)Enum.Parse(typeof(AssetFormat), extension.Substring(1), true);
            }
            catch (Exception)
            {
                return AssetFormat.unknown;
            }
        }
    }
}

using Fclp;
using isogame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace srafcshared
{
    public enum Format
    {
        unknown,
        json,
        bytes
    }

    public enum ThingType
    {
        unknown,
        item,
        pl,
        pcode,
        cpack,
        mf,
    }

    static class ExtHelper
    {
        public static string GetExtStr(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return string.Empty;
            }
            int lastDotIdx = filename.LastIndexOf('.');
            if (lastDotIdx == -1)
            {
                return string.Empty;
            }
            return filename.Substring(lastDotIdx + 1);
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }

    public static class FormatFileExtension
    {
        public static string GetFileExtension(this Format format)
        {
            return format switch
            {
                Format.json => ".json",
                Format.bytes => ".bytes",
                _ => throw new ArgumentException($"Unsupported format: {format}")
            };
        }
        public static string GetFileExtension(this Format format, ThingType thingType)
        {
            return thingType.GetFileExtension() + (format switch
            {
                Format.json => ".json",
                Format.bytes => ".bytes",
                _ => throw new ArgumentException($"Unsupported format: {format}")
            });
        }

        public static Format FromFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return Format.unknown;
            }
            string ext = ExtHelper.GetExtStr(filename);

            return ExtHelper.ParseEnum<Format>(ext);
        }
    }

    public static class ThingTypeExtension
    {
        public static string GetFileExtension(this ThingType thingType)
        {
            return thingType switch
            {
                ThingType.item => ".item",
                ThingType.pl => ".pl",
                ThingType.pcode => ".pcode",
                ThingType.cpack => ".cpack",
                ThingType.mf => ".mf",
                _ => throw new ArgumentException($"Unsupported thing type: {thingType}")
            };
        }

        public static ThingType FromFilename(string filename, Format format)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return ThingType.unknown;
            }

            filename = filename.Substring(0, filename.Length - (format.ToString().Length + 1));

            string thingExt = ExtHelper.GetExtStr(filename);
            
            return ExtHelper.ParseEnum<ThingType>(thingExt);
        }
    }

    public class AppArgs
    {
        public string infile { get; set; }
        public string outfile { get; set; }
        public Format? fromformat { get; set; }
        public Format? toformat { get; set; }
        public ThingType? type { get; set; }
    }

    class HandleRequest
    {
        private static HandleRequest _instance;
        private static readonly object _lock = new object();
        private string[] _args;
        private AppArgs _appArgs;

        private HandleRequest(string[] args)
        {
            _args = args;

            var p = new FluentCommandLineParser<AppArgs>();

            p.Setup(arg => arg.infile)
                .As('i', "infile")
                .Required();

            p.Setup(arg => arg.outfile)
                .As('o', "outfile")
                .Required();

            p.Setup(arg => arg.fromformat)
                .As('f', "fromformat")
                .WithDescription("Specifies the format of the input file. Valid values are JSON or BYTES.");

            p.Setup(arg => arg.toformat)
                .As('t', "toformat")
                .WithDescription("Specifies the format of the output file. Valid values are JSON or BYTES.");

            var validthingtypes = string.Join(", ", Enum.GetNames(typeof(ThingType)));
            p.Setup(arg => arg.type)
               .As('y', "type")
               .WithDescription($"Specifies the type of the thing. Valid values are {validthingtypes}.")
               .Required();

            var result = p.Parse(args);
            _appArgs = p.Object;
        }

        public static HandleRequest InitWithArgs(string[] args)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new HandleRequest(args).Validate();
                    }
                }
            }
            return _instance;
        }

        public static HandleRequest Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("HandleRequest has not been initialized with arguments. Call InitWithArgs first.");
                }
                return _instance;
            }
        }

        public HandleRequest Validate()
        {
            // Check if fromformat and toformat are the same
            if (_appArgs.fromformat.HasValue && _appArgs.toformat.HasValue && _appArgs.fromformat == _appArgs.toformat)
            {
                throw new ArgumentException("The source and destination formats cannot be the same.");
            }

            // Check if infile and outfile point to the same location
            if (string.Equals(Path.GetFullPath(_appArgs.infile), Path.GetFullPath(_appArgs.outfile), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The input file and output file cannot point to the same location.");
            }

            ValidateFiles();
            return this;
        }

        public void ValidateFiles()
        {
            bool infileIsDirectory = Directory.Exists(_appArgs.infile);
            bool outfileIsDirectory = Directory.Exists(_appArgs.outfile);

            // Check if one is a directory and the other is not
            if (infileIsDirectory != outfileIsDirectory)
            {
                throw new ArgumentException("Both 'infile' and 'outfile' must either be directories or files.");
            }

            // If both are files, proceed with existing file validation logic
            if (!infileIsDirectory)
            {
                FileInfo inputFile = new FileInfo(_appArgs.infile);
                if (!inputFile.Exists)
                {
                    throw new FileNotFoundException($"Input file not found: {_appArgs.infile}");
                }
                ThingType inthing = ThingType.unknown;
                _appArgs.fromformat = InferFormatFromExtension(inputFile, _appArgs.fromformat, "Input", ref inthing);

                FileInfo outputFile = new FileInfo(_appArgs.outfile);
                if (outputFile.Exists && (outputFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    throw new IOException($"Output file is read-only: {_appArgs.outfile}");
                }
                else if (!Directory.Exists(outputFile.DirectoryName))
                {
                    throw new DirectoryNotFoundException($"Output file directory not found: {outputFile.DirectoryName}");
                }
                ThingType outthing = ThingType.unknown;
                _appArgs.toformat = InferFormatFromExtension(outputFile, _appArgs.toformat, "Output", ref outthing);

                if (inthing != outthing)
                {
                    throw new ArgumentException("Input and output file types must match.");
                }
                _appArgs.type = inthing;
            }
        }

        private Format InferFormatFromExtension(FileInfo fileInfo, Format? currentFormat, string fileType, ref ThingType thingType)
        {
            if (currentFormat != null)
            {
                // If format is already specified, no need to infer
                return currentFormat.Value;
            }

            Format inferredFormat = ExtHelper.ParseEnum<Format>(ExtHelper.GetExtStr(fileInfo.FullName));

            string typeExtension = ExtHelper.GetExtStr(fileInfo.FullName.Substring(0, fileInfo.FullName.Length - (inferredFormat.ToString().Length + 1)));
            thingType = ExtHelper.ParseEnum<ThingType>(typeExtension);

            return inferredFormat;
        }

        public void Process()
        {
            // Check if infile is a directory
            if (Directory.Exists(_appArgs.infile))
            {
                // Ensure outfile is also specified as a directory
                if (!Directory.Exists(_appArgs.outfile))
                {
                    throw new ArgumentException("When 'infile' is a directory, 'outfile' must also be a directory.");
                }

                // Get all files in the input directory
                var inputFiles = Directory.GetFiles(_appArgs.infile);

                foreach (var inputFile in inputFiles)
                {
                    // Determine the output file name and path
                    var outputFile = Path.Combine(_appArgs.outfile, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(inputFile)) + _appArgs.toformat.Value.GetFileExtension(_appArgs.type ?? ThingType.unknown));

                    // Process each file individually
                    ProcessFile(inputFile, outputFile);
                }
            }
            else
            {
                // Existing logic for single file processing
                ProcessFile(_appArgs.infile, _appArgs.outfile);
            }
        }

        private void ProcessFile(string inputFile, string outputFile)
        {
            switch (_appArgs.type)
            {
                case ThingType.item:
                    ProcessAssetFile<ItemDef>(inputFile, outputFile);
                    break;
                case ThingType.pl:
                    ProcessAssetFile<PortraitList>(inputFile, outputFile);
                    break;
                case ThingType.pcode:
                    ProcessAssetFile<PortraitCodeList>(inputFile, outputFile);
                    break;
                case ThingType.cpack:
                    ProcessAssetFile<ProjectDef>(inputFile, outputFile);
                    break;
                case ThingType.mf:
                    ProcessAssetFile<Manifest>(inputFile, outputFile);
                    break;
            }
        }

        private void ProcessAssetFile<T>(string inputFile, string outputFile) where T : class
        {
            T obj = null;
            switch (_appArgs.fromformat)
            {
                case Format.json:
                    {
                        string jsonContent = File.ReadAllText(inputFile);

                        obj = SRFileConverter.ConvertFromJson<T>(jsonContent);
                    }
                    break;
                case Format.bytes:
                    {
                        obj = SRFileConverter.Deserialize<T>(inputFile);
                    }
                    break;
            }

            if (obj == null)
            {
                throw new InvalidOperationException($"Failed to deserialize input file: {inputFile}");
            }

            switch (_appArgs.toformat)
            {
                case Format.json:
                    {
                        string jsonContent = SRFileConverter.ConvertToJson(obj);

                        File.WriteAllText(outputFile, jsonContent);
                    }
                    break;

                case Format.bytes:
                    {
                        SRFileConverter.Serialize(outputFile, obj);
                    }
                    break;
            }
        }
    }
}
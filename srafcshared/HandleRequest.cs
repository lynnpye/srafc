using Fclp;
using System;
using System.IO;
using System.Text;

namespace srafcshared
{
    public class AppArgs
    {
        public string infile { get; set; }
        public string outfile { get; set; }
        public AssetFormat? informat { get; set; }
        public AssetFormat? outformat { get; set; }
        public AssetType? assettype { get; set; }
    }

    class HandleRequest
    {
        private AppArgs _appArgs;
        private string _appname;

        public static string GetUsage(string appname)
        {
            var usageStringBuilder = new StringBuilder();
            usageStringBuilder.AppendLine("Usage:");
            usageStringBuilder.AppendLine("  -i, --infile      Required. Specifies the input file or directory.");
            usageStringBuilder.AppendLine("  -o, --outfile     Required. Specifies the output file or directory.");

            var validformats = AssetFormatExtension.ValidOptionsString();
            usageStringBuilder.AppendLine($"  -f, --informat    Specifies the format of the input file. Valid values are '{validformats}'.");
            usageStringBuilder.AppendLine($"  -t, --outformat   Specifies the format of the output file. Valid values are '{validformats}'.");

            var validassettypes = AssetTypeExtension.ValidOptionsString();
            usageStringBuilder.AppendLine($"  -y, --type        Required. Specifies the type of the asset. Valid values are '{validassettypes}'.");
            usageStringBuilder.AppendLine();
            usageStringBuilder.AppendLine("Example:");
            usageStringBuilder.AppendLine($"  {appname} -i input.json -o output.bytes -f json -t bytes -y item");
            return usageStringBuilder.ToString();
        }

        public void HandleMain()
        {
            try
            {
                Process();
            }
            catch (Exception e)
            {
                Console.WriteLine(GetUsage(_appname));
                Console.WriteLine(e.Message);
                Console.WriteLine(e.ToString());
            }
        }

        public void Process()
        {
            if (Directory.Exists(_appArgs.infile))
            {
                var inputFiles = Directory.GetFiles(_appArgs.infile);
                foreach (var inputFile in inputFiles)
                {
                    var filenameNoExtensions = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(inputFile));

                    AssetFormat inferredInFormat = AssetFormatExtension.FromFilename(inputFile);
                    if (_appArgs.informat.HasValue && _appArgs.informat != inferredInFormat)
                    {
                        // skipping because it doesn't match the format you specified, skipping
                        continue;
                    }
                    AssetFormat informat = _appArgs.informat ?? inferredInFormat;
                    AssetType intype = AssetTypeExtension.FromFilename(inputFile);

                    var filenameExtension = intype.Handler().Extension + _appArgs.outformat.Value.Handler().Extension;
                    var outputFile = Path.Combine(_appArgs.outfile, filenameNoExtensions + filenameExtension);

                    AssetType outtype = intype;
                    AssetFormat outformat = _appArgs.outformat.Value;

                    if (intype == AssetType.unknown)
                    {
                        throw new ArgumentException($"Asset type cannot be unknown. Input file {inputFile}, output file {outputFile}, intype {intype}, outtype {outtype}");
                    }

                    intype.Handler().ProcessAssetFile(inputFile, informat, outputFile, outformat);
                }
            }
            else
            {
                _appArgs.assettype.Value.Handler().ProcessAssetFile(_appArgs.infile, _appArgs.informat.Value, _appArgs.outfile, _appArgs.outformat.Value);
            }
        }

        private HandleRequest(string appname, string[] args)
        {
            this._appname = appname;

            var p = new FluentCommandLineParser<AppArgs>();

            p.Setup(arg => arg.infile)
                .As('i', "infile")
                .Required();

            p.Setup(arg => arg.outfile)
                .As('o', "outfile")
                .Required();

            p.Setup(arg => arg.informat)
                .As('f', "fromformat")
                .WithDescription("Specifies the format of the input file. Valid values are JSON or BYTES.");

            p.Setup(arg => arg.outformat)
                .As('t', "toformat")
                .WithDescription("Specifies the format of the output file. Valid values are JSON or BYTES.");

            var validAssetTypes = AssetTypeExtension.ValidOptionsString();
            p.Setup(arg => arg.assettype)
               .As('y', "type")
               .WithDescription($"Specifies the type of the thing. Valid values are '{validAssetTypes}'.")
               .Required();

            var result = p.Parse(args);
            _appArgs = p.Object;
        }

        public static HandleRequest InitWithArgs(string appname, string[] args)
        {
            try
            {
                return new HandleRequest(appname, args).Validate();
            }
            catch (Exception e)
            {
                Console.WriteLine(GetUsage(appname));
                Console.WriteLine(e.Message);
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private HandleRequest Validate()
        {
            if (string.IsNullOrEmpty(_appArgs.infile))
            {
                throw new ArgumentException("Input file is required.");
            }

            if (string.IsNullOrEmpty(_appArgs.outfile))
            {
                throw new ArgumentException("Output file is required.");
            }

            if (string.Equals(Path.GetFullPath(_appArgs.infile), Path.GetFullPath(_appArgs.outfile)))
            {
                throw new ArgumentException("The input file and output file cannot point to the same location.");
            }

            ValidateFiles();
            AssetFormatExtension.Validate();
            AssetTypeExtension.Validate();
            return this;
        }

        private void ValidateFiles()
        {
            bool infileIsDirectory = Directory.Exists(_appArgs.infile);
            bool outfileIsDirectory = Directory.Exists(_appArgs.outfile);

            if (infileIsDirectory != outfileIsDirectory)
            {
                throw new ArgumentException("Both 'infile' and 'outfile' must either be directories or files.");
            }

            if (!infileIsDirectory)
            {
                FileInfo inputFile = new FileInfo(_appArgs.infile);
                if (!inputFile.Exists)
                {
                    throw new FileNotFoundException($"Input file not found: {_appArgs.infile}");
                }

                FileInfo outputFile = new FileInfo(_appArgs.outfile);
                if (outputFile.Exists && (outputFile.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    throw new IOException($"Output file is read-only: {_appArgs.outfile}");
                }
                else if (!Directory.Exists(outputFile.DirectoryName))
                {
                    throw new DirectoryNotFoundException($"Output file directory not found: {outputFile.DirectoryName}");
                }

                if (!_appArgs.informat.HasValue)
                {
                    _appArgs.informat = AssetFormatExtension.FromFilename(inputFile);
                }

                if (!_appArgs.outformat.HasValue)
                {
                    _appArgs.outformat = AssetFormatExtension.FromFilename(outputFile);
                }

                if (_appArgs.informat == _appArgs.outformat || _appArgs.informat == AssetFormat.unknown)
                {
                    throw new ArgumentException($"Input and output formats must be different and cannot be unknown: informat {_appArgs.informat}, outformat {_appArgs.outformat}");
                }

                if (!_appArgs.assettype.HasValue)
                {
                    AssetType inassettype = AssetTypeExtension.FromFilename(inputFile);
                    AssetType outassettype = AssetTypeExtension.FromFilename(outputFile);

                    if (inassettype != outassettype || inassettype == AssetType.unknown)
                    {
                        throw new ArgumentException($"Input and output asset types must match and cannot be unknown: inassettype {inassettype}, outassettype {outassettype}");
                    }
                    _appArgs.assettype = inassettype;
                }
            }
            else
            {
                if (_appArgs.outformat == AssetFormat.unknown)
                {
                    throw new ArgumentException("When 'infile' is a directory, 'outformat' must be specified.");
                }
            }
        }
    }
}
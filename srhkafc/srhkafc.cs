using isogame;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using srafcshared;

namespace srhkafc
{
    public class srhkafc
    {
        public static string GetUsage()
        {
            var usageStringBuilder = new StringBuilder();
            usageStringBuilder.AppendLine("Usage:");
            usageStringBuilder.AppendLine("  -i, --infile      Required. Specifies the input file or directory.");
            usageStringBuilder.AppendLine("  -o, --outfile     Required. Specifies the output file or directory.");
            usageStringBuilder.AppendLine("  -f, --fromformat  Specifies the format of the input file. Valid values are JSON or BYTES.");
            usageStringBuilder.AppendLine("  -t, --toformat    Specifies the format of the output file. Valid values are JSON or BYTES.");

            var validthingtypes = string.Join(", ", Enum.GetNames(typeof(ThingType)));
            usageStringBuilder.AppendLine($"  -y, --type        Required. Specifies the type of the thing. Valid values are '{validthingtypes}'.");
            usageStringBuilder.AppendLine();
            usageStringBuilder.AppendLine("Example:");
            usageStringBuilder.AppendLine("  srhkafc -i input.json -o output.bytes -f JSON -t BYTES -y item");
            return usageStringBuilder.ToString();
        }

        public static void Main(string[] args)
        {
            try
            {
                HandleRequest hr = HandleRequest.InitWithArgs(args);
                hr.Process();
            }
            catch (Exception e)
            {
                Console.WriteLine(GetUsage());
                Console.WriteLine(e.Message);
                Console.WriteLine(e.ToString());
            }
        }
    }
}

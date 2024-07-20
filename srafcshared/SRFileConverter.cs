using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace srafcshared
{
    public static class SRFileConverter
    {
        public static byte[] LoadFileToBytes(string filename)
        {
            return File.ReadAllBytes(filename);
        }

        public static T Deserialize<T>(string filename) where T : class
        {
            ShadowrunSerializer srser = new ShadowrunSerializer();
            byte[] fileBytes = LoadFileToBytes(filename);
            using (Stream stream = new MemoryStream(fileBytes))
            {
                T tDef = srser.Deserialize(stream, null, typeof(T)) as T;
                return tDef;
            }
        }

        public static void Serialize<T>(string filename, T obj) where T : class
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                ShadowrunSerializer shadowrunSerializer = new ShadowrunSerializer();
                shadowrunSerializer.Serialize(fs, obj);
            }
        }

        public static string ConvertToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new ProtoBufJsonConverter(), new StringEnumConverter());
        }

        public static T ConvertFromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new ProtoBufJsonConverter(), new StringEnumConverter());
        }
    }
}

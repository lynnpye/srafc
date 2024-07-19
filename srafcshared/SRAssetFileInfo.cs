using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProtoBuf;
using System.Runtime.Serialization;

namespace srafcshared
{
    public class SRAssetFileInfo
    {
        private static SRAssetFileInfo _instance;
        private static readonly object _lock = new object();

        public static SRAssetFileInfo Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SRAssetFileInfo();
                        }
                    }
                }
                return _instance;
            }
        }

        public Dictionary<string, Type> TypeMap { get; private set; }

        private SRAssetFileInfo()
        {
            TypeMap = new Dictionary<string, Type>();
            PopulateTypeMap();
        }

        private void PopulateTypeMap()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => typeof(ProtoBuf.IExtensible).IsAssignableFrom(t) // Implements IExtensible
                                && t.IsDefined(typeof(SerializableAttribute), false) // Decorated with [Serializable]
                                && t.IsDefined(typeof(ProtoContractAttribute), false)); // Decorated with [ProtoContract]

                foreach (var type in types)
                {
                    TypeMap[type.Name] = type;
                }
            }
        }
    }
}

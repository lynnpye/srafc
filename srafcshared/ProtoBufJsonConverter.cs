using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace srafcshared
{
    public class ProtoBufJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IExtensible).IsAssignableFrom(objectType) &&
                   objectType.GetCustomAttributes(typeof(ProtoContractAttribute), false).Any();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            var type = value.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(ProtoMemberAttribute), false).Any());

            foreach (var property in properties)
            {
                var protoMember = (ProtoMemberAttribute)property.GetCustomAttributes(typeof(ProtoMemberAttribute), false).First();
                writer.WritePropertyName(protoMember.Name ?? property.Name);

                var propertyValue = property.GetValue(value, null);
                serializer.Serialize(writer, propertyValue);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = Activator.CreateInstance(objectType);
            var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(ProtoMemberAttribute), false).Any());

            var jsonObject = JObject.Load(reader);
            foreach (var property in properties)
            {
                try
                {
                    var protoMember = (ProtoMemberAttribute)property.GetCustomAttributes(typeof(ProtoMemberAttribute), false).First();
                    var propertyName = protoMember.Name ?? property.Name;
                    var token = jsonObject[propertyName];
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        if (property.GetSetMethod(true) != null)
                        {
                            var value = token.ToObject(property.PropertyType, serializer);
                            property.SetValue(obj, value, null);
                        }
                        else if (property.PropertyType.IsGenericType &&
                                 property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            // Look for a backing field with a name that matches the property name, prepended with an underscore
                            var fieldName = "_" + propertyName;
                            var field = objectType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                var listType = property.PropertyType.GetGenericArguments()[0];
                                var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                                field.SetValue(obj, list);

                                // Populate the list with values from JSON
                                var listValues = token.ToObject(property.PropertyType, serializer);
                                if (listValues != null)
                                {
                                    foreach (var item in (IEnumerable)listValues)
                                    {
                                        ((IList)list).Add(item);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error setting property {property.Name}: {ex}");
                }
            }

            return obj;
        }
    }
}

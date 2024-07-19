using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace srafcshared
{
    public static class ObjectComparer
    {
        public static bool DeepCompare<T>(T obj1, T obj2, out string differences)
        {
            List<string> diffList = new List<string>();
            DeepCompareInternal(obj1, obj2, "", diffList);
            differences = string.Join("\n", diffList.ToArray());
            return diffList.Count == 0;
        }

        private static void DeepCompareInternal(object obj1, object obj2, string path, List<string> diffList)
        {
            if (obj1 == null && obj2 == null) return;
            if (obj1 == null || obj2 == null)
            {
                diffList.Add(string.Format("{0}: One of the objects is null", path));
                return;
            }
            if (obj1.GetType() != obj2.GetType())
            {
                diffList.Add(string.Format("{0}: Type mismatch ({1} vs {2})", path, obj1.GetType(), obj2.GetType()));
                return;
            }

            Type type = obj1.GetType();
            // Compare all fields
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                object value1 = field.GetValue(obj1);
                object value2 = field.GetValue(obj2);
                string currentPath = string.IsNullOrEmpty(path) ? field.Name : string.Format("{0}.{1}", path, field.Name);

                ValuesAreEqual(value1, value2, currentPath, diffList);
            }

            // Compare all properties
            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!prop.CanRead) continue; // Skip properties that can't be read

                object value1 = prop.GetValue(obj1, null);
                object value2 = prop.GetValue(obj2, null);
                string currentPath = string.IsNullOrEmpty(path) ? prop.Name : string.Format("{0}.{1}", path, prop.Name);

                ValuesAreEqual(value1, value2, currentPath, diffList);
            }
        }

        private static void ValuesAreEqual(object value1, object value2, string path, List<string> diffList)
        {
            if (value1 == null && value2 == null) return;
            if (value1 == null || value2 == null)
            {
                diffList.Add(string.Format("{0}: One of the values is null", path));
                return;
            }

            // Handle comparison for collections
            if (value1 is IEnumerable && value2 is IEnumerable)
            {
                CollectionsAreEqual((IEnumerable)value1, (IEnumerable)value2, path, diffList);
                return;
            }

            // Recurse if it's a complex type
            if (value1.GetType().IsClass && !value1.GetType().Equals(typeof(string)))
            {
                DeepCompareInternal(value1, value2, path, diffList);
                return;
            }

            // Direct comparison for value types and strings
            if (!value1.Equals(value2))
            {
                diffList.Add(string.Format("{0}: {1} != {2}", path, value1, value2));
            }
        }

        private static void CollectionsAreEqual(IEnumerable col1, IEnumerable col2, string path, List<string> diffList)
        {
            IEnumerator enumerator1 = col1.GetEnumerator();
            IEnumerator enumerator2 = col2.GetEnumerator();
            int index = 0;

            while (true)
            {
                bool hasNext1 = enumerator1.MoveNext();
                bool hasNext2 = enumerator2.MoveNext();

                if (!hasNext1 || !hasNext2)
                {
                    if (hasNext1 != hasNext2)
                    {
                        diffList.Add(string.Format("{0}: Collection size differs", path));
                    }
                    break;
                }

                ValuesAreEqual(enumerator1.Current, enumerator2.Current, string.Format("{0}[{1}]", path, index), diffList);
                index++;
            }
        }
    }


}

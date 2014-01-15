using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace SjUpdater.XML
{
    public abstract class CustomXmlSerializerBase
    {
        static Dictionary<string, IDictionary<string, PropertyInfo>> propertyInfoCache = new Dictionary<string, IDictionary<string, PropertyInfo>>();        

        protected XmlDocument doc = new XmlDocument();

        protected static IDictionary<string, PropertyInfo> GetTypePropertyInfo(Type objType)
        {
            string typeName = objType.FullName;
            IDictionary<string, PropertyInfo> properties;
            if (!propertyInfoCache.TryGetValue(typeName, out properties))
            {
                // fetch fields
                PropertyInfo[] propertyInfos = objType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);

         
                Dictionary<string, PropertyInfo> dict = new Dictionary<string, PropertyInfo>(propertyInfos.Length);
                foreach (PropertyInfo property in propertyInfos)
                {
                    if ( !property.PropertyType.IsSubclassOf(typeof(MulticastDelegate)))
                    {
                        object[] attribs = property.GetCustomAttributes(typeof(XmlIgnoreAttribute), false);
                        if (attribs.Length == 0)
                        {
                            dict.Add(property.Name, property);
                        }
                    }
                }

                // check base class as well
                Type baseType = objType.BaseType;
                if (baseType != null && baseType != typeof(object))
                {
                    // should we include this base class?
                    object[] attribs = objType.GetCustomAttributes(typeof(XmlIgnoreBaseTypeAttribute), false);
                    if (attribs.Length == 0)
                    {
                        IDictionary<string, PropertyInfo> baseProperties = GetTypePropertyInfo(baseType);
                        // add fields
                        foreach (KeyValuePair<string, PropertyInfo> kv in baseProperties)
                        {
                            string key = kv.Key;
                            if (dict.ContainsKey(key))
                            {
                                // make field name unique
                                key = "base." + key;
                            }
                            dict.Add(key, kv.Value);
                        }
                    }
                }

                properties = dict;
                propertyInfoCache.Add(typeName, properties);
            }
            return properties;
        }

        protected class TypeInfo
        {
            internal int TypeId;
            internal XmlElement OnlyElement;

            internal void WriteTypeId(XmlElement element,bool isRuntimeType=false)
            {
                if(isRuntimeType)
                    element.SetAttribute("ctypeid", TypeId.ToString());
                else
                    element.SetAttribute("typeid", TypeId.ToString());
            }
        }        
    }
}

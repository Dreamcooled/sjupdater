using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace SjUpdater.XML
{
    public class CustomXmlDeserializer : CustomXmlSerializerBase
    {
        CultureInfo cult;
        Dictionary<int, Type> deserializationTypeCache = null;
        Dictionary<int, object> deserializationObjCache = new Dictionary<int, object>();
        ITypeConverter typeConverter;

        protected CustomXmlDeserializer(ITypeConverter typeConverter)
        {
            this.typeConverter = typeConverter;
        }

        public static object Deserialize(string xml, int maxSupportedVer, out int actualVersion)
        {
            return Deserialize(xml, maxSupportedVer, null,out actualVersion);
        }

        public static Type GetTypeOfContent(string xml)
        {
            CustomXmlDeserializer deserializer = new CustomXmlDeserializer(null);
            deserializer.doc.LoadXml(xml);
            string culture = deserializer.doc.DocumentElement.GetAttribute("culture");
            deserializer.cult = new CultureInfo(culture);
            XmlElement element = deserializer.doc.DocumentElement;
            Type objType;
            string typeFullName = element.GetAttribute("type");
            string assemblyFullName = element.GetAttribute("assembly");
            if (string.IsNullOrEmpty(assemblyFullName))
            {
                // type is directly loadable
                objType = Type.GetType(typeFullName, true);
            }
            else
            {
                Assembly asm = Assembly.Load(assemblyFullName);
                objType = asm.GetType(typeFullName, true);
            }
            return objType;    
        }
        public static object Deserialize(string xml, int maxSupportedVer, ITypeConverter typeConverter, out int actualVersion)
        {
            CustomXmlDeserializer deserializer = new CustomXmlDeserializer(typeConverter);
            deserializer.doc.LoadXml(xml);
            string version = deserializer.doc.DocumentElement.GetAttribute("version");
            actualVersion = Convert.ToInt32(version);
            if (maxSupportedVer < actualVersion)
            {
                return null;
            }
            string culture = deserializer.doc.DocumentElement.GetAttribute("culture");
            deserializer.cult = new CultureInfo(culture);
            return deserializer.DeserializeCore(deserializer.doc.DocumentElement);
        }

        void DeserializeComplexType(object obj, Type objType, XmlNode firstChild)
        {
            // complex type
            // get the class's fields                                
            IDictionary<string, PropertyInfo> dictProperties = GetTypePropertyInfo(objType);
            // set values for fields that are found

            try
            {
                for (XmlNode node = firstChild; node != null; node = node.NextSibling)
                {
                    string PropertyName = node.Name;
                    PropertyInfo property = null;
                    if (dictProperties.TryGetValue(PropertyName, out property))
                    {
                        // field is present, get value
                        object val = DeserializeCore((XmlElement)node);
                        // set value in object
                        property.SetValue(obj, val, null);
                    }
                }
            }
            catch (Exception ex)
            {
                
                throw new Exception(objType.ToString(),ex);
            }
         
        }

        void LoadTypeCache(XmlElement element)
        {
            XmlNodeList children = element.GetElementsByTagName("TypeInfo");
            deserializationTypeCache = new Dictionary<int, Type>(children.Count);
            foreach (XmlElement child in children)
            {
                int typeId = Convert.ToInt32(child.GetAttribute("typeid"));
                Type objType = InferTypeFromElement(child);
                deserializationTypeCache.Add(typeId, objType);
            }
        }

        object DeserializeCore(XmlElement element)
        {
            // check if this is a reference to another object
            int objId;
            if (int.TryParse(element.GetAttribute("id"), out objId))
            {
                object objCached = GetObjFromCache(objId);
                if (objCached != null)
                {
                    return objCached;
                }
            }
            else
            {
                objId = -1;
            }

            // check for null
            string value = element.GetAttribute("value");
            if (value == "null")
            {
                return null;
            }

            int subItems = element.ChildNodes.Count;
            XmlNode firstChild = element.FirstChild;

            // load type cache if available            
            if (element.GetAttribute("hasTypeCache") == "true")
            {
                LoadTypeCache((XmlElement)firstChild);
                subItems--;
                firstChild = firstChild.NextSibling;
            }
            // get type            
            Type objType;
            string typeId = element.GetAttribute("typeid");
            if (string.IsNullOrEmpty(typeId))
            {
                // no type id so type information must be present
                objType = InferTypeFromElement(element);
            }
            else
            {
                // there is a type id present
                objType = deserializationTypeCache[Convert.ToInt32(typeId)];
            }

            // process enum
            if (objType.IsEnum)
            {
                long val = Convert.ToInt64(value, cult);
                return Enum.ToObject(objType, val);
            }

            // process some simple types
            switch (Type.GetTypeCode(objType))
            {
                case TypeCode.Boolean: return Convert.ToBoolean(value, cult);
                case TypeCode.Byte: return Convert.ToByte(value, cult);
                case TypeCode.Char: return Convert.ToChar(value, cult);
                case TypeCode.DBNull: return DBNull.Value;
                case TypeCode.DateTime: return Convert.ToDateTime(value, cult);
                case TypeCode.Decimal: return Convert.ToDecimal(value, cult);
                case TypeCode.Double: return Convert.ToDouble(value, cult);
                case TypeCode.Int16: return Convert.ToInt16(value, cult);
                case TypeCode.Int32: return Convert.ToInt32(value, cult);
                case TypeCode.Int64: return Convert.ToInt64(value, cult);
                case TypeCode.SByte: return Convert.ToSByte(value, cult);
                case TypeCode.Single: return Convert.ToSingle(value, cult);
                case TypeCode.String: return value;
                case TypeCode.UInt16: return Convert.ToUInt16(value, cult);
                case TypeCode.UInt32: return Convert.ToUInt32(value, cult);
                case TypeCode.UInt64: return Convert.ToUInt64(value, cult);
            }            

            // our value
            object obj;

            if (objType.IsArray)
            {
                Type elementType = objType.GetElementType();
                MethodInfo setMethod = objType.GetMethod("Set", new Type[] { typeof(int), elementType });

                ConstructorInfo constructor = objType.GetConstructor(new Type[] { typeof(int) });
                obj = constructor.Invoke(new object[] { subItems });
                // add object to cache if necessary
                if (objId >= 0)
                {
                    deserializationObjCache.Add(objId, obj);
                }

                int i = 0;
                foreach (object val in ValuesFromNode(firstChild))
                {
                    setMethod.Invoke(obj, new object[] { i, val });
                    i++;
                }
                return obj;
            }
            if (typeof(Type).IsAssignableFrom(objType))
            {
                string ctypeId = element.GetAttribute("ctypeid");
                if (string.IsNullOrEmpty(ctypeId))
                {
                    // no type id so type information must be present
                    return InferTypeFromElement(element,true);
                }
                else
                {
                    // there is a type id present
                    return deserializationTypeCache[Convert.ToInt32(ctypeId)];
                }

            }
            // create a new instance of the object
            try
            {
                obj = Activator.CreateInstance(objType, true);
            }
            catch (Exception ex)
            {
                
                throw new Exception(objType.ToString(),ex);
            }
   
            // add object to cache if necessary
            if (objId >= 0)
            {
                deserializationObjCache.Add(objId, obj);
            }

            IXmlSerializable xmlSer = obj as IXmlSerializable;
            if (xmlSer == null)
            {
                IList lst = obj as IList;
                if (lst == null)
                {
                    IDictionary dict = obj as IDictionary;
                    if (dict == null)
                    {
                        if (objType == typeof(DictionaryEntry) ||
                            (objType.IsGenericType &&
                             objType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)))
                        {
                            // load all field contents in a dictionary
                            Dictionary<string, object> properties = new Dictionary<string, object>(element.ChildNodes.Count);
                            for (XmlNode node = firstChild; node != null; node = node.NextSibling)
                            {
                                object val = DeserializeCore((XmlElement)node);
                                properties.Add(node.Name, val);
                            }
                            // return the dictionary
                            return properties;
                        }
                        // complex type
                        DeserializeComplexType(obj, objType, firstChild);
                    }
                    else
                    {
                        // it's a dictionary
                        foreach (object val in ValuesFromNode(firstChild))
                        {
                            // should be a Dictionary                                    
                            Dictionary<string, object> dictVal = (Dictionary<string, object>)val;
                            if (dictVal.ContainsKey("Key"))
                            {
                                // should be a KeyValuePair
                                dict.Add(dictVal["Key"], dictVal["Value"]);
                            }
                            else
                            {
                                // should be a DictionaryEntry
                                dict.Add(dictVal["_Key"], dictVal["_Value"]);
                            }
                        }
                    }
                }
                else
                {
                    // it's a list
                    foreach (object val in ValuesFromNode(firstChild))
                    {
                        lst.Add(val);
                    }
                }
            }
            else
            {
                // the object can deserialize itself
                StringReader sr = new StringReader(element.InnerXml);
                XmlReader rd = XmlReader.Create(sr);
                xmlSer.ReadXml(rd);
                rd.Close();
                sr.Close();
            }
            return obj;
        }

        IEnumerable ValuesFromNode(XmlNode firstChild)
        {
            for (XmlNode node = firstChild; node != null; node = node.NextSibling)
            {
                yield return DeserializeCore((XmlElement)node);
            }
        }

        object GetObjFromCache(int objId)
        {
            object obj;
            if (deserializationObjCache.TryGetValue(objId, out obj))
            {
                return obj;
            }
            return null;
        }

        Type InferTypeFromElement(XmlElement element, bool isRuntimeType = false)
        {
            Type objType;
            string typeFullName; 
            string assemblyFullName; 
            if(isRuntimeType)
            {
                typeFullName = element.GetAttribute("ctype");
                assemblyFullName = element.GetAttribute("cassembly");
            }
            else
            {
                typeFullName = element.GetAttribute("type");
                assemblyFullName = element.GetAttribute("assembly");
            }

            if (typeConverter != null)
            {
                typeConverter.ProcessType(ref assemblyFullName, ref typeFullName);
            }            

            if (string.IsNullOrEmpty(assemblyFullName))
            {
                // type is directly loadable
                objType = Type.GetType(typeFullName, true);
            }
            else
            {
                Assembly asm = Assembly.Load(assemblyFullName);
                objType = asm.GetType(typeFullName, true);
            }
            return objType;
        }
    

        public interface ITypeConverter
        {
            void ProcessType(ref string assemblyFullName, ref string typeFullName);
        }
    }
}

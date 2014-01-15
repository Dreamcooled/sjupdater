using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace SjUpdater.XML
{
 

    public class XmlSerialization
    {
        public static Type GetTypeFromXml(string filename)
        {
            return CustomXmlDeserializer.GetTypeOfContent(XmlFileReader.ReadXmlFile(filename).OuterXml);
        }
        public static T LoadFromXml<T>(string filename)
        {
            try
            {
                // load XML document and parse it
                // deserialize a Test1 instance having a version number of at most 1
                T obj = (T)CustomXmlDeserializer.Deserialize(XmlFileReader.ReadXmlFile(filename).OuterXml, 1);
                return obj;
            }
            catch (Exception ex )
            {
                throw new Exception("Fehler beim Lesen von Xml Datei",ex);
            }
        }
        public static void SaveToXml(object o, string filename, bool encrypt = false)
        {
            try
            {
                XmlDocument doc = CustomXmlSerializer.Serialize(o, 1, "AMS_FILE");

                if (!encrypt)
                    doc.Save(filename);
                else
                {
                    FileStream fileStream = new FileStream(filename, FileMode.Create);
                    fileStream.Write(XmlFileReader.EncHeader, 0, 10);

                    GZipStream compressStream = new GZipStream(fileStream, CompressionMode.Compress);

                    doc.Save(compressStream);

                    compressStream.Close();
                    fileStream.Close();
                }
                    
            }
            catch (Exception ex)
            {
                throw new Exception("Fehler beim Schreiben zu Xml Datei",ex);
            }
        }
    }

    
    class XmlFileReader
    {
        static public byte[] EncHeader = new byte[] { 0xb1, 0x83, 0xc2, 0x06, 0x9f, 0x50, 0x46, 0xd1, 0x17, 0xd9 };
        //random byte genrator^^
        static public XmlDocument ReadXmlFile(string Filepath)
        {
            Stream inputstream = new FileStream(Filepath, FileMode.Open);
            
            byte[] data= new byte[10];
            inputstream.Read(data, 0, 10);
            
            bool isEncrypted = true;
            for (int i = 0; i < 10; i++)
            {
                if (data[i] != EncHeader[i])
                {
                    isEncrypted = false;
                    break;
                }
            }


            Stream outputstream;
            if (isEncrypted)
                outputstream = new GZipStream(inputstream, CompressionMode.Decompress); //We take the decompressed stream.
            else
            {
                inputstream.Seek(0, SeekOrigin.Begin);
                outputstream = inputstream; // We can just take the input stream
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(outputstream);

            outputstream.Close();

            return xmlDoc;
        }
    }
}

using CSVTransformWPF.CsvConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace CSVTransformWPF
{
    public class De_Serializer<T> where T : class
    {
        public T FromXml(string xmlFilePath)
        {
            // Check if the settings file exists
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException($"The xml file {xmlFilePath} does not exist.");
            }
            // Create an XML serializer for the class T
            XmlSerializer serializer = new(typeof(T));
            // Read the XML file and deserialize it into a T object
            using (StreamReader reader = new(xmlFilePath))
            {
                var obj = serializer.Deserialize(reader) ?? throw new XmlException("The XML file is empty / defective ");
                // check if the deserialized object is of type T
                if (obj is T obj1)
                {
                    return obj1;
                }
                else
                {
                    throw new XmlException("The XML file does not contain a CsvConverter object.");
                }
            }
        }

        public void ToXml(string xmlFilePath, T obj) 
        { 
            // Create an XML serializer for the class T
            XmlSerializer serializer = new(typeof(T));
            // Write the object to the XML file
            using (StreamWriter writer = new(xmlFilePath))
            {
                serializer.Serialize(writer, obj);
            }
        }
    }
}

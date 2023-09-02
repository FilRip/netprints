using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

using NetPrints.Core;

namespace NetPrints.Serialization
{
    public static class SerializationHelper
    {
        private static readonly DataContractSerializer classSerializer = new(typeof(ClassGraph), new DataContractSerializerSettings()
        {
            PreserveObjectReferences = true,
            MaxItemsInObjectGraph = int.MaxValue,
        });

        /// <summary>
        /// Saves a class to a path. The class can be loaded again using LoadClass.
        /// </summary>
        /// <param name="cls">Class to save.</param>
        /// <param name="outputPath">Path to save the class at.</param>
        public static void SaveClass(ClassGraph cls, string outputPath)
        {
            XmlWriterSettings settings = new()
            {
                Indent = true,
            };
            XmlWriter xw = XmlWriter.Create(outputPath, settings);
            classSerializer.WriteObject(xw, cls);
            xw.Flush();
            xw.Close();
            xw.Dispose();
        }

        /// <summary>
        /// Loads a class from a path.
        /// </summary>
        /// <param name="outputPath">Path to load the class from. Throws a FileLoadException if the read object was not a class.</param>
        public static ClassGraph LoadClass(string path)
        {
            using (FileStream fileStream = File.OpenRead(path))
            {
                if (classSerializer.ReadObject(fileStream) is ClassGraph cls)
                {
                    return cls;
                }
            }

            throw new FileLoadException();
        }
    }
}

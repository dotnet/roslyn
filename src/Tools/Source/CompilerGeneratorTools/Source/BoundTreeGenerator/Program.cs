// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace BoundTreeGenerator
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            string language;
            string infilename;
            string outfilename;
            TargetLanguage targetLanguage;

            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: \"{0} <language> <input> <output>\", where <language> is \"VB\" or \"CSharp\"", Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
                return 1;
            }

            language = args[0];
            infilename = args[1];
            outfilename = args[2];

            switch (language)
            {
                case "VB":
                    targetLanguage = TargetLanguage.VB;
                    break;
                case "CSharp":
                case "C#":
                    targetLanguage = TargetLanguage.CSharp;
                    break;
                default:
                    Console.Error.WriteLine("Language must be \"VB\" or \"CSharp\"");
                    return 1;
            }

            var serializer = new XmlSerializer(typeof(Tree));
            serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
            serializer.UnknownElement += new XmlElementEventHandler(serializer_UnknownElement);
            serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnreferencedObject += new UnreferencedObjectEventHandler(serializer_UnreferencedObject);

            Tree tree;
            using (var reader = new XmlTextReader(infilename) { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })
            {
                tree = (Tree)serializer.Deserialize(reader);
            }

            using (var outfile = new StreamWriter(File.Open(outfilename, FileMode.Create)))
            {
                BoundNodeClassWriter.Write(outfile, tree, targetLanguage);
            }

            return 0;
        }

        private static void serializer_UnreferencedObject(object sender, UnreferencedObjectEventArgs e)
        {
            Console.WriteLine("Unreferenced Object in XML deserialization");
        }

        private static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown node {0} at line {1}, col {2}", e.Name, e.LineNumber, e.LinePosition);
        }

        private static void serializer_UnknownElement(object sender, XmlElementEventArgs e)
        {
            Console.WriteLine("Unknown element {0} at line {1}, col {2}", e.Element.Name, e.LineNumber, e.LinePosition);
        }

        private static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            Console.WriteLine("Unknown attribute {0} at line {1}, col {2}", e.Attr.Name, e.LineNumber, e.LinePosition);
        }
    }
}

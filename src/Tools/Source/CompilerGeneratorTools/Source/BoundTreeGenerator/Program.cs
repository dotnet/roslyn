// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
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
                Console.Error.WriteLine("Usage: \"{0} <language> <input> <output>\", where <language> is \"VB\" or \"CSharp\"", Path.GetFileNameWithoutExtension(args[0]));
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

            Tree tree;
            var serializer = new XmlSerializer(typeof(Tree));
            using (var reader = XmlReader.Create(infilename, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            {
                tree = (Tree)serializer.Deserialize(reader);
            }

            if (!ValidateTree(tree))
            {
                Console.WriteLine("Validation failed. Stopping generation");
                return 1;
            }

            using (var outfile = new StreamWriter(File.Open(outfilename, FileMode.Create), Encoding.UTF8))
            {
                BoundNodeClassWriter.Write(outfile, tree, targetLanguage);
            }

            return 0;
        }

        private static bool ValidateTree(Tree tree)
        {
            bool success = true;
            foreach (var type in tree.Types)
            {
                if (type is not AbstractNode node)
                {
                    continue;
                }

                // BoundConversion is the only node that can have a `Conversion` field
                if (type.Name == "BoundConversion")
                {
                    continue;
                }

                foreach (var field in node.Fields)
                {
                    if (field.Type == "Conversion")
                    {
                        Console.WriteLine($"Error: {type.Name} has a field {field.Name} of type 'Conversion'. Types that are not BoundConversions" +
                                                 " should represent conversions as actual BoundConversion nodes, with placeholders if necessary.");
                        success = false;
                    }
                }
            }

            return success;
        }
    }
}

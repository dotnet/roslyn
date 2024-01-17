// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace BoundTreeGenerator
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: \"{0} <language> <input> <output>\", where <language> is \"VB\" or \"CSharp\"", Path.GetFileNameWithoutExtension(args[0]));
                return 1;
            }

            var language = args[0];
            var infilename = args[1];
            var outfilename = args[2];

            var targetLanguage = ParseTargetLanguage(language);

            Tree tree = LoadTreeXml(infilename);

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

        private static TargetLanguage ParseTargetLanguage(string languageName)
        {
            switch (languageName.ToLowerInvariant())
            {
                case "vb":
                case "visualbasic":
                    return TargetLanguage.VB;
                case "csharp":
                case "c#":
                    return TargetLanguage.CSharp;
                default:
                    throw new ArgumentOutOfRangeException("Language must be \"VB\" or \"CSharp\"");
            }
        }

        private static Tree LoadTreeXml(string infilename, bool useXmlSerializer = true)
        {
            using (var stream = new FileStream(infilename, FileMode.Open, FileAccess.Read))
            {
                var serializer = new XmlSerializer(typeof(Tree));
                using (var reader = XmlReader.Create(infilename, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
                {
                    var tree = (Tree)serializer.Deserialize(reader);
                    return tree;
                }
            }
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

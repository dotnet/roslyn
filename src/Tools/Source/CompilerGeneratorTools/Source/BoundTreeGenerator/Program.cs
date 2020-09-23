// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            using (var outfile = new StreamWriter(File.Open(outfilename, FileMode.Create), Encoding.UTF8))
            {
                BoundNodeClassWriter.Write(outfile, tree, targetLanguage);
            }

            return 0;
        }
    }
}

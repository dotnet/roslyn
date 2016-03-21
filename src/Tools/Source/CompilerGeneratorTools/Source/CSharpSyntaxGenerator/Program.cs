// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CSharpSyntaxGenerator
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                WriteUsage();
                return;
            }

            string inputFile = args[0];

            if (!File.Exists(inputFile))
            {
                Console.WriteLine(inputFile + " not found.");
                return;
            }

            bool writeSource = true;
            bool writeTests = false;
            bool writeSignatures = false;
            string outputFile = null;

            if (args.Length == 3)
            {
                outputFile = args[1];

                if (args[2] == "/test")
                {
                    writeTests = true;
                    writeSource = false;
                }
                else
                {
                    WriteUsage();
                    return;
                }
            }
            else if (args.Length == 2)
            {
                if (args[1] == "/sig")
                {
                    writeSignatures = true;
                }
                else
                {
                    outputFile = args[1];
                }
            }

            var reader = XmlReader.Create(inputFile, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            var serializer = new XmlSerializer(typeof(Tree));
            Tree tree = (Tree)serializer.Deserialize(reader);

            if (writeSignatures)
            {
                SignatureWriter.Write(Console.Out, tree);
            }
            else
            {
                if (writeSource)
                {
                    WriteToFile(tree, SourceWriter.Write, outputFile);
                }
                if (writeTests)
                {
                    WriteToFile(tree, TestWriter.Write, outputFile);
                }
            }
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Invalid usage");
            Console.WriteLine(typeof(Program).GetTypeInfo().Assembly.ManifestModule.Name + " input-file output-file [/write-test]");
        }

        private static void WriteToFile(Tree tree, Action<TextWriter, Tree> writeAction, string outputFile)
        {
            var stringBuilder = new StringBuilder();
            var writer = new StringWriter(stringBuilder);
            writeAction(writer, tree);

            var text = stringBuilder.ToString();
            int length;
            do
            {
                length = text.Length;
                text = text.Replace("{\r\n\r\n", "{\r\n");
            } while (text.Length != length);

            try
            {
                using (var outFile = new StreamWriter(File.Open(outputFile, FileMode.Create)))
                {
                    outFile.Write(text);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Unable to access {0}.  Is it checked out?", outputFile);
            }
        }
    }
}

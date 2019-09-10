// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace IOperationGenerator
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            string inFileName;
            string outFilePath;

            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: \"{0} <input> <output>\"", Path.GetFileNameWithoutExtension(args[0]));
                return 1;
            }

            inFileName = args[0];
            outFilePath = args[1];

            Tree tree;
            var serializer = new XmlSerializer(typeof(Tree));
            using (var reader = XmlReader.Create(inFileName, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            {
                tree = (Tree)serializer.Deserialize(reader);
            }

            IOperationClassWriter.Write(tree, outFilePath);

            return 0;
        }
    }
}

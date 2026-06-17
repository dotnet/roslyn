// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace IOperationGenerator;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args is not [string inFilePath, string outFilePath])
        {
            Console.Error.WriteLine("Usage: \"{0} <input> <output>\"", Path.GetFileNameWithoutExtension(args[0]));
            return 1;
        }

        return Generate(inFilePath, outFilePath);
    }

    public static int Generate(string inFilePath, string outFilePath)
    {
        Tree? tree;
        var serializer = new XmlSerializer(typeof(Tree));
        using (var reader = XmlReader.Create(inFilePath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
        {
            tree = (Tree?)serializer.Deserialize(reader);
        }

        if (tree is null)
        {
            Console.WriteLine("Deserialize returned null.");
            return 1;
        }

        return IOperationClassWriter.Write(tree, outFilePath) ? 0 : 1;
    }
}

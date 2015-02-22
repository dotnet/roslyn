// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Roslyn.Test.PdbUtilities;

internal static class PdbToXmlApp
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Pdb2Xml <file.dll> [<output file>] [/tokens] [/methodSpans]");
            return;
        }

        List<string> argList = new List<string>(args);
        var options = PdbToXmlOptions.ResolveTokens;

        if (argList.Remove("/tokens"))
        {
            options |= PdbToXmlOptions.IncludeTokens;
        }

        if (argList.Remove("/methodSpans"))
        {
            options |= PdbToXmlOptions.IncludeMethodSpans;
        }

        string file = argList[0];
        if (!File.Exists(file))
        {
            Console.WriteLine("File not found- {0}", file);
            return;
        }

        string pdbFile = Path.ChangeExtension(file, ".pdb");
        if (!File.Exists(pdbFile))
        {
            Console.WriteLine("PDB File not found- {0}", pdbFile);
            return;
        }

        string xmlFile;
        if (argList.Count > 1)
        {
            xmlFile = argList[1];
        }
        else
        {
            xmlFile = Path.ChangeExtension(pdbFile, ".xml");
        }

        if (File.Exists(xmlFile))
        {
            try
            {
                File.Delete(xmlFile);
            }
            catch { }
        }

        GenXmlFromPdb(file, pdbFile, xmlFile, options);

        Console.WriteLine("PDB dump written to {0}", xmlFile);
    }

    public static void GenXmlFromPdb(string exePath, string pdbPath, string outPath, PdbToXmlOptions options)
    {
        using (var exebits = new FileStream(exePath, FileMode.Open, FileAccess.Read))
        {
            using (var pdbbits = new FileStream(pdbPath, FileMode.Open, FileAccess.Read))
            {
                using (var sw = new StreamWriter(outPath, append: false, encoding: Encoding.UTF8))
                {
                    PdbToXmlConverter.ToXml(sw, pdbbits, exebits, options);
                }
            }
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Pdb2Pdb <PE file>");
                return 0;
            }

            string peFile = args[0];
            string nativePdbFile = Path.ChangeExtension(peFile, "pdb");
            string portablePdbFile = Path.ChangeExtension(peFile, ".pdbx");

            if (!File.Exists(peFile))
            {
                Console.WriteLine($"PE file not: {peFile}");
                return 1;
            }

            if (!File.Exists(nativePdbFile))
            {
                Console.WriteLine($"PDB file not: {nativePdbFile}");
                return 1;
            }

            using (var peStream = new FileStream(peFile, FileMode.Open, FileAccess.Read))
            {
                using (var nativePdbStream = new FileStream(nativePdbFile, FileMode.Open, FileAccess.Read))
                {
                    using (var portablePdbStream = new FileStream(portablePdbFile, FileMode.Create, FileAccess.ReadWrite))
                    {
                        PdbToPdb.Convert(peStream, nativePdbStream, portablePdbStream);
                    }
                }
            }

            return 0;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// We only include this file in the command line version for now which is the netcoreapp target
#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Internal.CSharpErrorFactsGenerator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(
@"Usage: CSharpErrorFactsGenerator.exe input output
  input     The path to ErrorCode.cs
  output    The path to GeneratedErrorFacts.cs");

                return -1;
            }

            string inputPath = args[0];
            string outputPath = args[1];

            var errorNames = File.ReadAllLines(inputPath).Select(l => l.Trim().Substring(0, Math.Max(l.IndexOf(' '), 0)));
            var outputText = ErrorGenerator.GetOutputText(errorNames);
            File.WriteAllText(outputPath, outputText, Encoding.UTF8);

            return 0;
        }
    }
}

#endif

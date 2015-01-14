// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Internal.CSharpErrorFactsGenerator
{
    public static class Program
    {
        public static void Main()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Length != 3)
            {
                Console.WriteLine(
@"Usage: {0} input output
  input     The path to ErrorCode.cs
  output    The path to GeneratedErrorFacts.cs",
                Path.GetFileNameWithoutExtension(args[0]));

                Environment.Exit(-1);
            }

            string inputPath = args[1];
            string outputPath = args[2];

            var outputText = new StringBuilder();
            outputText.AppendLine("namespace Microsoft.CodeAnalysis.CSharp");
            outputText.AppendLine("{");
            outputText.AppendLine("    internal static partial class ErrorFacts");
            outputText.AppendLine("    {");

            var warningCodeNames = new List<string>();
            var fatalCodeNames = new List<string>();
            var infoCodeNames = new List<string>();
            var hiddenCodeNames = new List<string>();
            foreach (var line in File.ReadAllLines(inputPath).Select(l => l.Trim()))
            {
                if (line.StartsWith("WRN_", StringComparison.OrdinalIgnoreCase))
                {
                    warningCodeNames.Add(line.Substring(0, line.IndexOf(' ')));
                }
                else if (line.StartsWith("FTL_", StringComparison.OrdinalIgnoreCase))
                {
                    fatalCodeNames.Add(line.Substring(0, line.IndexOf(' ')));
                }
                else if (line.StartsWith("INF_", StringComparison.OrdinalIgnoreCase))
                {
                    infoCodeNames.Add(line.Substring(0, line.IndexOf(' ')));
                }
                else if (line.StartsWith("HDN_", StringComparison.OrdinalIgnoreCase))
                {
                    hiddenCodeNames.Add(line.Substring(0, line.IndexOf(' ')));
                }
            }

            outputText.AppendLine("        public static bool IsWarning(ErrorCode code)");
            outputText.AppendLine("        {");
            outputText.AppendLine("            switch (code)");
            outputText.AppendLine("            {");
            foreach (var name in warningCodeNames)
            {
                outputText.Append("                case ErrorCode.");
                outputText.Append(name);
                outputText.AppendLine(":");
            }
            outputText.AppendLine("                    return true;");
            outputText.AppendLine("                default:");
            outputText.AppendLine("                    return false;");
            outputText.AppendLine("            }");
            outputText.AppendLine("        }");

            outputText.AppendLine();

            outputText.AppendLine("        public static bool IsFatal(ErrorCode code)");
            outputText.AppendLine("        {");
            outputText.AppendLine("            switch (code)");
            outputText.AppendLine("            {");
            foreach (var name in fatalCodeNames)
            {
                outputText.Append("                case ErrorCode.");
                outputText.Append(name);
                outputText.AppendLine(":");
            }
            outputText.AppendLine("                    return true;");
            outputText.AppendLine("                default:");
            outputText.AppendLine("                    return false;");
            outputText.AppendLine("            }");
            outputText.AppendLine("        }");

            outputText.AppendLine();

            outputText.AppendLine("        public static bool IsInfo(ErrorCode code)");
            outputText.AppendLine("        {");
            outputText.AppendLine("            switch (code)");
            outputText.AppendLine("            {");
            foreach (var name in infoCodeNames)
            {
                outputText.Append("                case ErrorCode.");
                outputText.Append(name);
                outputText.AppendLine(":");
            }
            outputText.AppendLine("                    return true;");
            outputText.AppendLine("                default:");
            outputText.AppendLine("                    return false;");
            outputText.AppendLine("            }");
            outputText.AppendLine("        }");

            outputText.AppendLine();

            outputText.AppendLine("        public static bool IsHidden(ErrorCode code)");
            outputText.AppendLine("        {");
            outputText.AppendLine("            switch (code)");
            outputText.AppendLine("            {");
            foreach (var name in hiddenCodeNames)
            {
                outputText.Append("                case ErrorCode.");
                outputText.Append(name);
                outputText.AppendLine(":");
            }
            outputText.AppendLine("                    return true;");
            outputText.AppendLine("                default:");
            outputText.AppendLine("                    return false;");
            outputText.AppendLine("            }");
            outputText.AppendLine("        }");

            outputText.AppendLine("    }");
            outputText.AppendLine("}");

            File.WriteAllText(outputPath, outputText.ToString());
        }
    }
}

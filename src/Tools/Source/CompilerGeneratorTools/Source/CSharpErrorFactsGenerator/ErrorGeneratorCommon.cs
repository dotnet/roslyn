// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Internal.CSharpErrorFactsGenerator
{
    public sealed partial class ErrorGenerator
    {
        public static string GetOutputText(ImmutableArray<string> errorNames)
        {
            var outputText = new StringBuilder();
            outputText.AppendLine("namespace Microsoft.CodeAnalysis.CSharp");
            outputText.AppendLine("{");
            outputText.AppendLine("    internal static partial class ErrorFacts");
            outputText.AppendLine("    {");

            var warningCodeNames = new List<string>();
            var fatalCodeNames = new List<string>();
            var infoCodeNames = new List<string>();
            var hiddenCodeNames = new List<string>();
            foreach (var errorName in errorNames)
            {
                if (errorName.StartsWith("WRN_", StringComparison.OrdinalIgnoreCase))
                {
                    warningCodeNames.Add(errorName);
                }
                else if (errorName.StartsWith("FTL_", StringComparison.OrdinalIgnoreCase))
                {
                    fatalCodeNames.Add(errorName);
                }
                else if (errorName.StartsWith("INF_", StringComparison.OrdinalIgnoreCase))
                {
                    infoCodeNames.Add(errorName);
                }
                else if (errorName.StartsWith("HDN_", StringComparison.OrdinalIgnoreCase))
                {
                    hiddenCodeNames.Add(errorName);
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

            return outputText.ToString();
        }
    }
}

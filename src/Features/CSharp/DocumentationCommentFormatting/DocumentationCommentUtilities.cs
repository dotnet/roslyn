// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationCommentFormatting
{
    internal static class DocumentationCommentUtilities
    {
        private static readonly string[] s_newLineStrings = new[] { "\r\n" };

        public static string ExtractXMLFragment(string input)
        {
            var splitLines = input.Split(s_newLineStrings, StringSplitOptions.None);

            for (int i = 0; i < splitLines.Length; i++)
            {
                if (splitLines[i].StartsWith("///", StringComparison.Ordinal))
                {
                    splitLines[i] = splitLines[i].Substring(3);
                }
            }

            return splitLines.Join("\r\n");
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    internal static class PublicApiFixHelpers
    {
        internal static TextDocument? GetUnshippedDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicApiAnalyzer.UnshippedFileName, StringComparison.Ordinal));
        }

        internal static TextDocument? GetShippedDocument(Project project)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(DeclarePublicApiAnalyzer.ShippedFileName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Returns the trailing newline from the end of <paramref name="sourceText"/>, if one exists.
        /// </summary>
        /// <param name="sourceText">The source text.</param>
        /// <returns><paramref name="endOfLine"/> if <paramref name="sourceText"/> ends with a trailing newline;
        /// otherwise, <see cref="string.Empty"/>.</returns>
        internal static string GetEndOfFileText(SourceText? sourceText, string endOfLine)
        {
            if (sourceText == null || sourceText.Length == 0)
                return string.Empty;

            var lastLine = sourceText.Lines[^1];
            return lastLine.Span.IsEmpty ? endOfLine : string.Empty;
        }

        internal static string GetEndOfLine(SourceText? sourceText)
        {
            if (sourceText?.Lines.Count > 1)
            {
                var firstLine = sourceText.Lines[0];
                return sourceText.ToString(new TextSpan(firstLine.End, firstLine.EndIncludingLineBreak - firstLine.End));
            }

            return Environment.NewLine;
        }
    }
}

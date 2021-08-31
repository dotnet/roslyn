// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    internal static class PublicApiFixHelpers
    {
        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value)
        {
            key = kv.Key;
            value = kv.Value;
        }

        internal static TextDocument? GetPublicApiDocument(Project project, string name)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(name, StringComparison.Ordinal));
        }

        internal static TextDocument? GetUnshippedDocument(Project project)
            => GetPublicApiDocument(project, DeclarePublicApiAnalyzer.UnshippedFileName);

        internal static TextDocument? GetShippedDocument(Project project)
            => GetPublicApiDocument(project, DeclarePublicApiAnalyzer.ShippedFileName);

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

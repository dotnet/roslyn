// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    internal static class PublicApiFixHelpers
    {
        private static readonly char[] SemicolonSplit = new[] { ';' };

        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value)
        {
            key = kv.Key;
            value = kv.Value;
        }

        private const string ApiDocEquivalenceKeyPrefix = "__ApiDoc__";

        internal static string CreateEquivalenceKey(this DocumentId? doc)
        {
            if (doc is null)
            {
                return $"{ApiDocEquivalenceKeyPrefix};;";
            }
            else
            {
                return $"{ApiDocEquivalenceKeyPrefix};{doc.ProjectId.Id};{doc.Id}";

            }
        }

        internal static DocumentId? CreateDocIdFromEquivalenceKey(this FixAllContext fixAllContext)
        {
            var split = fixAllContext.CodeActionEquivalenceKey.Split(SemicolonSplit, StringSplitOptions.RemoveEmptyEntries);

            if (split.Length == 3 &&
                string.Equals(split[0], ApiDocEquivalenceKeyPrefix, StringComparison.Ordinal) &&
                Guid.TryParse(split[1], out var projectGuid) && projectGuid != Guid.Empty &&
                Guid.TryParse(split[2], out var docGuid) && docGuid != Guid.Empty)
            {
                var projectId = ProjectId.CreateFromSerialized(projectGuid);
                return DocumentId.CreateFromSerialized(projectId, docGuid);
            }

            return null;
        }

        internal static TextDocument? GetPublicApiDocument(this Project project, string name)
        {
            return project.AdditionalDocuments.FirstOrDefault(doc => doc.Name.Equals(name, StringComparison.Ordinal));
        }

        internal static TextDocument? GetShippedDocument(this Project project)
            => project.GetPublicApiDocument(DeclarePublicApiAnalyzer.ShippedFileName);

        /// <summary>
        /// Returns the trailing newline from the end of <paramref name="sourceText"/>, if one exists.
        /// </summary>
        /// <param name="sourceText">The source text.</param>
        /// <returns><paramref name="endOfLine"/> if <paramref name="sourceText"/> ends with a trailing newline;
        /// otherwise, <see cref="string.Empty"/>.</returns>
        internal static string GetEndOfFileText(this SourceText? sourceText, string endOfLine)
        {
            if (sourceText == null || sourceText.Length == 0)
                return string.Empty;

            var lastLine = sourceText.Lines[^1];
            return lastLine.Span.IsEmpty ? endOfLine : string.Empty;
        }

        internal static string GetEndOfLine(this SourceText? sourceText)
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    internal static class PublicApiFixHelpers
    {
        private static readonly char[] SemicolonSplit = [';'];

        internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kv, out TKey key, out TValue value)
        {
            key = kv.Key;
            value = kv.Value;
        }

        private const string ApiDocEquivalenceKeyPrefix = "__ApiDoc__";

        internal static string CreateEquivalenceKey(this DocumentId? doc, bool isPublic)
        {
            if (doc is null)
            {
                return $"{ApiDocEquivalenceKeyPrefix};;;{isPublic}";
            }
            else
            {
                return $"{ApiDocEquivalenceKeyPrefix};{doc.ProjectId.Id};{doc.Id};{isPublic}";

            }
        }

        internal static DocumentId? CreateDocIdFromEquivalenceKey(this FixAllContext fixAllContext, out bool isPublic)
        {
            var equivalenceKey = fixAllContext.CodeActionEquivalenceKey;
            if (equivalenceKey is null)
            {
                isPublic = false;
                return null;
            }

            var split = equivalenceKey.Split(SemicolonSplit, StringSplitOptions.RemoveEmptyEntries);

            isPublic = bool.Parse(split[^1]);

            if (split is [ApiDocEquivalenceKeyPrefix, var projectGuidString, var docGuidString, _]
                && Guid.TryParse(projectGuidString, out var projectGuid) && projectGuid != Guid.Empty &&
                Guid.TryParse(docGuidString, out var docGuid) && docGuid != Guid.Empty)
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

        internal static TextDocument? GetShippedDocument(this Project project, bool isPublic)
            => project.GetPublicApiDocument(isPublic ? DeclarePublicApiAnalyzer.PublicShippedFileName : DeclarePublicApiAnalyzer.InternalShippedFileName);

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

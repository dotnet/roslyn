// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderHelper
    {
        protected AbstractFileHeaderHelper(ISyntaxFacts syntaxFacts)
        {
            SyntaxFacts = syntaxFacts;
        }

        /// <summary>
        /// Gets the text prefix indicating a single-line comment.
        /// </summary>
        public abstract string CommentPrefix { get; }

        protected abstract string GetTextContextOfComment(SyntaxTrivia commentTrivia);

        private ISyntaxFacts SyntaxFacts { get; }

        public FileHeader ParseFileHeader(SyntaxNode root)
        {
            var banner = SyntaxFacts.GetFileBanner(root);
            if (banner.Length == 0)
            {
                var missingHeaderOffset = root.GetLeadingTrivia().FirstOrDefault(t => t.IsDirective).FullSpan.End;
                return FileHeader.MissingFileHeader(missingHeaderOffset);
            }

            using var _ = PooledStringBuilder.GetInstance(out var sb);
            var fileHeaderStart = int.MaxValue;
            var fileHeaderEnd = int.MinValue;

            foreach (var trivia in banner)
            {
                if (SyntaxFacts.IsRegularComment(trivia))
                {
                    var comment = GetTextContextOfComment(trivia);
                    fileHeaderStart = Math.Min(trivia.FullSpan.Start, fileHeaderStart);
                    fileHeaderEnd = trivia.FullSpan.End;
                    sb.AppendLine(comment.Trim());
                }
            }

            return new FileHeader(sb.ToString(), fileHeaderStart, fileHeaderEnd, CommentPrefix.Length);
        }
    }
}

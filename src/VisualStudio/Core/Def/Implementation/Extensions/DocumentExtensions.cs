// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Extensions
{
    internal static class DocumentExtensions
    {
        public static IList<Tuple<TextSpan, uint>> GetVisibleCodeBlocks(this Document document, CancellationToken cancellationToken)
        {
            var codeBlocks = new List<Tuple<TextSpan, uint>>();

            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            int start = 0;
            uint cookie = 0;

            foreach (var line in text.Lines)
            {
                var trivia = document.GetSyntaxRootSynchronously(cancellationToken).FindTrivia(line.Start);

                // We should only see structured trivia here
                if (trivia.HasStructure)
                {
                    var directive = trivia.GetStructure();
                    if (syntaxFacts.IsDirective(directive))
                    {
                        ExternalSourceInfo info;
                        if (syntaxFacts.TryGetExternalSourceInfo(directive, out info))
                        {
                            // Is this start of a line directive? if so, then add this new entry
                            if (info.StartLine.HasValue)
                            {
                                // The token's .Value is a boxed integer, so we need to unbox and then cast to get a uint
                                cookie = (uint)info.StartLine.Value;
                                start = text.Lines[line.LineNumber + 1].Start;
                            }
                            else if (info.Ends)
                            {
                                // This is the end of this code block, so end TextSpanAndCookie we previously added
                                var previousLine = text.Lines[line.LineNumber - 1];

                                // If the #region and #endregion are on consecutive lines, we don't want to end up with
                                // end before start.
                                var end = Math.Max(start, previousLine.End);
                                codeBlocks.Add(Tuple.Create(TextSpan.FromBounds(start, end), cookie));
                            }
                        }
                    }
                }
            }

            return codeBlocks;
        }
    }
}

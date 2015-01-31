// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    internal static class BreakpointGetter
    {
        internal static async Task<BreakpointResolutionResult> GetBreakpointAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            TextSpan span;
            if (!TryGetBreakpointSpan(tree, position, cancellationToken, out span))
            {
                return null;
            }

            if (span.Length == 0)
            {
                return BreakpointResolutionResult.CreateLineResult(document);
            }

            return BreakpointResolutionResult.CreateSpanResult(document, span);
        }

        internal static bool TryGetBreakpointSpan(SyntaxTree tree, int position, CancellationToken cancellationToken, out TextSpan breakpointSpan)
        {
            var source = tree.GetText(cancellationToken);

            // If the line is entirely whitespace, then don't set any breakpoint there.
            var line = source.Lines.GetLineFromPosition(position);
            if (IsBlank(line))
            {
                breakpointSpan = default(TextSpan);
                return false;
            }

            // If the user is asking for breakpoint in an inactive region, then just create a line
            // breakpoint there.
            if (tree.IsInInactiveRegion(position, cancellationToken))
            {
                breakpointSpan = default(TextSpan);
                return true;
            }

            var root = tree.GetRoot(cancellationToken);
            return root.TryGetClosestBreakpointSpan(position, out breakpointSpan);
        }

        private static bool IsBlank(TextLine line)
        {
            var text = line.ToString();

            for (int i = 0; i < text.Length; i++)
            {
                if (!SyntaxFacts.IsWhitespace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

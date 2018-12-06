// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    /// <summary>
    /// Common implementation of all IWrappers.  This type takes care of a lot of common logic for
    /// all of them, including:
    /// 
    /// 1. Keeping track of code action invocations, allowing code actions to then be prioritized on
    ///    subsequent invocations.
    ///    
    /// 2. Checking nodes and tokens to make sure they are safe to be wrapped.
    /// 
    /// Individual subclasses may be targeted at specific syntactic forms.  For example, wrapping
    /// lists, or wrapping logical expressions.
    /// </summary>
    internal abstract partial class AbstractWrapper : IWrapper
    {
        public abstract Task<ICodeActionComputer> TryCreateComputerAsync(Document document, int position, SyntaxNode node, CancellationToken cancellationToken);

        protected static async Task<bool> ContainsUnformattableContentAsync(
            Document document, IEnumerable<SyntaxNodeOrToken> nodesAndTokens, CancellationToken cancellationToken)
        {
            // For now, don't offer if any item spans multiple lines.  We'll very likely screw up
            // formatting badly.  If this is really important to support, we can put in the effort
            // to properly move multi-line items around (which would involve properly fixing up the
            // indentation of lines within them.
            //
            // https://github.com/dotnet/roslyn/issues/31575
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in nodesAndTokens)
            {
                if (item == null ||
                    item.Span.IsEmpty)
                {
                    return true;
                }

                var firstToken = item.IsToken ? item.AsToken() : item.AsNode().GetFirstToken();
                var lastToken = item.IsToken ? item.AsToken() : item.AsNode().GetLastToken();

                if (!sourceText.AreOnSameLine(firstToken, lastToken))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

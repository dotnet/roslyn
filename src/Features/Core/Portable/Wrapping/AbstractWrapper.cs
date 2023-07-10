// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Wrapping
{

    /// <summary>
    /// Common implementation of all <see cref="ISyntaxWrapper"/>.  This type takes care of a lot of common logic for
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
    internal abstract partial class AbstractSyntaxWrapper : ISyntaxWrapper
    {
        protected IIndentationService IndentationService { get; }

        protected AbstractSyntaxWrapper(IIndentationService indentationService)
            => IndentationService = indentationService;

        public abstract Task<ICodeActionComputer?> TryCreateComputerAsync(
            Document document, int position, SyntaxNode node, SyntaxWrappingOptions options, bool containsSyntaxError, CancellationToken cancellationToken);

        protected static async Task<bool> ContainsUnformattableContentAsync(
            Document document, IEnumerable<SyntaxNodeOrToken> nodesAndTokens, CancellationToken cancellationToken)
        {
            // For now, don't offer if any item spans multiple lines.  We'll very likely screw up
            // formatting badly.  If this is really important to support, we can put in the effort
            // to properly move multi-line items around (which would involve properly fixing up the
            // indentation of lines within them.
            //
            // https://github.com/dotnet/roslyn/issues/31575
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            foreach (var item in nodesAndTokens)
            {
                if (item == null || item.Span.IsEmpty || item.IsMissing)
                    return true;

                var firstToken = item.IsToken ? item.AsToken() : item.AsNode()!.GetFirstToken();
                var lastToken = item.IsToken ? item.AsToken() : item.AsNode()!.GetLastToken();

                // Note: we check if things are on the same line, even in the case of a single token.
                // This is so that we don't try to wrap multiline tokens either (like a multi-line 
                // string).
                if (!sourceText.AreOnSameLine(firstToken, lastToken))
                    return true;
            }

            return false;
        }

        protected static bool ContainsOverlappingSyntaxErrror(SyntaxNode declaration, TextSpan headerSpan)
            => declaration.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error && d.Location.SourceSpan.OverlapsWith(headerSpan));
    }
}

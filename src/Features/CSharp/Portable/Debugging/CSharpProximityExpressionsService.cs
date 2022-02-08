// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
{
    /// <summary>
    /// Given a position in a source file, returns the expressions in close proximity that should
    /// show up in the debugger 'autos' window.  In general, the expressions we place into the autos
    /// window are things that appear to be 'side effect free'.  Note: because we only use the syntax
    /// tree for this, it's possible for us to get this wrong.  However, this should only happen in
    /// code that behaves unexpectedly.  For example, we will assume that "a + b" is side effect free
    /// (when in practice it may not be).  
    /// 
    /// The general tactic we take is to add the expressions for the statements on the
    /// line the debugger is currently at.  We will also try to find the 'previous' statement as well
    /// to add the expressions from that.  The 'previous' statement is a bit of an interesting beast.
    /// Consider, for example, if the user has just jumped out of a switch and is the statement
    /// directly following it.  What is the previous statement?  Without keeping state, there's no way
    /// to know.  So, in this case, we treat all 'exit points' (i.e. the last statement of a switch
    /// section) of the switch statement as the 'previous statement'.  There are many cases like this
    /// we need to handle.  Basically anything that might have nested statements/blocks might
    /// contribute to the 'previous statement'
    /// </summary>
    [ExportLanguageService(typeof(IProximityExpressionsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpProximityExpressionsService : IProximityExpressionsService
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpProximityExpressionsService()
        {
        }

        public async Task<bool> IsValidAsync(
            Document document,
            int position,
            string expressionValue,
            CancellationToken cancellationToken)
        {
            var expression = SyntaxFactory.ParseExpression(expressionValue);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            if (token.Kind() == SyntaxKind.CloseBraceToken && token.GetPreviousToken().Kind() != SyntaxKind.None)
            {
                token = token.GetPreviousToken();
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var info = semanticModel.GetSpeculativeSymbolInfo(token.SpanStart, expression, SpeculativeBindingOption.BindAsExpression);
            if (info.Symbol == null)
            {
                return false;
            }

            // We seem to have bound successfully.  However, if it bound to a local, then make
            // sure that that local isn't after the statement that we're currently looking at.  
            if (info.Symbol.Kind == SymbolKind.Local)
            {
                var statement = info.Symbol.Locations.First().FindToken(cancellationToken).GetAncestor<StatementSyntax>();
                if (statement != null && position < statement.SpanStart)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns null indicating a failure.
        /// </summary>
        public async Task<IList<string>> GetProximityExpressionsAsync(
            Document document,
            int position,
            CancellationToken cancellationToken)
        {
            try
            {
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                return GetProximityExpressions(tree, position, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }

        public static IList<string> GetProximityExpressions(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => new Worker(syntaxTree, position).Do(cancellationToken);

        [Obsolete($"Use {nameof(GetProximityExpressions)}.")]
        private static IList<string> Do(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            => new Worker(syntaxTree, position).Do(cancellationToken);

        private static void AddRelevantExpressions(
            StatementSyntax statement,
            IList<string> expressions,
            bool includeDeclarations)
        {
            new RelevantExpressionsCollector(includeDeclarations, expressions).Visit(statement);
        }
    }
}

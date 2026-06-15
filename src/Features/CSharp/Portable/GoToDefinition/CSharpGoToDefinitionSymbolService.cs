// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GoToDefinition;

[ExportLanguageService(typeof(IGoToDefinitionSymbolService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpGoToDefinitionSymbolService() : AbstractGoToDefinitionSymbolService
{
    protected override Task<ISymbol> FindRelatedExplicitlyDeclaredSymbolAsync(Project project, ISymbol symbol, CancellationToken cancellationToken)
        => Task.FromResult(symbol);

    protected override int? GetTargetPositionIfControlFlow(SemanticModel semanticModel, SyntaxToken token)
    {
        var node = token.GetRequiredParent();

        switch (token.Kind())
        {
            case SyntaxKind.ContinueKeyword:
                return GetBreakOrContinueTargetPosition(semanticModel, node, isBreak: false);

            case SyntaxKind.BreakKeyword:
                if (token.GetPreviousToken().IsKind(SyntaxKind.YieldKeyword))
                    goto case SyntaxKind.YieldKeyword;

                return GetBreakOrContinueTargetPosition(semanticModel, node, isBreak: true);

            case SyntaxKind.YieldKeyword:
            case SyntaxKind.ReturnKeyword:
                {
                    var foundReturnableConstruct = TryFindContainingReturnableConstruct(node);
                    if (foundReturnableConstruct is null)
                    {
                        return null;
                    }

                    var symbol = semanticModel.GetDeclaredSymbol(foundReturnableConstruct);
                    if (symbol is null)
                    {
                        // for lambdas
                        return foundReturnableConstruct.GetFirstToken().Span.Start;
                    }

                    return symbol.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0;
                }

            case SyntaxKind.GotoKeyword:
            case SyntaxKind.DefaultKeyword:
            case SyntaxKind.CaseKeyword:
                {
                    if (node.FirstAncestorOrSelf<GotoStatementSyntax>() is not GotoStatementSyntax gotoStatement)
                        return null;

                    if (semanticModel.GetOperation(gotoStatement) is not IBranchOperation gotoOperation)
                        return null;

                    var target = gotoOperation.Target;
                    return target.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.SpanStart;
                }
        }

        return null;

        static int? GetBreakOrContinueTargetPosition(SemanticModel semanticModel, SyntaxNode node, bool isBreak)
        {
            if (semanticModel.GetOperation(node) is not IBranchOperation branchOperation)
                return null;

            // 'corresponding' is the loop or switch that this break/continue transfers control to.
            var corresponding = branchOperation.GetCorrespondingOperation();
            if (corresponding is null)
                return null;

            // 'continue' transfers control back to the construct itself, so navigate to its start.
            if (!isBreak)
                return corresponding.Syntax.GetFirstToken().Span.Start;

            // 'break' resumes control at whatever follows the construct, so navigate to the start of the
            // next token after it. If the construct is the last thing in the file (nothing but
            // end-of-file follows), there's nothing to jump to, so fall back to the end of the construct.
            var lastToken = corresponding.Syntax.GetLastToken();
            var nextToken = lastToken.GetNextToken();
            return nextToken.Kind() is SyntaxKind.None or SyntaxKind.EndOfFileToken
                ? lastToken.Span.End
                : nextToken.Span.Start;
        }

        static SyntaxNode? TryFindContainingReturnableConstruct(SyntaxNode? node)
        {
            while (node is not null && !node.IsReturnableConstruct())
            {
                if (SyntaxFacts.GetTypeDeclarationKind(node.Kind()) != SyntaxKind.None)
                {
                    return null;
                }

                node = node.Parent;
            }

            return node;
        }
    }
}

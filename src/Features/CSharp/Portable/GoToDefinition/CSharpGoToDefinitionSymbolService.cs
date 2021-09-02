// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToDefinitionSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionSymbolService : AbstractGoToDefinitionSymbolService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGoToDefinitionSymbolService()
        {
        }

        protected override ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation)
            => symbol;

        protected override int? GetTargetPositionIfControlFlow(SemanticModel semanticModel, SyntaxToken token)
        {
            var node = token.GetRequiredParent();

            switch (token.Kind())
            {
                case SyntaxKind.ContinueKeyword:
                    var foundContinuedLoop = TryFindContinuableConstruct(node);

                    return foundContinuedLoop?.IsContinuableConstruct() == true
                        ? foundContinuedLoop.GetFirstToken().Span.Start
                        : null;

                case SyntaxKind.BreakKeyword:
                    if (token.GetPreviousToken().IsKind(SyntaxKind.YieldKeyword))
                    {
                        goto case SyntaxKind.YieldKeyword;
                    }

                    var foundBrokenLoop = TryFindBreakableConstruct(node);

                    return foundBrokenLoop?.IsBreakableConstruct() == true
                        ? foundBrokenLoop.GetLastToken().Span.End
                        : null;

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

                        return symbol.Locations.FirstOrNone().SourceSpan.Start;
                    }
            }

            return null;

            static SyntaxNode? TryFindContinuableConstruct(SyntaxNode? node)
            {
                while (node is not null && !node.IsContinuableConstruct())
                {
                    var kind = node.Kind();

                    if (node.IsReturnableConstruct() ||
                        SyntaxFacts.GetTypeDeclarationKind(kind) != SyntaxKind.None)
                    {
                        return null;
                    }

                    node = node.Parent;
                }

                return node;
            }

            static SyntaxNode? TryFindBreakableConstruct(SyntaxNode? node)
            {
                while (node is not null && !node.IsBreakableConstruct())
                {
                    if (node.IsReturnableConstruct() ||
                        SyntaxFacts.GetTypeDeclarationKind(node.Kind()) != SyntaxKind.None)
                    {
                        return null;
                    }

                    node = node.Parent;
                }

                return node;
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
}

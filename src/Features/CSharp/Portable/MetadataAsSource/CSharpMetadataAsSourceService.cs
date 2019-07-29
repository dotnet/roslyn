// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource
{
    internal class CSharpMetadataAsSourceService : AbstractMetadataAsSourceService
    {
        private static readonly AbstractFormattingRule s_memberSeparationRule = new FormattingRule();

        public CSharpMetadataAsSourceService(HostLanguageServices languageServices)
            : base(languageServices.GetService<ICodeGenerationService>())
        {
        }

        protected override async Task<Document> AddAssemblyInfoRegionAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken)
        {
            var assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
            var assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(symbolCompilation, symbol.ContainingAssembly);

            var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
                .WithTrailingTrivia(new[] { SyntaxFactory.Space, SyntaxFactory.PreprocessingMessage(assemblyInfo) });

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.WithLeadingTrivia(new[]
                {
                    SyntaxFactory.Trivia(regionTrivia),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("// " + assemblyPath),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia(true)),
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.CarriageReturnLineFeed
                });

            return document.WithSyntaxRoot(newRoot);
        }

        protected override IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document)
        {
            return s_memberSeparationRule.Concat(Formatter.GetDefaultFormattingRules(document));
        }

        protected override async Task<Document> ConvertDocCommentsToRegularComments(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken);

            return document.WithSyntaxRoot(newSyntaxRoot);
        }

        protected override ImmutableArray<AbstractReducer> GetReducers()
            => ImmutableArray.Create<AbstractReducer>(
                new CSharpNameReducer(),
                new CSharpEscapingReducer(),
                new CSharpParenthesesReducer(),
                new CSharpDefaultExpressionReducer());

        private class FormattingRule : AbstractMetadataFormattingRule
        {
            protected override AdjustNewLinesOperation GetAdjustNewLinesOperationBetweenMembersAndUsings(SyntaxToken token1, SyntaxToken token2)
            {
                var previousToken = token1;
                var currentToken = token2;

                // We are not between members or usings if the last token wasn't the end of a statement or if the current token
                // is the end of a scope.
                if ((previousToken.Kind() != SyntaxKind.SemicolonToken && previousToken.Kind() != SyntaxKind.CloseBraceToken) ||
                    currentToken.Kind() == SyntaxKind.CloseBraceToken)
                {
                    return null;
                }

                SyntaxNode previousMember = FormattingRangeHelper.GetEnclosingMember(previousToken);
                SyntaxNode nextMember = FormattingRangeHelper.GetEnclosingMember(currentToken);

                // Is the previous statement an using directive? If so, treat it like a member to add
                // the right number of lines.
                if (previousToken.Kind() == SyntaxKind.SemicolonToken && previousToken.Parent.Kind() == SyntaxKind.UsingDirective)
                {
                    previousMember = previousToken.Parent;
                }

                if (previousMember == null || nextMember == null || previousMember == nextMember)
                {
                    return null;
                }

                // If we have two members of the same kind, we won't insert a blank line 
                if (previousMember.Kind() == nextMember.Kind())
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }

                // Force a blank line between the two nodes by counting the number of lines of
                // trivia and adding one to it.
                var triviaList = token1.TrailingTrivia.Concat(token2.LeadingTrivia);
                return FormattingOperations.CreateAdjustNewLinesOperation(GetNumberOfLines(triviaList) + 1, AdjustNewLinesOption.ForceLines);
            }

            public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, in NextAnchorIndentationOperationAction nextOperation)
            {
                return;
            }

            protected override bool IsNewLine(char c)
            {
                return SyntaxFacts.IsNewLine(c);
            }
        }
    }
}

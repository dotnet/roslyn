// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.DocumentationCommentFormatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource
{
    internal class CSharpMetadataAsSourceService : AbstractMetadataAsSourceService
    {
        private static readonly IFormattingRule s_memberSeparationRule = new FormattingRule();

        public CSharpMetadataAsSourceService(HostLanguageServices languageServices)
            : base(languageServices.GetService<ICodeGenerationService>())
        {
        }

        protected override async Task<Document> AddAssemblyInfoRegionAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            string assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            string assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(compilation, symbol.ContainingAssembly);

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

        protected override IEnumerable<IFormattingRule> GetFormattingRules(Document document)
        {
            return s_memberSeparationRule.Concat(Formatter.GetDefaultFormattingRules(document));
        }

        protected override async Task<Document> ConvertDocCommentsToRegularComments(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken);

            return document.WithSyntaxRoot(newSyntaxRoot);
        }

        protected override IEnumerable<AbstractReducer> GetReducers()
        {
            yield return new CSharpNameReducer();
            yield return new CSharpEscapingReducer();
            yield return new CSharpParenthesesReducer();
        }

        private class FormattingRule : AbstractFormattingRule
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

            public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, NextAction<AnchorIndentationOperation> nextOperation)
            {
                return;
            }

            protected override bool IsNewLine(char c)
            {
                return SyntaxFacts.IsNewLine(c);
            }
        }

        private class DocCommentConverter : CSharpSyntaxRewriter
        {
            private readonly IDocumentationCommentFormattingService _formattingService;
            private readonly CancellationToken _cancellationToken;

            public static SyntaxNode ConvertToRegularComments(SyntaxNode node, IDocumentationCommentFormattingService formattingService, CancellationToken cancellationToken)
            {
                var converter = new DocCommentConverter(formattingService, cancellationToken);

                return converter.Visit(node);
            }

            private DocCommentConverter(IDocumentationCommentFormattingService formattingService, CancellationToken cancellationToken)
                : base(visitIntoStructuredTrivia: false)
            {
                _formattingService = formattingService;
                _cancellationToken = cancellationToken;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (node == null)
                {
                    return node;
                }

                // Process children first
                node = base.Visit(node);

                // Check the leading trivia for doc comments.
                if (node.GetLeadingTrivia().Any(SyntaxKind.SingleLineDocumentationCommentTrivia))
                {
                    var newLeadingTrivia = new List<SyntaxTrivia>();

                    foreach (var trivia in node.GetLeadingTrivia())
                    {
                        if (trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)
                        {
                            newLeadingTrivia.Add(SyntaxFactory.Comment("//"));
                            newLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);

                            var structuredTrivia = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
                            newLeadingTrivia.AddRange(ConvertDocCommentToRegularComment(structuredTrivia));
                        }
                        else
                        {
                            newLeadingTrivia.Add(trivia);
                        }
                    }

                    node = node.WithLeadingTrivia(newLeadingTrivia);
                }

                return node;
            }

            private IEnumerable<SyntaxTrivia> ConvertDocCommentToRegularComment(DocumentationCommentTriviaSyntax structuredTrivia)
            {
                var xmlFragment = DocumentationCommentUtilities.ExtractXMLFragment(structuredTrivia.ToFullString());

                var docComment = DocumentationComment.FromXmlFragment(xmlFragment);

                var commentLines = AbstractMetadataAsSourceService.DocCommentFormatter.Format(_formattingService, docComment);

                foreach (var line in commentLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return SyntaxFactory.Comment("// " + line);
                    }
                    else
                    {
                        yield return SyntaxFactory.Comment("//");
                    }

                    yield return SyntaxFactory.ElasticCarriageReturnLineFeed;
                }
            }
        }
    }
}

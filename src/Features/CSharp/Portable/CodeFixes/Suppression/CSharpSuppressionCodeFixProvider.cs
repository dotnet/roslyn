// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression
{
    [ExportSuppressionFixProvider(PredefinedCodeFixProviderNames.Suppression, LanguageNames.CSharp), Shared]
    internal class CSharpSuppressionCodeFixProvider : AbstractSuppressionCodeFixProvider
    {
        protected override Task<SyntaxTriviaList> CreatePragmaRestoreDirectiveTriviaAsync(Diagnostic diagnostic, Func<SyntaxNode, Task<SyntaxNode>> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine)
        {
            var restoreKeyword = SyntaxFactory.Token(SyntaxKind.RestoreKeyword);
            return CreatePragmaDirectiveTriviaAsync(restoreKeyword, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine);
        }

        protected override Task<SyntaxTriviaList> CreatePragmaDisableDirectiveTriviaAsync(
            Diagnostic diagnostic, Func<SyntaxNode, Task<SyntaxNode>> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine)
        {
            var disableKeyword = SyntaxFactory.Token(SyntaxKind.DisableKeyword);
            return CreatePragmaDirectiveTriviaAsync(disableKeyword, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine);
        }

        private async Task<SyntaxTriviaList> CreatePragmaDirectiveTriviaAsync(
            SyntaxToken disableOrRestoreKeyword, Diagnostic diagnostic, Func<SyntaxNode, Task<SyntaxNode>> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine)
        {
            var id = SyntaxFactory.IdentifierName(diagnostic.Id);
            var ids = new SeparatedSyntaxList<ExpressionSyntax>().Add(id);
            var pragmaDirective = SyntaxFactory.PragmaWarningDirectiveTrivia(disableOrRestoreKeyword, ids, true);
            pragmaDirective = (PragmaWarningDirectiveTriviaSyntax)await formatNode(pragmaDirective).ConfigureAwait(false);
            var pragmaDirectiveTrivia = SyntaxFactory.Trivia(pragmaDirective);
            var endOfLineTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
            var triviaList = SyntaxFactory.TriviaList(pragmaDirectiveTrivia);

            var title = diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture);
            if (!string.IsNullOrWhiteSpace(title))
            {
                var titleComment = SyntaxFactory.Comment(string.Format(" // {0}", title)).WithAdditionalAnnotations(Formatter.Annotation);
                triviaList = triviaList.Add(titleComment);
            }

            if (needsLeadingEndOfLine)
            {
                triviaList = triviaList.Insert(0, endOfLineTrivia);
            }

            if (needsTrailingEndOfLine)
            {
                triviaList = triviaList.Add(endOfLineTrivia);
            }

            return triviaList;
        }

        protected override string DefaultFileExtension
        {
            get
            {
                return ".cs";
            }
        }

        protected override string SingleLineCommentStart
        {
            get
            {
                return "//";
            }
        }

        protected override bool IsAttributeListWithAssemblyAttributes(SyntaxNode node)
        {
            var attributeList = node as AttributeListSyntax;
            return attributeList != null &&
                attributeList.Target != null &&
                attributeList.Target.Identifier.Kind() == SyntaxKind.AssemblyKeyword;
        }

        protected override bool IsEndOfLine(SyntaxTrivia trivia)
        {
            return trivia.Kind() == SyntaxKind.EndOfLineTrivia;
        }

        protected override bool IsEndOfFileToken(SyntaxToken token)
        {
            return token.Kind() == SyntaxKind.EndOfFileToken;
        }

        protected override async Task<SyntaxNode> AddGlobalSuppressMessageAttributeAsync(SyntaxNode newRoot, ISymbol targetSymbol, Diagnostic diagnostic, Workspace workspace, CancellationToken cancellationToken)
        {
            var compilationRoot = (CompilationUnitSyntax)newRoot;
            var isFirst = !compilationRoot.AttributeLists.Any();
            var leadingTriviaForAttributeList = isFirst && !compilationRoot.HasLeadingTrivia ?
                SyntaxFactory.TriviaList(SyntaxFactory.Comment(GlobalSuppressionsFileHeaderComment)) :
                default(SyntaxTriviaList);
            var attributeList = CreateAttributeList(targetSymbol, diagnostic, leadingTrivia: leadingTriviaForAttributeList, needsLeadingEndOfLine: !isFirst);
            attributeList = (AttributeListSyntax)await Formatter.FormatAsync(attributeList, workspace, cancellationToken: cancellationToken).ConfigureAwait(false);
            return compilationRoot.AddAttributeLists(attributeList);
        }

        private AttributeListSyntax CreateAttributeList(
            ISymbol targetSymbol,
            Diagnostic diagnostic,
            SyntaxTriviaList leadingTrivia,
            bool needsLeadingEndOfLine)
        {
            var attributeArguments = CreateAttributeArguments(targetSymbol, diagnostic);
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(SuppressMessageAttributeName), attributeArguments);
            var attributes = new SeparatedSyntaxList<AttributeSyntax>().Add(attribute);

            var targetSpecifier = SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword));
            var attributeList = SyntaxFactory.AttributeList(targetSpecifier, attributes);
            var endOfLineTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
            var triviaList = SyntaxFactory.TriviaList();

            if (needsLeadingEndOfLine)
            {
                triviaList = triviaList.Add(endOfLineTrivia);
            }

            return attributeList.WithLeadingTrivia(leadingTrivia.AddRange(triviaList));
        }

        private AttributeArgumentListSyntax CreateAttributeArguments(ISymbol targetSymbol, Diagnostic diagnostic)
        {
            // SuppressMessage("Rule Category", "Rule Id", Justification = "Justification", Scope = "Scope", Target = "Target")
            var category = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(diagnostic.Descriptor.Category));
            var categoryArgument = SyntaxFactory.AttributeArgument(category);

            var title = diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture);
            var ruleIdText = string.IsNullOrWhiteSpace(title) ? diagnostic.Id : string.Format("{0}:{1}", diagnostic.Id, title);
            var ruleId = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(ruleIdText));
            var ruleIdArgument = SyntaxFactory.AttributeArgument(ruleId);

            var justificationExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(FeaturesResources.SuppressionPendingJustification));
            var justificationArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Justification"), nameColon: null, expression: justificationExpr);

            var attributeArgumentList = SyntaxFactory.AttributeArgumentList().AddArguments(categoryArgument, ruleIdArgument, justificationArgument);

            var scopeString = GetScopeString(targetSymbol.Kind);
            if (scopeString != null)
            {
                var scopeExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(scopeString));
                var scopeArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Scope"), nameColon: null, expression: scopeExpr);

                var targetString = GetTargetString(targetSymbol);
                var targetExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(targetString));
                var targetArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Target"), nameColon: null, expression: targetExpr);

                attributeArgumentList = attributeArgumentList.AddArguments(scopeArgument, targetArgument);
            }

            return attributeArgumentList;
        }

        protected override bool IsSingleAttributeInAttributeList(SyntaxNode attribute)
        {
            var attributeSyntax = attribute as AttributeSyntax;
            if (attributeSyntax != null)
            {
                var attributeList = attributeSyntax.Parent as AttributeListSyntax;
                return attributeList != null && attributeList.Attributes.Count == 1;
            }

            return false;
        }

        protected override bool IsAnyPragmaDirectiveForId(SyntaxTrivia trivia, string id, out bool enableDirective, out bool hasMultipleIds)
        {
            if (trivia.Kind() == SyntaxKind.PragmaWarningDirectiveTrivia)
            {
                var pragmaWarning = (PragmaWarningDirectiveTriviaSyntax)trivia.GetStructure();
                enableDirective = pragmaWarning.DisableOrRestoreKeyword.Kind() == SyntaxKind.RestoreKeyword;
                hasMultipleIds = pragmaWarning.ErrorCodes.Count > 1;
                return pragmaWarning.ErrorCodes.Any(n => n.ToString() == id);
            }

            enableDirective = false;
            hasMultipleIds = false;
            return false;
        }

        protected override SyntaxTrivia TogglePragmaDirective(SyntaxTrivia trivia)
        {
            var pragmaWarning = (PragmaWarningDirectiveTriviaSyntax)trivia.GetStructure();
            var currentKeyword = pragmaWarning.DisableOrRestoreKeyword;
            var toggledKeywordKind = currentKeyword.Kind() == SyntaxKind.DisableKeyword ? SyntaxKind.RestoreKeyword : SyntaxKind.DisableKeyword;
            var toggledToken = SyntaxFactory.Token(currentKeyword.LeadingTrivia, toggledKeywordKind, currentKeyword.TrailingTrivia);
            var newPragmaWarning = pragmaWarning.WithDisableOrRestoreKeyword(toggledToken);
            return SyntaxFactory.Trivia(newPragmaWarning);
        }
    }
}

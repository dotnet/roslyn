// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression
{
    [ExportConfigurationFixProvider(PredefinedCodeFixProviderNames.Suppression, LanguageNames.CSharp), Shared]
    internal class CSharpSuppressionCodeFixProvider : AbstractSuppressionCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpSuppressionCodeFixProvider()
        {
        }

        protected override SyntaxTriviaList CreatePragmaRestoreDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine)
        {
            var restoreKeyword = SyntaxFactory.Token(SyntaxKind.RestoreKeyword);
            return CreatePragmaDirectiveTrivia(restoreKeyword, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine);
        }

        protected override SyntaxTriviaList CreatePragmaDisableDirectiveTrivia(
            Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine)
        {
            var disableKeyword = SyntaxFactory.Token(SyntaxKind.DisableKeyword);
            return CreatePragmaDirectiveTrivia(disableKeyword, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine);
        }

        private SyntaxTriviaList CreatePragmaDirectiveTrivia(
            SyntaxToken disableOrRestoreKeyword, Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine)
        {
            var diagnosticId = GetOrMapDiagnosticId(diagnostic, out var includeTitle);
            var id = SyntaxFactory.IdentifierName(diagnosticId);
            var ids = new SeparatedSyntaxList<ExpressionSyntax>().Add(id);
            var pragmaDirective = SyntaxFactory.PragmaWarningDirectiveTrivia(disableOrRestoreKeyword, ids, true);
            pragmaDirective = (PragmaWarningDirectiveTriviaSyntax)formatNode(pragmaDirective);
            var pragmaDirectiveTrivia = SyntaxFactory.Trivia(pragmaDirective);
            var endOfLineTrivia = SyntaxFactory.CarriageReturnLineFeed;
            var triviaList = SyntaxFactory.TriviaList(pragmaDirectiveTrivia);

            var title = includeTitle ? diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture) : null;
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

        protected override string DefaultFileExtension => ".cs";

        protected override string SingleLineCommentStart => "//";

        protected override bool IsAttributeListWithAssemblyAttributes(SyntaxNode node)
        {
            return node is AttributeListSyntax attributeList &&
                attributeList.Target != null &&
                attributeList.Target.Identifier.Kind() == SyntaxKind.AssemblyKeyword;
        }

        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.EndOfLineTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia);

        protected override bool IsEndOfFileToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.EndOfFileToken;

        protected override SyntaxNode AddGlobalSuppressMessageAttribute(SyntaxNode newRoot, ISymbol targetSymbol, Diagnostic diagnostic, Workspace workspace, CancellationToken cancellationToken)
        {
            var compilationRoot = (CompilationUnitSyntax)newRoot;
            var isFirst = !compilationRoot.AttributeLists.Any();
            var leadingTriviaForAttributeList = isFirst && !compilationRoot.HasLeadingTrivia ?
                SyntaxFactory.TriviaList(SyntaxFactory.Comment(GlobalSuppressionsFileHeaderComment)) :
                default;
            var attributeList = CreateAttributeList(targetSymbol, diagnostic, isAssemblyAttribute: true, leadingTrivia: leadingTriviaForAttributeList, needsLeadingEndOfLine: !isFirst);
            attributeList = (AttributeListSyntax)Formatter.Format(attributeList, workspace, cancellationToken: cancellationToken);
            return compilationRoot.AddAttributeLists(attributeList);
        }

        protected override SyntaxNode AddLocalSuppressMessageAttribute(SyntaxNode targetNode, ISymbol targetSymbol, Diagnostic diagnostic)
        {
            var memberNode = (MemberDeclarationSyntax)targetNode;

            SyntaxTriviaList leadingTriviaForAttributeList;
            bool needsLeadingEndOfLine;
            if (!memberNode.GetAttributes().Any())
            {
                leadingTriviaForAttributeList = memberNode.GetLeadingTrivia();
                memberNode = memberNode.WithoutLeadingTrivia();
                needsLeadingEndOfLine = !leadingTriviaForAttributeList.Any() || !IsEndOfLine(leadingTriviaForAttributeList.Last());
            }
            else
            {
                leadingTriviaForAttributeList = default;
                needsLeadingEndOfLine = true;
            }

            var attributeList = CreateAttributeList(targetSymbol, diagnostic, isAssemblyAttribute: false, leadingTrivia: leadingTriviaForAttributeList, needsLeadingEndOfLine: needsLeadingEndOfLine);
            return memberNode.AddAttributeLists(attributeList);
        }

        private AttributeListSyntax CreateAttributeList(
            ISymbol targetSymbol,
            Diagnostic diagnostic,
            bool isAssemblyAttribute,
            SyntaxTriviaList leadingTrivia,
            bool needsLeadingEndOfLine)
        {
            var attributeName = SyntaxFactory.ParseName(SuppressMessageAttributeName).WithAdditionalAnnotations(Simplifier.Annotation);
            var attributeArguments = CreateAttributeArguments(targetSymbol, diagnostic, isAssemblyAttribute);

            var attributes = new SeparatedSyntaxList<AttributeSyntax>()
                .Add(SyntaxFactory.Attribute(attributeName, attributeArguments));

            AttributeListSyntax attributeList;
            if (isAssemblyAttribute)
            {
                var targetSpecifier = SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword));
                attributeList = SyntaxFactory.AttributeList(targetSpecifier, attributes);
            }
            else
            {
                attributeList = SyntaxFactory.AttributeList(attributes);
            }

            var endOfLineTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
            var triviaList = SyntaxFactory.TriviaList();

            if (needsLeadingEndOfLine)
            {
                triviaList = triviaList.Add(endOfLineTrivia);
            }

            return attributeList.WithLeadingTrivia(leadingTrivia.AddRange(triviaList));
        }

        private AttributeArgumentListSyntax CreateAttributeArguments(ISymbol targetSymbol, Diagnostic diagnostic, bool isAssemblyAttribute)
        {
            // SuppressMessage("Rule Category", "Rule Id", Justification = nameof(Justification), Scope = nameof(Scope), Target = nameof(Target))
            var category = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(diagnostic.Descriptor.Category));
            var categoryArgument = SyntaxFactory.AttributeArgument(category);

            var title = diagnostic.Descriptor.Title.ToString(CultureInfo.CurrentUICulture);
            var ruleIdText = string.IsNullOrWhiteSpace(title) ? diagnostic.Id : string.Format("{0}:{1}", diagnostic.Id, title);
            var ruleId = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(ruleIdText));
            var ruleIdArgument = SyntaxFactory.AttributeArgument(ruleId);

            var justificationExpr = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(FeaturesResources.Pending));
            var justificationArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Justification"), nameColon: null, expression: justificationExpr);

            var attributeArgumentList = SyntaxFactory.AttributeArgumentList().AddArguments(categoryArgument, ruleIdArgument, justificationArgument);

            if (isAssemblyAttribute)
            {
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
            }

            return attributeArgumentList;
        }

        protected override bool IsSingleAttributeInAttributeList(SyntaxNode attribute)
        {
            if (attribute is AttributeSyntax attributeSyntax)
            {
                return attributeSyntax.Parent is AttributeListSyntax attributeList && attributeList.Attributes.Count == 1;
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

        protected override SyntaxToken GetAdjustedTokenForPragmaRestore(
            SyntaxToken token, SyntaxNode root, TextLineCollection lines, int indexOfLine)
        {
            var nextToken = token.GetNextToken();
            if (nextToken.Kind() == SyntaxKind.SemicolonToken &&
                nextToken.Parent is StatementSyntax statement &&
                statement.GetLastToken() == nextToken &&
                token.Parent.FirstAncestorOrSelf<StatementSyntax>() == statement)
            {
                // both the current and next tokens belong to the same statement, and the next token
                // is the final semicolon in a statement.  Do not put the pragma before that
                // semicolon.  Place it after the semicolon so the statement stays whole.

                return nextToken;
            }

            return token;
        }
    }
}

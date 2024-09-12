// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;

[ExportConfigurationFixProvider(PredefinedConfigurationFixProviderNames.Suppression, LanguageNames.CSharp), Shared]
internal class CSharpSuppressionCodeFixProvider : AbstractSuppressionCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpSuppressionCodeFixProvider()
    {
    }

    protected override SyntaxTriviaList CreatePragmaRestoreDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, CancellationToken, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine, CancellationToken cancellationToken)
    {
        var restoreKeyword = SyntaxFactory.Token(SyntaxKind.RestoreKeyword);
        return CreatePragmaDirectiveTrivia(restoreKeyword, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine, cancellationToken);
    }

    protected override SyntaxTriviaList CreatePragmaDisableDirectiveTrivia(
        Diagnostic diagnostic, Func<SyntaxNode, CancellationToken, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine, CancellationToken cancellationToken)
    {
        var disableKeyword = SyntaxFactory.Token(SyntaxKind.DisableKeyword);
        return CreatePragmaDirectiveTrivia(disableKeyword, diagnostic, formatNode, needsLeadingEndOfLine, needsTrailingEndOfLine, cancellationToken);
    }

    private static SyntaxTriviaList CreatePragmaDirectiveTrivia(
        SyntaxToken disableOrRestoreKeyword, Diagnostic diagnostic, Func<SyntaxNode, CancellationToken, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine, CancellationToken cancellationToken)
    {
        var diagnosticId = GetOrMapDiagnosticId(diagnostic, out var includeTitle);
        var id = SyntaxFactory.IdentifierName(diagnosticId);
        var ids = new SeparatedSyntaxList<ExpressionSyntax>().Add(id);
        var pragmaDirective = SyntaxFactory.PragmaWarningDirectiveTrivia(disableOrRestoreKeyword, ids, true);
        pragmaDirective = (PragmaWarningDirectiveTriviaSyntax)formatNode(pragmaDirective, cancellationToken);
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
        => trivia.Kind() is SyntaxKind.EndOfLineTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia;

    protected override bool IsEndOfFileToken(SyntaxToken token)
        => token.Kind() == SyntaxKind.EndOfFileToken;

    protected override SyntaxNode AddGlobalSuppressMessageAttribute(
        SyntaxNode newRoot,
        ISymbol targetSymbol,
        INamedTypeSymbol suppressMessageAttribute,
        Diagnostic diagnostic,
        SolutionServices services,
        SyntaxFormattingOptions options,
        IAddImportsService addImportsService,
        CancellationToken cancellationToken)
    {
        var compilationRoot = (CompilationUnitSyntax)newRoot;
        var isFirst = !compilationRoot.AttributeLists.Any();

        var attributeName = suppressMessageAttribute.GenerateNameSyntax()
                                                    .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

        compilationRoot = compilationRoot.AddAttributeLists(
            CreateAttributeList(
                targetSymbol,
                attributeName,
                diagnostic,
                isAssemblyAttribute: true,
                leadingTrivia: default));

        if (isFirst && !newRoot.HasLeadingTrivia)
            compilationRoot = compilationRoot.WithLeadingTrivia(SyntaxFactory.Comment(GlobalSuppressionsFileHeaderComment));

        return compilationRoot;
    }

    protected override SyntaxNode AddLocalSuppressMessageAttribute(
        SyntaxNode targetNode, ISymbol targetSymbol, INamedTypeSymbol suppressMessageAttribute, Diagnostic diagnostic)
    {
        var memberNode = (MemberDeclarationSyntax)targetNode;

        SyntaxTriviaList leadingTriviaForAttributeList;
        if (!memberNode.GetAttributes().Any())
        {
            leadingTriviaForAttributeList = memberNode.GetLeadingTrivia();
            memberNode = memberNode.WithoutLeadingTrivia();
        }
        else
        {
            leadingTriviaForAttributeList = default;
        }

        var attributeName = suppressMessageAttribute.GenerateNameSyntax();
        var attributeList = CreateAttributeList(
            targetSymbol, attributeName, diagnostic, isAssemblyAttribute: false, leadingTrivia: leadingTriviaForAttributeList);
        return memberNode.AddAttributeLists(attributeList);
    }

    private static AttributeListSyntax CreateAttributeList(
        ISymbol targetSymbol,
        NameSyntax attributeName,
        Diagnostic diagnostic,
        bool isAssemblyAttribute,
        SyntaxTriviaList leadingTrivia)
    {
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

        return attributeList.WithLeadingTrivia(leadingTrivia);
    }

    private static AttributeArgumentListSyntax CreateAttributeArguments(ISymbol targetSymbol, Diagnostic diagnostic, bool isAssemblyAttribute)
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

    protected override SyntaxNode GetContainingStatement(SyntaxToken token)
        // If we can't get a containing statement, such as for expression bodied members, then
        // return the arrow clause instead
        => (SyntaxNode)token.GetAncestor<StatementSyntax>() ?? token.GetAncestor<ArrowExpressionClauseSyntax>();

    protected override bool TokenHasTrailingLineContinuationChar(SyntaxToken token)
        => false;
}

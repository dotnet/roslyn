// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal abstract partial class CSharpSelectionResult : SelectionResult<StatementSyntax>
{
    public static async Task<CSharpSelectionResult> CreateAsync(
        TextSpan originalSpan,
        TextSpan finalSpan,
        bool selectionInExpression,
        SemanticDocument document,
        SyntaxToken firstToken,
        SyntaxToken lastToken,
        bool selectionChanged,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document);

        var firstTokenAnnotation = new SyntaxAnnotation();
        var lastTokenAnnotation = new SyntaxAnnotation();

        var root = await document.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newDocument = await SemanticDocument.CreateAsync(document.Document.WithSyntaxRoot(AddAnnotations(
            root,
            new[]
            {
                (firstToken, firstTokenAnnotation),
                (lastToken, lastTokenAnnotation)
            })), cancellationToken).ConfigureAwait(false);

        if (selectionInExpression)
        {
            return new ExpressionResult(
                originalSpan, finalSpan, selectionInExpression,
                newDocument, firstTokenAnnotation, lastTokenAnnotation, selectionChanged);
        }
        else
        {
            return new StatementResult(
                originalSpan, finalSpan, selectionInExpression,
                newDocument, firstTokenAnnotation, lastTokenAnnotation, selectionChanged);
        }
    }

    protected CSharpSelectionResult(
        TextSpan originalSpan,
        TextSpan finalSpan,
        bool selectionInExpression,
        SemanticDocument document,
        SyntaxAnnotation firstTokenAnnotation,
        SyntaxAnnotation lastTokenAnnotation,
        bool selectionChanged)
        : base(originalSpan, finalSpan, selectionInExpression,
               document, firstTokenAnnotation, lastTokenAnnotation, selectionChanged)
    {
    }

    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    public override SyntaxNode GetNodeForDataFlowAnalysis()
    {
        var node = base.GetNodeForDataFlowAnalysis();

        // If we're returning a value by ref we actually want to do the analysis on the underlying expression.
        return node is RefExpressionSyntax refExpression
            ? refExpression.Expression
            : node;
    }

    protected override bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken)
    {
        for (var current = token.Parent; current != null; current = current.Parent)
        {
            if (current is MemberDeclarationSyntax)
                return false;

            if (current is
                    SimpleLambdaExpressionSyntax or
                    ParenthesizedLambdaExpressionSyntax or
                    AnonymousMethodExpressionSyntax or
                    LocalFunctionStatementSyntax)
            {
                // make sure the selection contains the lambda
                return firstToken.SpanStart <= current.GetFirstToken().SpanStart &&
                    current.GetLastToken().Span.End <= lastToken.Span.End;
            }
        }

        return false;
    }

    public override SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken)
    {
        if (this.SelectionInExpression)
        {
            var container = this.GetInnermostStatementContainer();

            Contract.ThrowIfNull(container);
            Contract.ThrowIfFalse(container.IsStatementContainerNode() ||
                                  container is TypeDeclarationSyntax ||
                                  container is ConstructorDeclarationSyntax ||
                                  container is CompilationUnitSyntax);

            return container;
        }

        if (this.IsExtractMethodOnSingleStatement())
        {
            var firstStatement = this.GetFirstStatement();
            return firstStatement.Parent;
        }

        if (this.IsExtractMethodOnMultipleStatements())
        {
            var firstStatement = this.GetFirstStatementUnderContainer();
            var container = firstStatement.Parent;
            if (container is GlobalStatementSyntax)
                return container.Parent;

            return container;
        }

        throw ExceptionUtilities.Unreachable();
    }

    public override StatementSyntax GetFirstStatementUnderContainer()
    {
        Contract.ThrowIfTrue(SelectionInExpression);

        var firstToken = GetFirstTokenInSelection();
        var statement = firstToken.Parent.GetStatementUnderContainer();
        Contract.ThrowIfNull(statement);

        return statement;
    }

    public override StatementSyntax GetLastStatementUnderContainer()
    {
        Contract.ThrowIfTrue(SelectionInExpression);

        var lastToken = GetLastTokenInSelection();
        var statement = lastToken.Parent.GetStatementUnderContainer();

        Contract.ThrowIfNull(statement);
        var firstStatementUnderContainer = GetFirstStatementUnderContainer();
        Contract.ThrowIfFalse(CSharpSyntaxFacts.Instance.AreStatementsInSameContainer(statement, firstStatementUnderContainer));

        return statement;
    }

    public SyntaxNode GetInnermostStatementContainer()
    {
        Contract.ThrowIfFalse(SelectionInExpression);
        var containingScope = GetContainingScope();
        var statements = containingScope.GetAncestorsOrThis<StatementSyntax>();
        StatementSyntax last = null;

        foreach (var statement in statements)
        {
            if (statement.IsStatementContainerNode())
            {
                return statement;
            }

            last = statement;
        }

        // expression bodied member case
        var expressionBodiedMember = GetContainingScopeOf<ArrowExpressionClauseSyntax>();
        if (expressionBodiedMember != null)
        {
            // the class/struct declaration is the innermost statement container, since the 
            // member does not have a block body
            return GetContainingScopeOf<TypeDeclarationSyntax>();
        }

        // constructor initializer case
        var constructorInitializer = GetContainingScopeOf<ConstructorInitializerSyntax>();
        if (constructorInitializer != null)
        {
            return constructorInitializer.Parent;
        }

        // field initializer case
        var field = GetContainingScopeOf<FieldDeclarationSyntax>();
        if (field != null)
        {
            return field.Parent;
        }

        Contract.ThrowIfFalse(last.IsParentKind(SyntaxKind.GlobalStatement));
        Contract.ThrowIfFalse(last.Parent.IsParentKind(SyntaxKind.CompilationUnit));
        return last.Parent.Parent;
    }

    public bool ShouldPutUnsafeModifier()
    {
        var token = GetFirstTokenInSelection();
        var ancestors = token.GetAncestors<SyntaxNode>();

        // if enclosing type contains unsafe keyword, we don't need to put it again
        if (ancestors.Where(a => CSharp.SyntaxFacts.IsTypeDeclaration(a.Kind()))
                     .Cast<MemberDeclarationSyntax>()
                     .Any(m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword)))
        {
            return false;
        }

        return token.Parent.IsUnsafeContext();
    }

    public SyntaxKind UnderCheckedExpressionContext()
        => UnderCheckedContext<CheckedExpressionSyntax>();

    public SyntaxKind UnderCheckedStatementContext()
        => UnderCheckedContext<CheckedStatementSyntax>();

    private SyntaxKind UnderCheckedContext<T>() where T : SyntaxNode
    {
        var token = GetFirstTokenInSelection();
        var contextNode = token.Parent.GetAncestor<T>();
        if (contextNode == null)
        {
            return SyntaxKind.None;
        }

        return contextNode.Kind();
    }
}

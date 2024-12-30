// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
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

internal sealed partial class CSharpExtractMethodService
{
    internal abstract partial class CSharpSelectionResult(
        SemanticDocument document,
        SelectionType selectionType,
        TextSpan finalSpan)
        : SelectionResult(
            document, selectionType, finalSpan)
    {
        public static async Task<CSharpSelectionResult> CreateAsync(
            SemanticDocument document,
            FinalSelectionInfo selectionInfo,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(document);

            var root = await document.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDocument = await SemanticDocument.CreateAsync(document.Document.WithSyntaxRoot(AddAnnotations(
                root,
                [
                    (selectionInfo.FirstTokenInFinalSpan, s_firstTokenAnnotation),
                    (selectionInfo.LastTokenInFinalSpan, s_lastTokenAnnotation)
                ])), cancellationToken).ConfigureAwait(false);

            var selectionType = selectionInfo.GetSelectionType();
            var finalSpan = selectionInfo.FinalSpan;

            return selectionType == SelectionType.Expression
                ? new ExpressionResult(newDocument, selectionType, finalSpan)
                : new StatementResult(newDocument, selectionType, finalSpan);
        }

        protected override ISyntaxFacts SyntaxFacts
            => CSharpSyntaxFacts.Instance;

        protected override OperationStatus ValidateLanguageSpecificRules(CancellationToken cancellationToken)
        {
            // Nothing language specific for C#.
            return OperationStatus.SucceededStatus;
        }

        protected override SyntaxNode GetNodeForDataFlowAnalysis()
        {
            var node = base.GetNodeForDataFlowAnalysis();

            // If we're returning a value by ref we actually want to do the analysis on the underlying expression.
            return node is RefExpressionSyntax refExpression
                ? refExpression.Expression
                : node;
        }

        protected override bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken)
            => IsUnderAnonymousOrLocalMethod(token, firstToken, lastToken);

        public static bool IsUnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken)
        {
            for (var current = token.Parent; current != null; current = current.Parent)
            {
                if (current is MemberDeclarationSyntax)
                    return false;

                if (current is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
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
            if (this.IsExtractMethodOnExpression)
            {
                var container = this.GetInnermostStatementContainer();

                Contract.ThrowIfNull(container);
                Contract.ThrowIfFalse(
                    container.IsStatementContainerNode() ||
                    container is BaseListSyntax or TypeDeclarationSyntax or ConstructorDeclarationSyntax or CompilationUnitSyntax);

                return container;
            }

            if (this.IsExtractMethodOnSingleStatement)
            {
                var firstStatement = this.GetFirstStatement();
                return firstStatement.Parent;
            }

            if (this.IsExtractMethodOnMultipleStatements)
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
            Contract.ThrowIfTrue(IsExtractMethodOnExpression);

            var firstToken = GetFirstTokenInSelection();
            var statement = firstToken.Parent.GetStatementUnderContainer();
            Contract.ThrowIfNull(statement);

            return statement;
        }

        public override StatementSyntax GetLastStatementUnderContainer()
        {
            Contract.ThrowIfTrue(IsExtractMethodOnExpression);

            var lastToken = GetLastTokenInSelection();
            var statement = lastToken.Parent.GetStatementUnderContainer();

            return statement;
        }

        public SyntaxNode GetInnermostStatementContainer()
        {
            Contract.ThrowIfFalse(IsExtractMethodOnExpression);
            var containingScope = GetContainingScope();
            var statements = containingScope.GetAncestorsOrThis<StatementSyntax>();
            StatementSyntax last = null;

            foreach (var statement in statements)
            {
                if (statement.IsStatementContainerNode())
                    return statement;

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
                return constructorInitializer.Parent;

            // field initializer case
            var field = GetContainingScopeOf<FieldDeclarationSyntax>();
            if (field != null)
                return field.Parent;

            var primaryConstructorBaseType = GetContainingScopeOf<PrimaryConstructorBaseTypeSyntax>();
            if (primaryConstructorBaseType != null)
                return primaryConstructorBaseType.Parent;

            Contract.ThrowIfFalse(last.IsParentKind(SyntaxKind.GlobalStatement));
            Contract.ThrowIfFalse(last.Parent.IsParentKind(SyntaxKind.CompilationUnit));
            return last.Parent.Parent;
        }

        public override bool ContainsNonReturnExitPointsStatements(ImmutableArray<SyntaxNode> jumpsOutOfRegion)
            => jumpsOutOfRegion.Any(n => n is not ReturnStatementSyntax);

        public override ImmutableArray<StatementSyntax> GetOuterReturnStatements(SyntaxNode commonRoot, ImmutableArray<SyntaxNode> jumpsOutOfRegion)
        {
            var container = commonRoot.GetAncestorsOrThis<SyntaxNode>().Where(a => a.IsReturnableConstruct()).FirstOrDefault();
            if (container == null)
                return [];

            // now filter return statements to only include the one under outmost container
            return jumpsOutOfRegion
                .OfType<ReturnStatementSyntax>()
                .Select(returnStatement => (returnStatement, container: returnStatement.GetAncestors<SyntaxNode>().Where(a => a.IsReturnableConstruct()).FirstOrDefault()))
                .Where(p => p.container == container)
                .SelectAsArray(p => p.returnStatement)
                .CastArray<StatementSyntax>();
        }

        public override bool IsFinalSpanSemanticallyValidSpan(
            ImmutableArray<StatementSyntax> returnStatements, CancellationToken cancellationToken)
        {
            // return statement shouldn't contain any return value
            if (returnStatements.Cast<ReturnStatementSyntax>().Any(r => r.Expression != null))
                return false;

            var container = returnStatements.First().AncestorsAndSelf().FirstOrDefault(n => n.IsReturnableConstruct());
            if (container == null)
                return false;

            var body = container.GetBlockBody();
            if (body == null)
                return false;

            // make sure that next token of the last token in the selection is the close braces of containing block
            if (body.CloseBraceToken != GetLastTokenInSelection().GetNextToken(includeZeroWidth: true))
                return false;

            // alright, for these constructs, it must be okay to be extracted
            switch (container.Kind())
            {
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return true;
            }

            // now, only method is okay to be extracted out
            if (body.Parent is not MethodDeclarationSyntax method)
                return false;

            // make sure this method doesn't have return type.
            return method.ReturnType is PredefinedTypeSyntax p &&
                p.Keyword.Kind() == SyntaxKind.VoidKeyword;
        }
    }
}

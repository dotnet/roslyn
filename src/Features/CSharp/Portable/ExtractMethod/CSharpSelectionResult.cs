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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
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

        public override bool ContainsUnsupportedExitPointsStatements(ImmutableArray<SyntaxNode> exitPoints)
            => exitPoints.Any(n => n is not (BreakStatementSyntax or ContinueStatementSyntax or ReturnStatementSyntax));

        public override ImmutableArray<StatementSyntax> GetOuterReturnStatements(SyntaxNode commonRoot, ImmutableArray<SyntaxNode> exitPoints)
            => exitPoints.OfType<ReturnStatementSyntax>().ToImmutableArray().CastArray<StatementSyntax>();

        public override bool IsFinalSpanSemanticallyValidSpan(
            ImmutableArray<StatementSyntax> returnStatements, CancellationToken cancellationToken)
        {
            // Once we've gotten this far, everything is valid for us to return.  Only VB has special additional logic
            // it needs to apply at this point.
            return true;
        }
    }
}

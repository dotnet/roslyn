// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InlineTemporary), Shared]
    internal partial class InlineTemporaryCodeRefactoringProvider : CodeRefactoringProvider
    {
        internal static readonly SyntaxAnnotation DefinitionAnnotation = new SyntaxAnnotation();
        internal static readonly SyntaxAnnotation ReferenceAnnotation = new SyntaxAnnotation();
        internal static readonly SyntaxAnnotation InitializerAnnotation = new SyntaxAnnotation();
        internal static readonly SyntaxAnnotation ExpressionToInlineAnnotation = new SyntaxAnnotation();

        [ImportingConstructor]
        public InlineTemporaryCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var variableDeclarator = await context.TryGetRelevantNodeAsync<VariableDeclaratorSyntax>().ConfigureAwait(false);
            if (variableDeclarator == default)
            {
                return;
            }

            if (!variableDeclarator.IsParentKind(SyntaxKind.VariableDeclaration) ||
                !variableDeclarator.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement))
            {
                return;
            }

            var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
            if (variableDeclarator.Initializer == null ||
                variableDeclarator.Initializer.Value.IsMissing ||
                variableDeclarator.Initializer.Value.IsKind(SyntaxKind.StackAllocArrayCreationExpression))
            {
                return;
            }

            if (variableDeclaration.Type.Kind() == SyntaxKind.RefType)
            {
                // TODO: inlining ref returns:
                // https://github.com/dotnet/roslyn/issues/17132
                return;
            }

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
            if (localDeclarationStatement.ContainsDiagnostics ||
                localDeclarationStatement.UsingKeyword != default)
            {
                return;
            }

            var references = await GetReferencesAsync(document, variableDeclarator, cancellationToken).ConfigureAwait(false);
            if (!references.Any())
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Inline_temporary_variable,
                    c => this.InlineTemporaryAsync(document, variableDeclarator, c)));
        }

        private async Task<IEnumerable<ReferenceLocation>> GetReferencesAsync(
            Document document,
            VariableDeclaratorSyntax variableDeclarator,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var local = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

            if (local != null)
            {
                var findReferencesResult = await SymbolFinder.FindReferencesAsync(local, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                var locations = findReferencesResult.Single(r => Equals(r.Definition, local)).Locations;
                if (!locations.Any(loc => semanticModel.SyntaxTree.OverlapsHiddenPosition(loc.Location.SourceSpan, cancellationToken)))
                {
                    return locations;
                }
            }

            return SpecializedCollections.EmptyEnumerable<ReferenceLocation>();
        }

        private static bool HasConflict(IdentifierNameSyntax identifier, VariableDeclaratorSyntax variableDeclarator)
        {
            // TODO: Check for more conflict types.
            if (identifier.SpanStart < variableDeclarator.SpanStart)
            {
                return true;
            }

            var identifierNode = identifier
                .Ancestors()
                .TakeWhile(n => n.Kind() == SyntaxKind.ParenthesizedExpression || n.Kind() == SyntaxKind.CastExpression)
                .LastOrDefault();

            if (identifierNode == null)
            {
                identifierNode = identifier;
            }

            if (identifierNode.IsParentKind(SyntaxKind.Argument))
            {
                var argument = (ArgumentSyntax)identifierNode.Parent;
                if (argument.RefOrOutKeyword.Kind() != SyntaxKind.None)
                {
                    return true;
                }
            }
            else if (identifierNode.Parent.IsKind(
                SyntaxKind.PreDecrementExpression,
                SyntaxKind.PreIncrementExpression,
                SyntaxKind.PostDecrementExpression,
                SyntaxKind.PostIncrementExpression,
                SyntaxKind.AddressOfExpression))
            {
                return true;
            }
            else if (identifierNode.Parent is AssignmentExpressionSyntax binaryExpression)
            {
                if (binaryExpression.Left == identifierNode)
                {
                    return true;
                }
            }

            return false;
        }

        private static SyntaxAnnotation CreateConflictAnnotation()
        {
            return ConflictAnnotation.Create(CSharpFeaturesResources.Conflict_s_detected);
        }

        private async Task<Document> InlineTemporaryAsync(Document document, VariableDeclaratorSyntax declarator, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            // Annotate the variable declarator so that we can get back to it later.
            var updatedDocument = await document.ReplaceNodeAsync(declarator, declarator.WithAdditionalAnnotations(DefinitionAnnotation), cancellationToken).ConfigureAwait(false);
            var semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var variableDeclarator = await FindDeclaratorAsync(updatedDocument, cancellationToken).ConfigureAwait(false);

            // Create the expression that we're actually going to inline.
            var expressionToInline = await CreateExpressionToInlineAsync(variableDeclarator, updatedDocument, cancellationToken).ConfigureAwait(false);

            // Collect the identifier names for each reference.
            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
            var symbolRefs = await SymbolFinder.FindReferencesAsync(local, updatedDocument.Project.Solution, cancellationToken).ConfigureAwait(false);
            var references = symbolRefs.Single(r => Equals(r.Definition, local)).Locations;
            var syntaxRoot = await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Collect the topmost parenting expression for each reference.
            var nonConflictingIdentifierNodes = references
                .Select(loc => (IdentifierNameSyntax)syntaxRoot.FindToken(loc.Location.SourceSpan.Start).Parent)
                .Where(ident => !HasConflict(ident, variableDeclarator));

            // Add referenceAnnotations to identifier nodes being replaced.
            updatedDocument = await updatedDocument.ReplaceNodesAsync(
                nonConflictingIdentifierNodes,
                (o, n) => n.WithAdditionalAnnotations(ReferenceAnnotation),
                cancellationToken).ConfigureAwait(false);

            semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            variableDeclarator = await FindDeclaratorAsync(updatedDocument, cancellationToken).ConfigureAwait(false);

            // Get the annotated reference nodes.
            nonConflictingIdentifierNodes = await FindReferenceAnnotatedNodesAsync(updatedDocument, cancellationToken).ConfigureAwait(false);

            var topmostParentingExpressions = nonConflictingIdentifierNodes
                .Select(ident => GetTopMostParentingExpression(ident))
                .Distinct();

            var originalInitializerSymbolInfo = semanticModel.GetSymbolInfo(variableDeclarator.Initializer.Value, cancellationToken);

            // Make each topmost parenting statement or Equals Clause Expressions semantically explicit.
            updatedDocument = await updatedDocument.ReplaceNodesAsync(topmostParentingExpressions, (o, n) => Simplifier.Expand(n, semanticModel, workspace, cancellationToken: cancellationToken), cancellationToken).ConfigureAwait(false);
            semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticModelBeforeInline = semanticModel;

            variableDeclarator = await FindDeclaratorAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            var scope = GetScope(variableDeclarator);

            var newScope = ReferenceRewriter.Visit(semanticModel, scope, variableDeclarator, expressionToInline, cancellationToken);

            updatedDocument = await updatedDocument.ReplaceNodeAsync(scope, newScope, cancellationToken).ConfigureAwait(false);
            semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            variableDeclarator = await FindDeclaratorAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            newScope = GetScope(variableDeclarator);
            var conflicts = newScope.GetAnnotatedNodesAndTokens(ConflictAnnotation.Kind);
            var declaratorConflicts = variableDeclarator.GetAnnotatedNodesAndTokens(ConflictAnnotation.Kind);

            // Note that we only remove the local declaration if there weren't any conflicts,
            // unless those conflicts are inside the local declaration.
            if (conflicts.Count() == declaratorConflicts.Count())
            {
                // Certain semantic conflicts can be detected only after the reference rewriter has inlined the expression
                var newDocument = await DetectSemanticConflicts(updatedDocument,
                                                                semanticModel,
                                                                semanticModelBeforeInline,
                                                                originalInitializerSymbolInfo,
                                                                cancellationToken).ConfigureAwait(false);

                if (updatedDocument == newDocument)
                {
                    // No semantic conflicts, we can remove the definition.
                    updatedDocument = await updatedDocument.ReplaceNodeAsync(newScope, RemoveDeclaratorFromScope(variableDeclarator, newScope), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // There were some semantic conflicts, don't remove the definition.
                    updatedDocument = newDocument;
                }
            }

            return updatedDocument;
        }

        private static async Task<VariableDeclaratorSyntax> FindDeclaratorAsync(Document document, CancellationToken cancellationToken)
        {
            return await FindNodeWithAnnotationAsync<VariableDeclaratorSyntax>(document, DefinitionAnnotation, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ExpressionSyntax> FindInitializerAsync(Document document, CancellationToken cancellationToken)
        {
            return await FindNodeWithAnnotationAsync<ExpressionSyntax>(document, InitializerAnnotation, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<T> FindNodeWithAnnotationAsync<T>(Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
            where T : SyntaxNode
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root
                .GetAnnotatedNodesAndTokens(annotation)
                .Single()
                .AsNode() as T;
        }

        private static async Task<IEnumerable<IdentifierNameSyntax>> FindReferenceAnnotatedNodesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return FindReferenceAnnotatedNodes(root);
        }

        private static IEnumerable<IdentifierNameSyntax> FindReferenceAnnotatedNodes(SyntaxNode root)
        {
            var annotatedNodesAndTokens = root.GetAnnotatedNodesAndTokens(ReferenceAnnotation);
            foreach (var nodeOrToken in annotatedNodesAndTokens)
            {
                if (nodeOrToken.IsNode && nodeOrToken.AsNode().IsKind(SyntaxKind.IdentifierName))
                {
                    yield return (IdentifierNameSyntax)nodeOrToken.AsNode();
                }
            }
        }

        private SyntaxNode GetScope(VariableDeclaratorSyntax variableDeclarator)
        {
            var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
            var localDeclaration = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
            var scope = localDeclaration.Parent;

            while (scope.IsKind(SyntaxKind.LabeledStatement))
            {
                scope = scope.Parent;
            }

            var parentExpressions = scope.AncestorsAndSelf().OfType<ExpressionSyntax>();
            if (parentExpressions.Any())
            {
                scope = parentExpressions.LastOrDefault().Parent;
            }

            return scope;
        }

        private VariableDeclaratorSyntax FindDeclarator(SyntaxNode node)
        {
            var annotatedNodesOrTokens = node.GetAnnotatedNodesAndTokens(DefinitionAnnotation).ToList();
            Debug.Assert(annotatedNodesOrTokens.Count == 1, "Only a single variable declarator should have been annotated.");

            return (VariableDeclaratorSyntax)annotatedNodesOrTokens.First().AsNode();
        }

        private SyntaxNode RemoveDeclaratorFromVariableList(VariableDeclaratorSyntax variableDeclarator, VariableDeclarationSyntax variableDeclaration)
        {
            Debug.Assert(variableDeclaration.Variables.Count > 1);
            Debug.Assert(variableDeclaration.Variables.Contains(variableDeclarator));

            var localDeclaration = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
            var scope = GetScope(variableDeclarator);

            var newLocalDeclaration = variableDeclarator.GetLeadingTrivia().Any(t => t.IsDirective)
                ? localDeclaration.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepExteriorTrivia)
                : localDeclaration.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepNoTrivia);

            return scope.ReplaceNode(
                localDeclaration,
                newLocalDeclaration.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private SyntaxNode RemoveDeclaratorFromScope(VariableDeclaratorSyntax variableDeclarator, SyntaxNode scope)
        {
            var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;

            // If there is more than one variable declarator, remove this one from the variable declaration.
            if (variableDeclaration.Variables.Count > 1)
            {
                return RemoveDeclaratorFromVariableList(variableDeclarator, variableDeclaration);
            }

            var localDeclaration = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;

            // There's only one variable declarator, so we'll remove the local declaration
            // statement entirely. This means that we'll concatenate the leading and trailing
            // trivia of this declaration and move it to the next statement.
            var leadingTrivia = localDeclaration
                .GetLeadingTrivia()
                .Reverse()
                .SkipWhile(t => t.MatchesKind(SyntaxKind.WhitespaceTrivia))
                .Reverse()
                .ToSyntaxTriviaList();

            var trailingTrivia = localDeclaration
                .GetTrailingTrivia()
                .SkipWhile(t => t.MatchesKind(SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia))
                .ToSyntaxTriviaList();

            var newLeadingTrivia = leadingTrivia.Concat(trailingTrivia);

            var nextToken = localDeclaration.GetLastToken().GetNextTokenOrEndOfFile();
            var newNextToken = nextToken.WithPrependedLeadingTrivia(newLeadingTrivia)
                                        .WithAdditionalAnnotations(Formatter.Annotation);

            var newScope = scope.ReplaceToken(nextToken, newNextToken);

            var newLocalDeclaration = (LocalDeclarationStatementSyntax)FindDeclarator(newScope).Parent.Parent;

            // If the local is parented by a label statement, we can't remove this statement. Instead,
            // we'll replace the local declaration with an empty expression statement.
            if (newLocalDeclaration.IsParentKind(SyntaxKind.LabeledStatement))
            {
                var labeledStatement = (LabeledStatementSyntax)newLocalDeclaration.Parent;
                var newLabeledStatement = labeledStatement.ReplaceNode(newLocalDeclaration, SyntaxFactory.ParseStatement(""));

                return newScope.ReplaceNode(labeledStatement, newLabeledStatement);
            }

            return newScope.RemoveNode(newLocalDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
        }

        private ExpressionSyntax SkipRedundantExteriorParentheses(ExpressionSyntax expression)
        {
            while (expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                var parenthesized = (ParenthesizedExpressionSyntax)expression;
                if (parenthesized.Expression == null ||
                    parenthesized.Expression.IsMissing)
                {
                    break;
                }

                if (parenthesized.Expression.IsKind(SyntaxKind.ParenthesizedExpression) ||
                    parenthesized.Expression.IsKind(SyntaxKind.IdentifierName))
                {
                    expression = parenthesized.Expression;
                }
                else
                {
                    break;
                }
            }

            return expression;
        }

        private async Task<ExpressionSyntax> CreateExpressionToInlineAsync(
            VariableDeclaratorSyntax variableDeclarator,
            Document document,
            CancellationToken cancellationToken)
        {
            var updatedDocument = document;

            var expression = SkipRedundantExteriorParentheses(variableDeclarator.Initializer.Value);
            var semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
            var newExpression = InitializerRewriter.Visit(expression, localSymbol, semanticModel);

            // If this is an array initializer, we need to transform it into an array creation
            // expression for inlining.
            if (newExpression.Kind() == SyntaxKind.ArrayInitializerExpression)
            {
                var arrayType = (ArrayTypeSyntax)localSymbol.Type.GenerateTypeSyntax();
                var arrayInitializer = (InitializerExpressionSyntax)newExpression;

                // Add any non-whitespace trailing trivia from the equals clause to the type.
                var equalsToken = variableDeclarator.Initializer.EqualsToken;
                if (equalsToken.HasTrailingTrivia)
                {
                    var trailingTrivia = equalsToken.TrailingTrivia.SkipInitialWhitespace();
                    if (trailingTrivia.Any())
                    {
                        arrayType = arrayType.WithTrailingTrivia(trailingTrivia);
                    }
                }

                newExpression = SyntaxFactory.ArrayCreationExpression(arrayType, arrayInitializer);
            }

            newExpression = newExpression.WithAdditionalAnnotations(InitializerAnnotation);

            updatedDocument = await updatedDocument.ReplaceNodeAsync(variableDeclarator.Initializer.Value, newExpression, cancellationToken).ConfigureAwait(false);
            semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            newExpression = await FindInitializerAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            var newVariableDeclarator = await FindDeclaratorAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(newVariableDeclarator, cancellationToken);

            var explicitCastExpression = newExpression.CastIfPossible(localSymbol.Type, newVariableDeclarator.SpanStart, semanticModel);
            if (explicitCastExpression != newExpression)
            {
                updatedDocument = await updatedDocument.ReplaceNodeAsync(newExpression, explicitCastExpression, cancellationToken).ConfigureAwait(false);
                semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                newVariableDeclarator = await FindDeclaratorAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            }

            // Now that the variable declarator is normalized, make its initializer
            // value semantically explicit.
            newExpression = await Simplifier.ExpandAsync(newVariableDeclarator.Initializer.Value, updatedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            return newExpression.WithAdditionalAnnotations(ExpressionToInlineAnnotation);
        }

        private static SyntaxNode GetTopMostParentingExpression(ExpressionSyntax expression)
        {
            return expression.AncestorsAndSelf().OfType<ExpressionSyntax>().Last();
        }

        private static async Task<Document> DetectSemanticConflicts(
            Document inlinedDocument,
            SemanticModel newSemanticModelForInlinedDocument,
            SemanticModel semanticModelBeforeInline,
            SymbolInfo originalInitializerSymbolInfo,
            CancellationToken cancellationToken)
        {
            // In this method we detect if inlining the expression introduced the following semantic change:
            // The symbol info associated with any of the inlined expressions does not match the symbol info for original initializer expression prior to inline.

            // If any semantic changes were introduced by inlining, we update the document with conflict annotations.
            // Otherwise we return the given inlined document without any changes.

            var syntaxRootBeforeInline = await semanticModelBeforeInline.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Get all the identifier nodes which were replaced with inlined expression.
            var originalIdentifierNodes = FindReferenceAnnotatedNodes(syntaxRootBeforeInline);

            if (originalIdentifierNodes.IsEmpty())
            {
                // No conflicts
                return inlinedDocument;
            }

            // Get all the inlined expression nodes.
            var syntaxRootAfterInline = await inlinedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var inlinedExprNodes = syntaxRootAfterInline.GetAnnotatedNodesAndTokens(ExpressionToInlineAnnotation);
            Debug.Assert(originalIdentifierNodes.Count() == inlinedExprNodes.Count());

            Dictionary<SyntaxNode, SyntaxNode> replacementNodesWithChangedSemantics = null;
            using (var originalNodesEnum = originalIdentifierNodes.GetEnumerator())
            {
                using var inlinedNodesOrTokensEnum = inlinedExprNodes.GetEnumerator();

                while (originalNodesEnum.MoveNext())
                {
                    inlinedNodesOrTokensEnum.MoveNext();
                    var originalNode = originalNodesEnum.Current;

                    // expressionToInline is Parenthesized prior to replacement, so get the parenting parenthesized expression.
                    var inlinedNode = (ExpressionSyntax)inlinedNodesOrTokensEnum.Current.Parent;
                    Debug.Assert(inlinedNode.IsKind(SyntaxKind.ParenthesizedExpression));

                    // inlinedNode is the expanded form of the actual initializer expression in the original document.
                    // We have annotated the inner initializer with a special syntax annotation "InitializerAnnotation".
                    // Get this annotated node and compute the symbol info for this node in the inlined document.
                    var innerInitializerInInlineNodeOrToken = inlinedNode.GetAnnotatedNodesAndTokens(InitializerAnnotation).First();

                    var innerInitializerInInlineNode = (ExpressionSyntax)(innerInitializerInInlineNodeOrToken.IsNode ?
                        innerInitializerInInlineNodeOrToken.AsNode() :
                        innerInitializerInInlineNodeOrToken.AsToken().Parent);
                    var newInitializerSymbolInfo = newSemanticModelForInlinedDocument.GetSymbolInfo(innerInitializerInInlineNode, cancellationToken);

                    // Verification: The symbol info associated with any of the inlined expressions does not match the symbol info for original initializer expression prior to inline.
                    if (!SpeculationAnalyzer.SymbolInfosAreCompatible(originalInitializerSymbolInfo, newInitializerSymbolInfo, performEquivalenceCheck: true))
                    {
                        newInitializerSymbolInfo = newSemanticModelForInlinedDocument.GetSymbolInfo(inlinedNode, cancellationToken);
                        if (!SpeculationAnalyzer.SymbolInfosAreCompatible(originalInitializerSymbolInfo, newInitializerSymbolInfo, performEquivalenceCheck: true))
                        {
                            replacementNodesWithChangedSemantics ??= new Dictionary<SyntaxNode, SyntaxNode>();
                            replacementNodesWithChangedSemantics.Add(inlinedNode, originalNode);
                        }
                    }

                    // Verification: Do not inline a variable into the left side of a deconstruction-assignment
                    if (IsInDeconstructionAssignmentLeft(innerInitializerInInlineNode))
                    {
                        replacementNodesWithChangedSemantics ??= new Dictionary<SyntaxNode, SyntaxNode>();
                        replacementNodesWithChangedSemantics.Add(inlinedNode, originalNode);
                    }
                }
            }

            if (replacementNodesWithChangedSemantics == null)
            {
                // No conflicts.
                return inlinedDocument;
            }

            // Replace the conflicting inlined nodes with the original nodes annotated with conflict annotation.
            static SyntaxNode conflictAnnotationAdder(SyntaxNode oldNode, SyntaxNode newNode) =>
                    newNode.WithAdditionalAnnotations(ConflictAnnotation.Create(CSharpFeaturesResources.Conflict_s_detected));

            return await inlinedDocument.ReplaceNodesAsync(replacementNodesWithChangedSemantics.Keys, conflictAnnotationAdder, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsInDeconstructionAssignmentLeft(ExpressionSyntax node)
        {
            var parent = node.Parent;
            while (parent.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.CastExpression))
            {
                parent = parent.Parent;
            }

            while (parent.IsKind(SyntaxKind.Argument))
            {
                parent = parent.Parent;
                if (!parent.IsKind(SyntaxKind.TupleExpression))
                {
                    return false;
                }
                else if (parent.IsParentKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    return ((AssignmentExpressionSyntax)parent.Parent).Left == parent;
                }

                parent = parent.Parent;
            }

            return false;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}

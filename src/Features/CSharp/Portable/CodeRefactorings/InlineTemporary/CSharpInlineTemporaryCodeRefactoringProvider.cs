// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.InlineTemporary;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.InlineTemporary
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InlineTemporary), Shared]
    internal partial class CSharpInlineTemporaryCodeRefactoringProvider
        : AbstractInlineTemporaryCodeRefactoringProvider<IdentifierNameSyntax, VariableDeclaratorSyntax>
    {
        private static readonly SyntaxAnnotation DefinitionAnnotation = new();
        private static readonly SyntaxAnnotation ReferenceAnnotation = new();
        private static readonly SyntaxAnnotation InitializerAnnotation = new();
        private static readonly SyntaxAnnotation ExpressionToInlineAnnotation = new();
        private static readonly SyntaxAnnotation ExpressionAnnotation = new();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpInlineTemporaryCodeRefactoringProvider()
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
            if (variableDeclarator == null)
            {
                return;
            }

            if (variableDeclarator.Parent is not VariableDeclarationSyntax variableDeclaration ||
                variableDeclaration.Parent is not LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                return;
            }

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

            if (localDeclarationStatement.ContainsDiagnostics ||
                localDeclarationStatement.UsingKeyword != default)
            {
                return;
            }

            var references = await GetReferenceLocationsAsync(document, variableDeclarator, cancellationToken).ConfigureAwait(false);
            if (!references.Any())
                return;

            context.RegisterRefactoring(
                new MyCodeAction(c => InlineTemporaryAsync(document, variableDeclarator, c)),
                variableDeclarator.Span);
        }

        private static bool HasConflict(IdentifierNameSyntax identifier, VariableDeclaratorSyntax variableDeclarator)
        {
            // TODO: Check for more conflict types.
            if (identifier.SpanStart < variableDeclarator.SpanStart)
                return true;

            var identifierNode = identifier.WalkUpParentheses();
            if (IsInDeconstructionAssignmentLeft(identifierNode))
                return true;

            if (identifierNode.IsParentKind(SyntaxKind.Argument, out ArgumentSyntax argument))
            {
                if (argument.RefOrOutKeyword.Kind() != SyntaxKind.None)
                    return true;
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
            else if (identifierNode.Parent is AssignmentExpressionSyntax binaryExpression &&
                     binaryExpression.Left == identifierNode)
            {
                return true;
            }

            return false;
        }

        private static SyntaxAnnotation CreateConflictAnnotation()
            => ConflictAnnotation.Create(CSharpFeaturesResources.Conflict_s_detected);

        private static async Task<Document> InlineTemporaryAsync(Document document, VariableDeclaratorSyntax declarator, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            // Create the expression that we're actually going to inline.
            var expressionToInline = await CreateExpressionToInlineAsync(document, declarator, cancellationToken).ConfigureAwait(false);
            expressionToInline = expressionToInline.Parenthesize().WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation, ExpressionAnnotation);

            // Annotate the variable declarator so that we can get back to it later.
            document = await document.ReplaceNodeAsync(declarator, declarator.WithAdditionalAnnotations(DefinitionAnnotation), cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            declarator = await FindDeclaratorAsync(document, cancellationToken).ConfigureAwait(false);

            // Collect the identifier names for each reference.
            var allReferences = await GetReferenceLocationsAsync(document, declarator, cancellationToken).ConfigureAwait(false);

            // Add referenceAnnotations to identifier nodes being replaced.
            document = await document.ReplaceNodesAsync(
                allReferences,
                (o, n) => n.WithAdditionalAnnotations(ReferenceAnnotation),
                cancellationToken).ConfigureAwait(false);

            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            declarator = await FindDeclaratorAsync(document, cancellationToken).ConfigureAwait(false);

            allReferences = await FindReferenceAnnotatedNodesAsync(document, cancellationToken).ConfigureAwait(false);

            var conflictReferences = allReferences.Where(n => HasConflict(n, declarator)).ToSet();
            var nonConflictReferences = allReferences.Where(n => !conflictReferences.Contains(n)).ToSet();

            // Checks to see if inlining the temporary variable may change the code's meaning. This can only apply if the variable has two or more
            // references. We later use this heuristic to determine whether or not to display a warning message to the user.
            var mayContainSideEffects = allReferences.Count() > 1 &&
                MayContainSideEffects(declarator.Initializer.Value);

            var scope = GetScope(declarator);
            var newScope = ReferenceRewriter.Visit(scope, semanticModel, conflictReferences, nonConflictReferences, expressionToInline, cancellationToken);

            document = await document.ReplaceNodeAsync(scope, newScope, cancellationToken).ConfigureAwait(false);
            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            declarator = await FindDeclaratorAsync(document, cancellationToken).ConfigureAwait(false);
            newScope = GetScope(declarator);

            // Note that we only remove the local declaration if there weren't any conflicts,
            if (conflictReferences.Count == 0)
            {
                // No semantic conflicts, we can remove the definition.
                document = await document.ReplaceNodeAsync(
                    newScope, RemoveDeclaratorFromScope(declarator, newScope), cancellationToken).ConfigureAwait(false);
                semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            }

            // Finally, check all the places we inlined an expression and add some final warnings there if appropriate.
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var topmostParentingExpressions = root.GetAnnotatedNodes(ExpressionAnnotation)
                .OfType<ExpressionSyntax>()
                .Select(e => GetTopMostParentingExpression(e));

            // Make each topmost parenting statement or Equals Clause Expressions semantically explicit.
            document = await document.ReplaceNodesAsync(topmostParentingExpressions, (o, n) =>
            {
                // warn when inlining into a conditional expression, as the inlined expression will not be executed.
                if (semanticModel.GetSymbolInfo(o, cancellationToken).Symbol is IMethodSymbol { IsConditional: true })
                {
                    n = n.WithAdditionalAnnotations(
                        WarningAnnotation.Create(CSharpFeaturesResources.Warning_Inlining_temporary_into_conditional_method_call));
                }

                // If the refactoring may potentially change the code's semantics, display a warning message to the user.
                // on the first inlined location.
                if (mayContainSideEffects)
                {
                    n = n.WithAdditionalAnnotations(
                        WarningAnnotation.Create(CSharpFeaturesResources.Warning_Inlining_temporary_variable_may_change_code_meaning));
                    mayContainSideEffects = false;
                }

                return n;
            }, cancellationToken).ConfigureAwait(false);

            return document;
        }

        private static bool MayContainSideEffects(SyntaxNode expression)
        {
            // Checks to see if inlining the temporary variable may change the code's semantics. 
            // This is not meant to be an exhaustive check; it's more like a heuristic for obvious cases we know may cause side effects.

            var descendantNodesAndSelf = expression.DescendantNodesAndSelf();

            // Object creation:
            // e.g.:
            //     var [||]c = new C();
            //     c.P = 1;
            //     var x = c;
            // After refactoring:
            //     new C().P = 1;
            //     var x = new C();

            // Invocation:
            // e.g. - let method M return a new instance of an object containing property P:
            //     var [||]c = M();
            //     c.P = 0;
            //     var x = c;
            // After refactoring:
            //     M().P = 0;
            //     var x = M();
            if (descendantNodesAndSelf.Any(n => n.IsKind(SyntaxKind.ObjectCreationExpression, SyntaxKind.InvocationExpression)))
            {
                return true;
            }

            // Assume if we reach here that the refactoring won't cause side effects.
            return false;
        }

        private static async Task<VariableDeclaratorSyntax> FindDeclaratorAsync(Document document, CancellationToken cancellationToken)
            => await FindNodeWithAnnotationAsync<VariableDeclaratorSyntax>(document, DefinitionAnnotation, cancellationToken).ConfigureAwait(false);

        private static async Task<ExpressionSyntax> FindInitializerAsync(Document document, CancellationToken cancellationToken)
            => await FindNodeWithAnnotationAsync<ExpressionSyntax>(document, InitializerAnnotation, cancellationToken).ConfigureAwait(false);

        private static async Task<T> FindNodeWithAnnotationAsync<T>(Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
            where T : SyntaxNode
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root
                .GetAnnotatedNodesAndTokens(annotation)
                .Single()
                .AsNode() as T;
        }

        private static async Task<ImmutableArray<IdentifierNameSyntax>> FindReferenceAnnotatedNodesAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root.GetAnnotatedNodesAndTokens(ReferenceAnnotation).Select(n => (IdentifierNameSyntax)n.AsNode()).ToImmutableArray();
        }

        private static SyntaxNode GetScope(VariableDeclaratorSyntax variableDeclarator)
        {
            var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
            var localDeclaration = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
            var scope = localDeclaration.Parent;

            while (scope.IsKind(SyntaxKind.LabeledStatement))
                scope = scope.Parent;

            var parentExpressions = scope.AncestorsAndSelf().OfType<ExpressionSyntax>();
            scope = parentExpressions.LastOrDefault()?.Parent ?? scope;

            if (scope.IsKind(SyntaxKind.GlobalStatement))
                scope = scope.Parent;

            return scope;
        }

        private static VariableDeclaratorSyntax FindDeclarator(SyntaxNode node)
        {
            var annotatedNodesOrTokens = node.GetAnnotatedNodesAndTokens(DefinitionAnnotation).ToList();
            Debug.Assert(annotatedNodesOrTokens.Count == 1, "Only a single variable declarator should have been annotated.");

            return (VariableDeclaratorSyntax)annotatedNodesOrTokens.First().AsNode();
        }

        private static SyntaxNode RemoveDeclaratorFromVariableList(VariableDeclaratorSyntax variableDeclarator, VariableDeclarationSyntax variableDeclaration)
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

        private static SyntaxNode RemoveDeclaratorFromScope(VariableDeclaratorSyntax variableDeclarator, SyntaxNode scope)
        {
            var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;

            // If there is more than one variable declarator, remove this one from the variable declaration.
            if (variableDeclaration.Variables.Count > 1)
                return RemoveDeclaratorFromVariableList(variableDeclarator, variableDeclaration);

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
            if (newLocalDeclaration.IsParentKind(SyntaxKind.LabeledStatement, out LabeledStatementSyntax labeledStatement))
            {
                var newLabeledStatement = labeledStatement.ReplaceNode(newLocalDeclaration, SyntaxFactory.ParseStatement(""));
                return newScope.ReplaceNode(labeledStatement, newLabeledStatement);
            }

            // If the local is parented by a global statement, we need to remove the parent global statement.
            if (newLocalDeclaration.IsParentKind(SyntaxKind.GlobalStatement, out GlobalStatementSyntax globalStatement))
            {
                return newScope.RemoveNode(globalStatement, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return newScope.RemoveNode(newLocalDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
        }

        //private static ExpressionSyntax SkipRedundantExteriorParentheses(ExpressionSyntax expression)
        //{
        //    while (expression.IsKind(SyntaxKind.ParenthesizedExpression, out ParenthesizedExpressionSyntax parenthesized))
        //    {
        //        if (parenthesized.Expression == null ||
        //            parenthesized.Expression.IsMissing)
        //        {
        //            break;
        //        }

        //        if (parenthesized.Expression.IsKind(SyntaxKind.ParenthesizedExpression) ||
        //            parenthesized.Expression.IsKind(SyntaxKind.IdentifierName))
        //        {
        //            expression = parenthesized.Expression;
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }

        //    return expression;
        //}

        private static async Task<ExpressionSyntax> CreateExpressionToInlineAsync(
            Document document,
            VariableDeclaratorSyntax variableDeclarator,
            CancellationToken cancellationToken)
        {
            var isVar = ((VariableDeclarationSyntax)variableDeclarator.Parent).Type.IsVar;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var expression = variableDeclarator.Initializer.Value.WalkDownParentheses();

            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
            expression = InitializerRewriter.Visit(expression, localSymbol, semanticModel);

            if (expression is ImplicitObjectCreationExpressionSyntax implicitCreation)
            {
                // Consider: C c = new(); Console.WriteLine(c.ToString());
                // Inlining result should be: Console.WriteLine(new C().ToString()); instead of Console.WriteLine(new().ToString());
                // This condition converts implicit object creation expression to normal object creation expression.

                var type = localSymbol.Type.GenerateTypeSyntax();
                return SyntaxFactory.ObjectCreationExpression(implicitCreation.NewKeyword, type, implicitCreation.ArgumentList, implicitCreation.Initializer);
            }
            else if (expression.IsKind(SyntaxKind.ArrayInitializerExpression, out InitializerExpressionSyntax arrayInitializer))
            {
                // If this is an array initializer, we need to transform it into an array creation
                // expression for inlining.

                var arrayType = (ArrayTypeSyntax)localSymbol.Type.GenerateTypeSyntax();

                // Add any non-whitespace trailing trivia from the equals clause to the type.
                var equalsToken = variableDeclarator.Initializer.EqualsToken;
                if (equalsToken.HasTrailingTrivia)
                {
                    var trailingTrivia = equalsToken.TrailingTrivia.SkipInitialWhitespace();
                    if (trailingTrivia.Any())
                        arrayType = arrayType.WithTrailingTrivia(trailingTrivia);
                }

                return SyntaxFactory.ArrayCreationExpression(arrayType, arrayInitializer);
            }
            else if (isVar && expression is ObjectCreationExpressionSyntax or ArrayCreationExpressionSyntax or CastExpressionSyntax)
            {
                // if we have `var x = new Y();` there's no need to do any casting as the type is indicated
                // directly in the existing code.  The same holds for `new Y[]` or `(Y)...`
                return expression;
            }
            {
                if (localSymbol.Type.ContainsAnonymousType() || localSymbol.Type is IErrorTypeSymbol { Name: null or "" })
                    return expression;

                return expression.Cast(localSymbol.Type);
            }

#if false
            var expression = variableDeclarator.Initializer.Value.WalkDownParentheses();
            var semanticModel = await updatedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var localSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);
            var newExpression = InitializerRewriter.Visit(expression, localSymbol, semanticModel);

            // Consider: C c = new(); Console.WriteLine(c.ToString());
            // Inlining result should be: Console.WriteLine(new C().ToString()); instead of Console.WriteLine(new().ToString());
            // This condition converts implicit object creation expression to normal object creation expression.
            if (newExpression.IsKind(SyntaxKind.ImplicitObjectCreationExpression))
            {
                var implicitCreation = (ImplicitObjectCreationExpressionSyntax)newExpression;
                var type = localSymbol.Type.GenerateTypeSyntax();
                newExpression = SyntaxFactory.ObjectCreationExpression(implicitCreation.NewKeyword, type, implicitCreation.ArgumentList, implicitCreation.Initializer);
            }

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

            var explicitCastExpression = newExpression.CastIfPossible(localSymbol.Type, newVariableDeclarator.SpanStart, semanticModel, cancellationToken);
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
#endif
        }

        private static SyntaxNode GetTopMostParentingExpression(ExpressionSyntax expression)
            => expression.AncestorsAndSelf().OfType<ExpressionSyntax>().Last();

        //private static async Task<Document> DetectSemanticConflictsAsync(
        //    Document inlinedDocument,
        //    SemanticModel newSemanticModelForInlinedDocument,
        //    SemanticModel semanticModelBeforeInline,
        //    SymbolInfo originalInitializerSymbolInfo,
        //    CancellationToken cancellationToken)
        //{
        //    // In this method we detect if inlining the expression introduced the following semantic change:
        //    // The symbol info associated with any of the inlined expressions does not match the symbol info for original initializer expression prior to inline.

        //    // If any semantic changes were introduced by inlining, we update the document with conflict annotations.
        //    // Otherwise we return the given inlined document without any changes.

        //    var syntaxRootBeforeInline = await semanticModelBeforeInline.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        //    // Get all the identifier nodes which were replaced with inlined expression.
        //    var originalIdentifierNodes = FindReferenceAnnotatedNodes(syntaxRootBeforeInline);

        //    if (originalIdentifierNodes.IsEmpty())
        //    {
        //        // No conflicts
        //        return inlinedDocument;
        //    }

        //    // Get all the inlined expression nodes.
        //    var syntaxRootAfterInline = await inlinedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        //    var inlinedExprNodes = syntaxRootAfterInline.GetAnnotatedNodesAndTokens(ExpressionToInlineAnnotation);
        //    Debug.Assert(originalIdentifierNodes.Count() == inlinedExprNodes.Count());

        //    Dictionary<SyntaxNode, SyntaxNode> replacementNodesWithChangedSemantics = null;
        //    using (var originalNodesEnum = originalIdentifierNodes.GetEnumerator())
        //    {
        //        using var inlinedNodesOrTokensEnum = inlinedExprNodes.GetEnumerator();

        //        while (originalNodesEnum.MoveNext())
        //        {
        //            inlinedNodesOrTokensEnum.MoveNext();
        //            var originalNode = originalNodesEnum.Current;

        //            // expressionToInline is Parenthesized prior to replacement, so get the parenting parenthesized expression.
        //            var inlinedNode = (ExpressionSyntax)inlinedNodesOrTokensEnum.Current.Parent;
        //            Debug.Assert(inlinedNode.IsKind(SyntaxKind.ParenthesizedExpression));

        //            // inlinedNode is the expanded form of the actual initializer expression in the original document.
        //            // We have annotated the inner initializer with a special syntax annotation "InitializerAnnotation".
        //            // Get this annotated node and compute the symbol info for this node in the inlined document.
        //            var innerInitializerInInlineNodeOrToken = inlinedNode.GetAnnotatedNodesAndTokens(InitializerAnnotation).First();

        //            var innerInitializerInInlineNode = (ExpressionSyntax)(innerInitializerInInlineNodeOrToken.IsNode ?
        //                innerInitializerInInlineNodeOrToken.AsNode() :
        //                innerInitializerInInlineNodeOrToken.AsToken().Parent);
        //            var newInitializerSymbolInfo = newSemanticModelForInlinedDocument.GetSymbolInfo(innerInitializerInInlineNode, cancellationToken);

        //            // Verification: The symbol info associated with any of the inlined expressions does not match the symbol info for original initializer expression prior to inline.
        //            if (!SpeculationAnalyzer.SymbolInfosAreCompatible(originalInitializerSymbolInfo, newInitializerSymbolInfo, performEquivalenceCheck: true))
        //            {
        //                newInitializerSymbolInfo = newSemanticModelForInlinedDocument.GetSymbolInfo(inlinedNode, cancellationToken);
        //                if (!SpeculationAnalyzer.SymbolInfosAreCompatible(originalInitializerSymbolInfo, newInitializerSymbolInfo, performEquivalenceCheck: true))
        //                {
        //                    replacementNodesWithChangedSemantics ??= new Dictionary<SyntaxNode, SyntaxNode>();
        //                    replacementNodesWithChangedSemantics.Add(inlinedNode, originalNode);
        //                }
        //            }

        //            // Verification: Do not inline a variable into the left side of a deconstruction-assignment
        //            if (IsInDeconstructionAssignmentLeft(innerInitializerInInlineNode))
        //            {
        //                replacementNodesWithChangedSemantics ??= new Dictionary<SyntaxNode, SyntaxNode>();
        //                replacementNodesWithChangedSemantics.Add(inlinedNode, originalNode);
        //            }
        //        }
        //    }

        //    if (replacementNodesWithChangedSemantics == null)
        //    {
        //        // No conflicts.
        //        return inlinedDocument;
        //    }

        //    // Replace the conflicting inlined nodes with the original nodes annotated with conflict annotation.
        //    static SyntaxNode conflictAnnotationAdder(SyntaxNode oldNode, SyntaxNode newNode) =>
        //            newNode.WithAdditionalAnnotations(ConflictAnnotation.Create(CSharpFeaturesResources.Conflict_s_detected));

        //    return await inlinedDocument.ReplaceNodesAsync(replacementNodesWithChangedSemantics.Keys, conflictAnnotationAdder, cancellationToken).ConfigureAwait(false);
        //}

        private static bool IsInDeconstructionAssignmentLeft(ExpressionSyntax node)
        {
            var parent = node.Parent;
            while (parent.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.CastExpression))
                parent = parent.Parent;

            while (parent.IsKind(SyntaxKind.Argument))
            {
                parent = parent.Parent;
                if (!parent.IsKind(SyntaxKind.TupleExpression))
                {
                    return false;
                }
                else if (parent.IsParentKind(SyntaxKind.SimpleAssignmentExpression, out AssignmentExpressionSyntax assignment))
                {
                    return assignment.Left == parent;
                }

                parent = parent.Parent;
            }

            return false;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Inline_temporary_variable, createChangedDocument, nameof(CSharpFeaturesResources.Inline_temporary_variable))
            {
            }
        }
    }
}

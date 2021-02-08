// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

#nullable disable

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceParameterService<TService, TExpressionSyntax> : CodeRefactoringProvider
        where TService : AbstractIntroduceParameterService<TService, TExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
    {
        public TExpressionSyntax Expression { get; private set; }
        protected abstract Task<Document> IntroduceParameterAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken);
        protected abstract IEnumerable<SyntaxNode> GetContainingExecutableBlocks(TExpressionSyntax expression);
        protected virtual bool BlockOverlapsHiddenPosition(SyntaxNode block, CancellationToken cancellationToken)
            => block.OverlapsHiddenPosition(cancellationToken);
        protected abstract bool CanReplace(TExpressionSyntax expression);
        protected abstract bool IsExpressionInStaticLocalFunction(TExpressionSyntax expression);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var action = await IntroduceParameterAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (action != null)
            {
                context.RegisterRefactoring(action, textSpan);
            }
        }

        public async Task<CodeAction> IntroduceParameterAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            Expression = await document.TryGetRelevantNodeAsync<TExpressionSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
            if (Expression == null || CodeRefactoringHelpers.IsNodeUnderselected(Expression, textSpan))
            {
                return null;
            }

            var expressionType = semanticDocument.SemanticModel.GetTypeInfo(Expression, cancellationToken).Type;
            if (expressionType is IErrorTypeSymbol)
            {
                return null;
            }

            var containingType = Expression.AncestorsAndSelf()
                .Select(n => semanticDocument.SemanticModel.GetDeclaredSymbol(n, cancellationToken))
                .OfType<INamedTypeSymbol>()
                .FirstOrDefault();

            containingType ??= semanticDocument.SemanticModel.Compilation.ScriptClass;

            if (containingType == null || containingType.TypeKind == TypeKind.Interface)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (Expression != null)
            {
                var (title, actions) = AddActions(semanticDocument, cancellationToken);

                if (actions.Length > 0)
                {
                    return new CodeActionWithNestedActions(title, actions, isInlinable: true);
                }
            }
            return null;

        }

        private (string title, ImmutableArray<CodeAction> actions) AddActions(SemanticDocument semanticDocument, CancellationToken cancellationToken)
        {
            var actionsBuilder = new ArrayBuilder<CodeAction>();
            var enclosingBlocks = GetContainingExecutableBlocks(Expression);
            if (enclosingBlocks.Any())
            {
                // If we're inside a block, then don't even try the other options (like field,
                // constructor initializer, etc.).  This is desirable behavior.  If we're in a 
                // block in a field, then we're in a lambda, and we want to offer to generate
                // a local, and not a field.
                if (IsInBlockContext(semanticDocument, cancellationToken))
                {
                    var block = enclosingBlocks.FirstOrDefault();

                    if (!BlockOverlapsHiddenPosition(block, cancellationToken))
                    {
                        actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, Expression, false));

                        if (enclosingBlocks.All(b => !BlockOverlapsHiddenPosition(b, cancellationToken)))
                        {
                            actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, Expression, true));
                        }
                    }
                }
            }
            return (FeaturesResources.Introduce_parameter, actionsBuilder.ToImmutable());
        }

        protected static ITypeSymbol GetTypeSymbol(
            SemanticDocument document,
            TExpressionSyntax expression,
            CancellationToken cancellationToken,
            bool objectAsDefault = true)
        {
            var semanticModel = document.SemanticModel;
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

            if (typeInfo.Type?.SpecialType == SpecialType.System_String &&
                typeInfo.ConvertedType?.IsFormattableStringOrIFormattable() == true)
            {
                return typeInfo.ConvertedType;
            }

            if (typeInfo.Type != null)
            {
                return typeInfo.Type;
            }

            if (typeInfo.ConvertedType != null)
            {
                return typeInfo.ConvertedType;
            }

            if (objectAsDefault)
            {
                return semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }

            return null;
        }

        private bool IsInBlockContext(SemanticDocument semanticDocument,
                CancellationToken cancellationToken)
        {
            /* if (!IsInTypeDeclarationOrValidCompilationUnit())
             {
                 return false;
             }*/

            // If refer to a query property, then we use the query context instead.
            var bindingMap = GetSemanticMap(semanticDocument, cancellationToken);
            if (bindingMap.AllReferencedSymbols.Any(s => s is IRangeVariableSymbol))
            {
                return false;
            }

            var type = GetTypeSymbol(semanticDocument, Expression, cancellationToken, objectAsDefault: false);
            if (type == null || type.SpecialType == SpecialType.System_Void)
            {
                return false;
            }

            return true;
        }

        public SemanticMap GetSemanticMap(SemanticDocument semanticDocument, CancellationToken cancellationToken)
        {
            var semanticMap = semanticDocument.SemanticModel.GetSemanticMap(Expression, cancellationToken);
            return semanticMap;
        }

        protected ISet<TExpressionSyntax> FindMatches(
           SemanticDocument originalDocument,
           TExpressionSyntax expressionInOriginal,
           SemanticDocument currentDocument,
           SyntaxNode withinNodeInCurrent,
           bool allOccurrences,
           CancellationToken cancellationToken)
        {
            var syntaxFacts = currentDocument.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var originalSemanticModel = originalDocument.SemanticModel;
            var currentSemanticModel = currentDocument.SemanticModel;

            var result = new HashSet<TExpressionSyntax>();
            var matches = from nodeInCurrent in withinNodeInCurrent.DescendantNodesAndSelf().OfType<TExpressionSyntax>()
                          where NodeMatchesExpression(originalSemanticModel, currentSemanticModel, expressionInOriginal, nodeInCurrent, allOccurrences, cancellationToken)
                          select nodeInCurrent;
            result.AddRange(matches.OfType<TExpressionSyntax>());

            return result;
        }

        private bool NodeMatchesExpression(
            SemanticModel originalSemanticModel,
            SemanticModel currentSemanticModel,
            TExpressionSyntax expressionInOriginal,
            TExpressionSyntax nodeInCurrent,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (nodeInCurrent == expressionInOriginal)
            {
                return true;
            }

            if (allOccurrences && CanReplace(nodeInCurrent))
            {
                // Original expression and current node being semantically equivalent isn't enough when the original expression 
                // is a member access via instance reference (either implicit or explicit), the check only ensures that the expression
                // and current node are both backed by the same member symbol. So in this case, in addition to SemanticEquivalence check, 
                // we also check if expression and current node are both instance member access.
                //
                // For example, even though the first `c` binds to a field and we are introducing a local for it,
                // we don't want other references to that field to be replaced as well (i.e. the second `c` in the expression).
                //
                //  class C
                //  {
                //      C c;
                //      void Test()
                //      {
                //          var x = [|c|].c;
                //      }
                //  }

                if (SemanticEquivalence.AreEquivalent(
                    originalSemanticModel, currentSemanticModel, expressionInOriginal, nodeInCurrent))
                {
                    var originalOperation = originalSemanticModel.GetOperation(expressionInOriginal, cancellationToken);
                    if (IsInstanceMemberReference(originalOperation))
                    {
                        var currentOperation = currentSemanticModel.GetOperation(nodeInCurrent, cancellationToken);
                        return IsInstanceMemberReference(currentOperation);
                    }

                    // If the original expression is within a static local function, further checks are unnecessary since our scope has already been narrowed down to within the local function.
                    // If the original expression is not within a static local function, we need to further check whether the expression we're comparing against is within a static local
                    // function. If so, the expression is not a valid match since we cannot refer to instance variables from within static local functions.
                    if (!IsExpressionInStaticLocalFunction(expressionInOriginal))
                    {
                        return !IsExpressionInStaticLocalFunction(nodeInCurrent);
                    }

                    return true;
                }
            }

            return false;
            static bool IsInstanceMemberReference(IOperation operation)
                => operation is IMemberReferenceOperation memberReferenceOperation &&
                    memberReferenceOperation.Instance?.Kind == OperationKind.InstanceReference;
        }
        protected static async Task<(SemanticDocument newSemanticDocument, ISet<TExpressionSyntax> newMatches)> ComplexifyParentingStatementsAsync(
            SemanticDocument semanticDocument,
            ISet<TExpressionSyntax> matches,
            CancellationToken cancellationToken)
        {
            // First, track the matches so that we can get back to them later.
            var newRoot = semanticDocument.Root.TrackNodes(matches);
            var newDocument = semanticDocument.Document.WithSyntaxRoot(newRoot);
            var newSemanticDocument = await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
            var newMatches = newSemanticDocument.Root.GetCurrentNodes(matches.AsEnumerable()).ToSet();

            // Next, expand the topmost parenting expression of each match, being careful
            // not to expand the matches themselves.
            var topMostExpressions = newMatches
                .Select(m => m.AncestorsAndSelf().OfType<TExpressionSyntax>().Last())
                .Distinct();

            newRoot = await newSemanticDocument.Root
                .ReplaceNodesAsync(
                    topMostExpressions,
                    computeReplacementAsync: async (oldNode, newNode, ct) =>
                    {
                        return await Simplifier
                            .ExpandAsync(
                                oldNode,
                                newSemanticDocument.Document,
                                expandInsideNode: node =>
                                {
                                    return !(node is TExpressionSyntax expression)
                                        || !newMatches.Contains(expression);
                                },
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            newDocument = newSemanticDocument.Document.WithSyntaxRoot(newRoot);
            newSemanticDocument = await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
            newMatches = newSemanticDocument.Root.GetCurrentNodes(matches.AsEnumerable()).ToSet();

            return (newSemanticDocument, newMatches);
        }

        protected TNode Rewrite<TNode>(
            SemanticDocument originalDocument,
            TExpressionSyntax expressionInOriginal,
            TExpressionSyntax variableName,
            SemanticDocument currentDocument,
            TNode withinNodeInCurrent,
            bool allOccurrences,
            CancellationToken cancellationToken)
            where TNode : SyntaxNode
        {
            var generator = SyntaxGenerator.GetGenerator(originalDocument.Document);
            var matches = FindMatches(originalDocument, expressionInOriginal, currentDocument, withinNodeInCurrent, allOccurrences, cancellationToken);

            // Parenthesize the variable, and go and replace anything we find with it.
            // NOTE: we do not want elastic trivia as we want to just replace the existing code 
            // as is, while preserving the trivia there.  We do not want to update it.
            var replacement = generator.AddParentheses(variableName, includeElasticTrivia: false)
                                         .WithAdditionalAnnotations(Formatter.Annotation);

            return RewriteCore(withinNodeInCurrent, replacement, matches);
        }

        protected abstract TNode RewriteCore<TNode>(
            TNode node,
            SyntaxNode replacementNode,
            ISet<TExpressionSyntax> matches)
            where TNode : SyntaxNode;

        private class IntroduceParameterCodeAction : AbstractIntroduceParameterCodeAction
        {
            internal IntroduceParameterCodeAction(
                SemanticDocument document,
                TService service,
                TExpressionSyntax expression,
                bool allOccurrences)
                : base(document, service, expression, allOccurrences)
            { }
        }
    }
}

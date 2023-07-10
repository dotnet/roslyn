// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax> : IIntroduceVariableService
        where TService : AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax, TNameSyntax>
        where TExpressionSyntax : SyntaxNode
        where TTypeSyntax : TExpressionSyntax
        where TTypeDeclarationSyntax : SyntaxNode
        where TQueryExpressionSyntax : TExpressionSyntax
        where TNameSyntax : TTypeSyntax
    {
        protected abstract bool IsInNonFirstQueryClause(TExpressionSyntax expression);
        protected abstract bool IsInFieldInitializer(TExpressionSyntax expression);
        protected abstract bool IsInParameterInitializer(TExpressionSyntax expression);
        protected abstract bool IsInConstructorInitializer(TExpressionSyntax expression);
        protected abstract bool IsInAttributeArgumentInitializer(TExpressionSyntax expression);
        protected abstract bool IsInAutoPropertyInitializer(TExpressionSyntax expression);
        protected abstract bool IsInExpressionBodiedMember(TExpressionSyntax expression);

        protected abstract IEnumerable<SyntaxNode> GetContainingExecutableBlocks(TExpressionSyntax expression);
        protected abstract IList<bool> GetInsertionIndices(TTypeDeclarationSyntax destination, CancellationToken cancellationToken);

        protected abstract bool CanIntroduceVariableFor(TExpressionSyntax expression);
        protected abstract bool CanReplace(TExpressionSyntax expression);

        protected abstract bool IsExpressionInStaticLocalFunction(TExpressionSyntax expression);

        protected abstract Task<Document> IntroduceQueryLocalAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken);
        protected abstract Task<Document> IntroduceLocalAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool isConstant, CancellationToken cancellationToken);
        protected abstract Task<Document> IntroduceFieldAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool isConstant, CancellationToken cancellationToken);

        protected abstract int DetermineFieldInsertPosition(TTypeDeclarationSyntax oldDeclaration, TTypeDeclarationSyntax newDeclaration);
        protected abstract int DetermineConstantInsertPosition(TTypeDeclarationSyntax oldDeclaration, TTypeDeclarationSyntax newDeclaration);

        protected virtual bool BlockOverlapsHiddenPosition(SyntaxNode block, CancellationToken cancellationToken)
            => block.OverlapsHiddenPosition(cancellationToken);

        public async Task<CodeAction> IntroduceVariableAsync(
            Document document,
            TextSpan textSpan,
            CodeCleanupOptions options,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_IntroduceVariable, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateAsync((TService)this, semanticDocument, options, textSpan, cancellationToken).ConfigureAwait(false);
                if (state != null)
                {
                    var (title, actions) = CreateActions(state, cancellationToken);
                    if (actions.Length > 0)
                    {
                        // We may end up creating a lot of viable code actions for the selected
                        // piece of code.  Create a top level code action so that we don't overwhelm
                        // the light bulb if there are a lot of other options in the list.  Set 
                        // the code action as 'inlinable' so that if the lightbulb is not cluttered
                        // then the nested items can just be lifted into it, giving the user fast
                        // access to them.
                        return CodeAction.Create(title, actions, isInlinable: true);
                    }
                }

                return null;
            }
        }

        private (string title, ImmutableArray<CodeAction>) CreateActions(State state, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
            var title = AddActionsAndGetTitle(state, actions, cancellationToken);

            return (title, actions.ToImmutable());
        }

        private string AddActionsAndGetTitle(State state, ArrayBuilder<CodeAction> actions, CancellationToken cancellationToken)
        {
            if (state.InQueryContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: false, isLocal: false, isQueryLocal: true));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: false, isLocal: false, isQueryLocal: true));

                return FeaturesResources.Introduce_query_variable;
            }
            else if (state.InParameterContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: true, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: true, isLocal: false, isQueryLocal: false));

                return FeaturesResources.Introduce_constant;
            }
            else if (state.InFieldContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));

                return GetConstantOrFieldResource(state.IsConstant);
            }
            else if (state.InConstructorInitializerContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));

                return GetConstantOrFieldResource(state.IsConstant);
            }
            else if (state.InAutoPropertyInitializerContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));

                return GetConstantOrFieldResource(state.IsConstant);
            }
            else if (state.InAttributeContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: true, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: true, isLocal: false, isQueryLocal: false));

                return FeaturesResources.Introduce_constant;
            }
            else if (state.InBlockContext)
            {
                CreateConstantFieldActions(state, actions, cancellationToken);

                var blocks = GetContainingExecutableBlocks(state.Expression);
                var block = blocks.FirstOrDefault();

                if (!BlockOverlapsHiddenPosition(block, cancellationToken))
                {
                    actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));

                    if (blocks.All(b => !BlockOverlapsHiddenPosition(b, cancellationToken)))
                    {
                        actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));
                    }
                }

                return GetConstantOrLocalResource(state.IsConstant);
            }
            else if (state.InExpressionBodiedMemberContext)
            {
                CreateConstantFieldActions(state, actions, cancellationToken);
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));

                return GetConstantOrLocalResource(state.IsConstant);
            }
            else
            {
                return null;
            }
        }

        private static string GetConstantOrFieldResource(bool isConstant)
            => isConstant ? FeaturesResources.Introduce_constant : FeaturesResources.Introduce_field;

        private static string GetConstantOrLocalResource(bool isConstant)
            => isConstant ? FeaturesResources.Introduce_constant : FeaturesResources.Introduce_local;

        private void CreateConstantFieldActions(State state, ArrayBuilder<CodeAction> actions, CancellationToken cancellationToken)
        {
            if (state.IsConstant &&
                !state.GetSemanticMap(cancellationToken).AllReferencedSymbols.OfType<ILocalSymbol>().Any() &&
                !state.GetSemanticMap(cancellationToken).AllReferencedSymbols.OfType<IParameterSymbol>().Any())
            {
                // If something is a constant, and it doesn't access any other locals constants,
                // then we prefer to offer to generate a constant field instead of a constant
                // local.
                if (CanGenerateIntoContainer(state, cancellationToken))
                {
                    actions.Add(CreateAction(state, allOccurrences: false, isConstant: true, isLocal: false, isQueryLocal: false));
                    actions.Add(CreateAction(state, allOccurrences: true, isConstant: true, isLocal: false, isQueryLocal: false));
                }
            }
        }

        protected int GetFieldInsertionIndex(
            bool isConstant, TTypeDeclarationSyntax oldType, TTypeDeclarationSyntax newType, CancellationToken cancellationToken)
        {
            var preferredInsertionIndex = isConstant
                ? DetermineConstantInsertPosition(oldType, newType)
                : DetermineFieldInsertPosition(oldType, newType);

            var legalInsertionIndices = GetInsertionIndices(oldType, cancellationToken);
            if (legalInsertionIndices[preferredInsertionIndex])
            {
                return preferredInsertionIndex;
            }

            // location we wanted to insert into isn't legal (i.e. it's hidden).  Try to find a
            // non-hidden location.
            var legalIndex = legalInsertionIndices.IndexOf(true);
            if (legalIndex >= 0)
            {
                return legalIndex;
            }

            // Couldn't find a viable non-hidden position.  Fall back to the computed position we
            // wanted originally.
            return preferredInsertionIndex;
        }

        private bool CanGenerateIntoContainer(State state, CancellationToken cancellationToken)
        {
            var destination = state.Expression.GetAncestor<TTypeDeclarationSyntax>() ?? state.Document.Root;
            if (!destination.OverlapsHiddenPosition(cancellationToken))
            {
                return true;
            }

            if (destination is TTypeDeclarationSyntax typeDecl)
            {
                var insertionIndices = GetInsertionIndices(typeDecl, cancellationToken);
                // We can generate into a containing type as long as there is at least one non-hidden location in it.
                if (insertionIndices.Contains(true))
                {
                    return true;
                }
            }

            return false;
        }

        private CodeAction CreateAction(State state, bool allOccurrences, bool isConstant, bool isLocal, bool isQueryLocal)
        {
            if (allOccurrences)
            {
                return new IntroduceVariableAllOccurrenceCodeAction((TService)this, state.Document, state.Options, state.Expression, allOccurrences, isConstant, isLocal, isQueryLocal);
            }

            return new IntroduceVariableCodeAction((TService)this, state.Document, state.Options, state.Expression, allOccurrences, isConstant, isLocal, isQueryLocal);
        }

        protected static SyntaxToken GenerateUniqueFieldName(
            SemanticDocument semanticDocument,
            TExpressionSyntax expression,
            bool isConstant,
            CancellationToken cancellationToken)
        {
            var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();

            var semanticModel = semanticDocument.SemanticModel;
            var baseName = semanticFacts.GenerateNameForExpression(
                semanticModel, expression, isConstant, cancellationToken);

            // A field can't conflict with any existing member names.
            var declaringType = semanticModel.GetEnclosingNamedType(expression.SpanStart, cancellationToken);
            var reservedNames = declaringType.GetMembers().Select(m => m.Name);

            return semanticFacts.GenerateUniqueName(baseName, reservedNames);
        }

        protected static SyntaxToken GenerateUniqueLocalName(
            SemanticDocument semanticDocument,
            TExpressionSyntax expression,
            bool isConstant,
            SyntaxNode containerOpt,
            CancellationToken cancellationToken)
        {
            var semanticModel = semanticDocument.SemanticModel;

            var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
            var baseName = semanticFacts.GenerateNameForExpression(
                semanticModel, expression, capitalize: isConstant, cancellationToken: cancellationToken);

            return semanticFacts.GenerateUniqueLocalName(
                semanticModel, expression, containerOpt, baseName, cancellationToken);
        }

        protected ISet<TExpressionSyntax> FindMatches(
            SemanticDocument originalDocument,
            TExpressionSyntax expressionInOriginal,
            SemanticDocument currentDocument,
            SyntaxNode withinNodeInCurrent,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = currentDocument.Project.Services.GetService<ISyntaxFactsService>();
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

        protected static IEnumerable<IParameterSymbol> GetAnonymousMethodParameters(
            SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var semanticModel = document.SemanticModel;
            var semanticMap = semanticModel.GetSemanticMap(expression, cancellationToken);

            var anonymousMethodParameters = semanticMap.AllReferencedSymbols
                                                       .OfType<IParameterSymbol>()
                                                       .Where(p => p.ContainingSymbol.IsAnonymousFunction());
            return anonymousMethodParameters;
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
                                    return node is not TExpressionSyntax expression
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
    }
}

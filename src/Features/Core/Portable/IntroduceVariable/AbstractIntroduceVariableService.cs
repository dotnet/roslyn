// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
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

        protected abstract Task<Document> IntroduceQueryLocalAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken);
        protected abstract Task<Document> IntroduceLocalAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool isConstant, CancellationToken cancellationToken);
        protected abstract Task<Tuple<Document, SyntaxNode, int>> IntroduceFieldAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool isConstant, CancellationToken cancellationToken);

        protected virtual bool BlockOverlapsHiddenPosition(SyntaxNode block, CancellationToken cancellationToken)
        {
            return block.OverlapsHiddenPosition(cancellationToken);
        }

        public async Task<CodeAction> IntroduceVariableAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_IntroduceVariable, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
                if (state != null)
                {
                    var (title, actions) = await CreateActionsAsync(state, cancellationToken).ConfigureAwait(false);
                    if (actions.Length > 0)
                    {
                        // We may end up creating a lot of viable code actions for the selected
                        // piece of code.  Create a top level code action so that we don't overwhelm
                        // the light bulb if there are a lot of other options in the list.  Set 
                        // the code action as 'inlinable' so that if the lightbulb is not cluttered
                        // then the nested items can just be lifted into it, giving the user fast
                        // access to them.
                        return new CodeActionWithNestedActions(title, actions, isInlinable: true);
                    }
                }

                return default;
            }
        }

        private async Task<(string title, ImmutableArray<CodeAction>)> CreateActionsAsync(State state, CancellationToken cancellationToken)
        {
            var actions = ArrayBuilder<CodeAction>.GetInstance();
            var title = await AddActionsAndGetTitleAsync(state, actions, cancellationToken).ConfigureAwait(false);

            return (title, actions.ToImmutableAndFree());
        }

        private async Task<string> AddActionsAndGetTitleAsync(State state, ArrayBuilder<CodeAction> actions, CancellationToken cancellationToken)
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
                await CreateConstantFieldActionsAsync(state, actions, cancellationToken).ConfigureAwait(false);

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
                await CreateConstantFieldActionsAsync(state, actions, cancellationToken).ConfigureAwait(false);
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

        private async Task CreateConstantFieldActionsAsync(State state, ArrayBuilder<CodeAction> actions, CancellationToken cancellationToken)
        {
            if (state.IsConstant &&
                !state.GetSemanticMap(cancellationToken).AllReferencedSymbols.OfType<ILocalSymbol>().Any() &&
                !state.GetSemanticMap(cancellationToken).AllReferencedSymbols.OfType<IParameterSymbol>().Any())
            {
                // If something is a constant, and it doesn't access any other locals constants,
                // then we prefer to offer to generate a constant field instead of a constant
                // local.
                var action1 = CreateAction(state, allOccurrences: false, isConstant: true, isLocal: false, isQueryLocal: false);
                if (await CanGenerateIntoContainerAsync(state, action1, cancellationToken).ConfigureAwait(false))
                {
                    actions.Add(action1);
                }

                var action2 = CreateAction(state, allOccurrences: true, isConstant: true, isLocal: false, isQueryLocal: false);
                if (await CanGenerateIntoContainerAsync(state, action2, cancellationToken).ConfigureAwait(false))
                {
                    actions.Add(action2);
                }
            }
        }

        private async Task<bool> CanGenerateIntoContainerAsync(State state, CodeAction action, CancellationToken cancellationToken)
        {
            var result = await IntroduceFieldAsync(
                state.Document, state.Expression,
                allOccurrences: false, isConstant: state.IsConstant, cancellationToken: cancellationToken).ConfigureAwait(false);

            var destination = result.Item2;
            var insertionIndex = result.Item3;

            if (!destination.OverlapsHiddenPosition(cancellationToken))
            {
                return true;
            }

            if (destination is TTypeDeclarationSyntax typeDecl)
            {
                var insertionIndices = GetInsertionIndices(typeDecl, cancellationToken);
                if (insertionIndices != null &&
                    insertionIndices.Count > insertionIndex &&
                    insertionIndices[insertionIndex])
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
                return new IntroduceVariableAllOccurrenceCodeAction((TService)this, state.Document, state.Expression, allOccurrences, isConstant, isLocal, isQueryLocal);
            }

            return new IntroduceVariableCodeAction((TService)this, state.Document, state.Expression, allOccurrences, isConstant, isLocal, isQueryLocal);
        }

        protected static SyntaxToken GenerateUniqueFieldName(
            SemanticDocument semanticDocument,
            TExpressionSyntax expression,
            bool isConstant,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();

            var semanticModel = semanticDocument.SemanticModel;
            var baseName = semanticFacts.GenerateNameForExpression(
                semanticModel, expression, isConstant, cancellationToken);

            // A field can't conflict with any existing member names.
            var declaringType = semanticModel.GetEnclosingNamedType(expression.SpanStart, cancellationToken);
            var reservedNames = declaringType.GetMembers().Select(m => m.Name);

            return syntaxFacts.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(baseName, reservedNames, syntaxFacts.IsCaseSensitive));
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
                // we don't want other refrences to that field to be replaced as well (i.e. the second `c` in the expression).
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
            var syntaxFacts = originalDocument.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var matches = FindMatches(originalDocument, expressionInOriginal, currentDocument, withinNodeInCurrent, allOccurrences, cancellationToken);

            // Parenthesize the variable, and go and replace anything we find with it.
            // NOTE: we do not want elastic trivia as we want to just replace the existing code 
            // as is, while preserving the trivia there.  We do not want to update it.
            var replacement = syntaxFacts.Parenthesize(variableName, includeElasticTrivia: false)
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
                typeInfo.ConvertedType?.IsFormattableString() == true)
            {
                return typeInfo.GetConvertedTypeWithFlowNullability();
            }

            if (typeInfo.Type != null)
            {
                return typeInfo.GetTypeWithFlowNullability();
            }

            if (typeInfo.ConvertedType != null)
            {
                return typeInfo.GetConvertedTypeWithFlowNullability();
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

        protected static async Task<(SemanticDocument newSemanticDocument, ISet<TExpressionSyntax> newMatches)> ComplexifyParentingStatements(
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
    }
}

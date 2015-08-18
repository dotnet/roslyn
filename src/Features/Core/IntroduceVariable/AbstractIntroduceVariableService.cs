// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax> : IIntroduceVariableService
        where TService : AbstractIntroduceVariableService<TService, TExpressionSyntax, TTypeSyntax, TTypeDeclarationSyntax, TQueryExpressionSyntax>
        where TExpressionSyntax : SyntaxNode
        where TTypeSyntax : TExpressionSyntax
        where TTypeDeclarationSyntax : SyntaxNode
        where TQueryExpressionSyntax : TExpressionSyntax
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

        public async Task<IIntroduceVariableResult> IntroduceVariableAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_IntroduceVariable, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
                if (state == null)
                {
                    return IntroduceVariableResult.Failure;
                }

                var actions = await CreateActionsAsync(state, cancellationToken).ConfigureAwait(false);
                if (actions.Count == 0)
                {
                    return IntroduceVariableResult.Failure;
                }

                return new IntroduceVariableResult(new CodeRefactoring(null, actions));
            }
        }

        private async Task<List<CodeAction>> CreateActionsAsync(State state, CancellationToken cancellationToken)
        {
            var actions = new List<CodeAction>();

            if (state.InQueryContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: false, isLocal: false, isQueryLocal: true));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: false, isLocal: false, isQueryLocal: true));
            }
            else if (state.InParameterContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: true, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: true, isLocal: false, isQueryLocal: false));
            }
            else if (state.InFieldContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
            }
            else if (state.InConstructorInitializerContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
            }
            else if (state.InAutoPropertyInitializerContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: false, isQueryLocal: false));
            }
            else if (state.InAttributeContext)
            {
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: true, isLocal: false, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: true, isLocal: false, isQueryLocal: false));
            }
            else if (state.InBlockContext)
            {
                await CreateConstantFieldActionsAsync(state, actions, cancellationToken).ConfigureAwait(false);

                var blocks = this.GetContainingExecutableBlocks(state.Expression);
                var block = blocks.FirstOrDefault();

                if (!BlockOverlapsHiddenPosition(block, cancellationToken))
                {
                    actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));

                    if (blocks.All(b => !BlockOverlapsHiddenPosition(b, cancellationToken)))
                    {
                        actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));
                    }
                }
            }
            else if (state.InExpressionBodiedMemberContext)
            {
                await CreateConstantFieldActionsAsync(state, actions, cancellationToken).ConfigureAwait(false);
                actions.Add(CreateAction(state, allOccurrences: false, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));
                actions.Add(CreateAction(state, allOccurrences: true, isConstant: state.IsConstant, isLocal: true, isQueryLocal: false));
            }

            return actions;
        }

        private async Task CreateConstantFieldActionsAsync(State state, List<CodeAction> actions, CancellationToken cancellationToken)
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
            var result = await this.IntroduceFieldAsync(
                state.Document, state.Expression,
                allOccurrences: false, isConstant: state.IsConstant, cancellationToken: cancellationToken).ConfigureAwait(false);

            SyntaxNode destination = result.Item2;
            int insertionIndex = result.Item3;

            if (!destination.OverlapsHiddenPosition(cancellationToken))
            {
                return true;
            }

            if (destination is TTypeDeclarationSyntax)
            {
                var insertionIndices = this.GetInsertionIndices((TTypeDeclarationSyntax)destination, cancellationToken);
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
            SemanticDocument document,
            TExpressionSyntax expression,
            bool isConstant,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var semanticFacts = document.Project.LanguageServices.GetService<ISemanticFactsService>();

            var semanticModel = document.SemanticModel;
            var baseName = semanticFacts.GenerateNameForExpression(semanticModel, expression, isConstant);

            // A field can't conflict with any existing member names.
            var declaringType = semanticModel.GetEnclosingNamedType(expression.SpanStart, cancellationToken);
            var reservedNames = declaringType.GetMembers().Select(m => m.Name);

            return syntaxFacts.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(baseName, reservedNames, syntaxFacts.IsCaseSensitive));
        }

        protected static SyntaxToken GenerateUniqueLocalName(
            SemanticDocument document,
            TExpressionSyntax expression,
            bool isConstant,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var semanticFacts = document.Project.LanguageServices.GetService<ISemanticFactsService>();

            var semanticModel = document.SemanticModel;
            var baseName = semanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize: isConstant);
            var reservedNames = semanticModel.LookupSymbols(expression.SpanStart).Select(s => s.Name);

            return syntaxFacts.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(baseName, reservedNames, syntaxFacts.IsCaseSensitive));
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
                          where NodeMatchesExpression(originalSemanticModel, currentSemanticModel, syntaxFacts, expressionInOriginal, nodeInCurrent, allOccurrences, cancellationToken)
                          select nodeInCurrent;
            result.AddRange(matches.OfType<TExpressionSyntax>());

            return result;
        }

        private bool NodeMatchesExpression(
            SemanticModel originalSemanticModel,
            SemanticModel currentSemanticModel,
            ISyntaxFactsService syntaxFacts,
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
            else
            {
                if (allOccurrences &&
                    this.CanReplace(nodeInCurrent))
                {
                    return SemanticEquivalence.AreSemanticallyEquivalent(
                        originalSemanticModel, currentSemanticModel, expressionInOriginal, nodeInCurrent);
                }
            }

            return false;
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
    }
}

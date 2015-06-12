// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class NamedParameterCompletionProvider : AbstractCompletionProvider, IEqualityComparer<IParameterSymbol>
    {
        private const string ColonString = ":";

        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return new TextChange(
                selectedItem.FilterSpan,
                selectedItem.DisplayText.Substring(0, selectedItem.DisplayText.Length - ColonString.Length));
        }

        protected override async Task<bool> IsExclusiveAsync(Document document, int caretPosition, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = syntaxTree.FindTokenOnLeftOfPosition(caretPosition, cancellationToken)
                                  .GetPreviousTokenIfTouchingWord(caretPosition);

            return token.IsMandatoryNamedParameterPosition();
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken))
            {
                return null;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() != SyntaxKind.OpenParenToken &&
                token.Kind() != SyntaxKind.OpenBracketToken &&
                token.Kind() != SyntaxKind.CommaToken)
            {
                return null;
            }

            var argumentList = token.Parent as BaseArgumentListSyntax;
            if (argumentList == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(argumentList, cancellationToken).ConfigureAwait(false);
            var parameterLists = GetParameterLists(semanticModel, position, argumentList.Parent, cancellationToken);
            if (parameterLists == null)
            {
                return null;
            }

            var existingNamedParameters = GetExistingNamedParameters(argumentList, position);
            parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

            var unspecifiedParameters = parameterLists.SelectMany(pl => pl)
                                                      .Where(p => !existingNamedParameters.Contains(p.Name))
                                                      .Distinct(this);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return unspecifiedParameters.Select(
                p =>
                {
                    // Note: the filter text does not include the ':'.  We want to ensure that if 
                    // the user types the name exactly (up to the colon) that it is selected as an
                    // exact match.
                    var workspace = document.Project.Solution.Workspace;
                    var escaped = p.Name.ToIdentifierToken().ToString();
                    return new CSharpCompletionItem(
                        workspace,
                        this,
                        escaped + ColonString,
                        CompletionUtilities.GetTextChangeSpan(text, position),
                        CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, token.SpanStart, p),
                        p.GetGlyph(),
                        sortText: p.Name,
                        filterText: escaped);
                });
        }

        private bool IsValid(ImmutableArray<IParameterSymbol> parameterList, ISet<string> existingNamedParameters)
        {
            // A parameter list is valid if it has parameters that match in name all the existing
            // named parameters that have been provided.
            return existingNamedParameters.Except(parameterList.Select(p => p.Name)).IsEmpty();
        }

        private ISet<string> GetExistingNamedParameters(BaseArgumentListSyntax argumentList, int position)
        {
            var existingArguments = argumentList.Arguments.Where(a => a.Span.End <= position && a.NameColon != null)
                                                          .Select(a => a.NameColon.Name.Identifier.ValueText);

            return existingArguments.ToSet();
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetParameterLists(
            SemanticModel semanticModel,
            int position,
            SyntaxNode invocableNode,
            CancellationToken cancellationToken)
        {
            return invocableNode.TypeSwitch(
                (InvocationExpressionSyntax invocationExpression) => GetInvocationExpressionParameterLists(semanticModel, position, invocationExpression, cancellationToken),
                (ConstructorInitializerSyntax constructorInitializer) => GetConstructorInitializerParameterLists(semanticModel, position, constructorInitializer, cancellationToken),
                (ElementAccessExpressionSyntax elementAccessExpression) => GetElementAccessExpressionParameterLists(semanticModel, position, elementAccessExpression, cancellationToken),
                (ObjectCreationExpressionSyntax objectCreationExpression) => GetObjectCreationExpressionParameterLists(semanticModel, position, objectCreationExpression, cancellationToken));
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetObjectCreationExpressionParameterLists(
            SemanticModel semanticModel,
            int position,
            ObjectCreationExpressionSyntax objectCreationExpression,
            CancellationToken cancellationToken)
        {
            var type = semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type as INamedTypeSymbol;
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (type != null && within != null && type.TypeKind != TypeKind.Delegate)
            {
                return type.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
                                                .Select(c => c.Parameters);
            }

            return null;
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetElementAccessExpressionParameterLists(
            SemanticModel semanticModel,
            int position,
            ElementAccessExpressionSyntax elementAccessExpression,
            CancellationToken cancellationToken)
        {
            var expressionSymbol = semanticModel.GetSymbolInfo(elementAccessExpression.Expression, cancellationToken).GetAnySymbol();
            var expressionType = semanticModel.GetTypeInfo(elementAccessExpression.Expression, cancellationToken).Type;

            if (expressionSymbol != null && expressionType != null)
            {
                var indexers = semanticModel.LookupSymbols(position, expressionType, WellKnownMemberNames.Indexer).OfType<IPropertySymbol>();
                var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
                if (within != null)
                {
                    return indexers.Where(i => i.IsAccessibleWithin(within, throughTypeOpt: expressionType))
                                   .Select(i => i.Parameters);
                }
            }

            return null;
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetConstructorInitializerParameterLists(
            SemanticModel semanticModel,
            int position,
            ConstructorInitializerSyntax constructorInitializer,
            CancellationToken cancellationToken)
        {
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within != null &&
                (within.TypeKind == TypeKind.Struct || within.TypeKind == TypeKind.Class))
            {
                var type = constructorInitializer.Kind() == SyntaxKind.BaseConstructorInitializer
                    ? within.BaseType
                    : within;

                if (type != null)
                {
                    return type.InstanceConstructors.Where(c => c.IsAccessibleWithin(within))
                                                    .Select(c => c.Parameters);
                }
            }

            return null;
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetInvocationExpressionParameterLists(
            SemanticModel semanticModel,
            int position,
            InvocationExpressionSyntax invocationExpression,
            CancellationToken cancellationToken)
        {
            var within = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
            if (within != null)
            {
                var methodGroup = semanticModel.GetMemberGroup(invocationExpression.Expression, cancellationToken).OfType<IMethodSymbol>();
                var expressionType = semanticModel.GetTypeInfo(invocationExpression.Expression, cancellationToken).Type as INamedTypeSymbol;

                if (methodGroup.Any())
                {
                    return methodGroup.Where(m => m.IsAccessibleWithin(within))
                                      .Select(m => m.Parameters);
                }
                else if (expressionType.IsDelegateType())
                {
                    var delegateType = (INamedTypeSymbol)expressionType;
                    return SpecializedCollections.SingletonEnumerable(delegateType.DelegateInvokeMethod.Parameters);
                }
            }

            return null;
        }

        bool IEqualityComparer<IParameterSymbol>.Equals(IParameterSymbol x, IParameterSymbol y)
        {
            return x.Name.Equals(y.Name);
        }

        int IEqualityComparer<IParameterSymbol>.GetHashCode(IParameterSymbol obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}

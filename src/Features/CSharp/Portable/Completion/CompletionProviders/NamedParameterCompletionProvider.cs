// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Completion.Providers;
using System;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class NamedParameterCompletionProvider : CommonCompletionProvider, IEqualityComparer<IParameterSymbol>
    {
        private const string ColonString = ":";

        // Explicitly remove ":" from the set of filter characters because (by default)
        // any character that appears in DisplayText gets treated as a filter char.
        private static readonly CompletionItemRules s_rules = CompletionItemRules.Default
            .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var cancellationToken = context.CancellationToken;

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxTree.IsInNonUserCode(position, cancellationToken))
                {
                    return;
                }

                var token = syntaxTree
                    .FindTokenOnLeftOfPosition(position, cancellationToken)
                    .GetPreviousTokenIfTouchingWord(position);

                if (!token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.OpenBracketToken, SyntaxKind.CommaToken))
                {
                    return;
                }

                if (!(token.Parent is BaseArgumentListSyntax argumentList))
                {
                    return;
                }

                var semanticModel = await document.GetSemanticModelForNodeAsync(argumentList, cancellationToken).ConfigureAwait(false);
                var parameterLists = GetParameterLists(semanticModel, position, argumentList.Parent, cancellationToken);
                if (parameterLists == null)
                {
                    return;
                }

                var existingNamedParameters = GetExistingNamedParameters(argumentList, position);
                parameterLists = parameterLists.Where(pl => IsValid(pl, existingNamedParameters));

                var unspecifiedParameters = parameterLists.SelectMany(pl => pl)
                                                          .Where(p => !existingNamedParameters.Contains(p.Name))
                                                          .Distinct(this);

                if (!unspecifiedParameters.Any())
                {
                    return;
                }

                // Consider refining this logic to mandate completion with an argument name, if preceded by an out-of-position name
                // See https://github.com/dotnet/roslyn/issues/20657
                var languageVersion = ((CSharpParseOptions)document.Project.ParseOptions).LanguageVersion;
                if (languageVersion < LanguageVersion.CSharp7_2 && token.IsMandatoryNamedParameterPosition())
                {
                    context.IsExclusive = true;
                }

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var workspace = document.Project.Solution.Workspace;

                foreach (var parameter in unspecifiedParameters)
                {
                    // Note: the filter text does not include the ':'.  We want to ensure that if 
                    // the user types the name exactly (up to the colon) that it is selected as an
                    // exact match.
                    var escapedName = parameter.Name.ToIdentifierToken().ToString();

                    context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                        displayText: escapedName,
                        displayTextSuffix: ColonString,
                        symbols: ImmutableArray.Create(parameter),
                        rules: s_rules.WithMatchPriority(SymbolMatchPriority.PreferNamedArgument),
                        contextPosition: token.SpanStart,
                        filterText: escapedName));
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

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
            switch (invocableNode)
            {
                case InvocationExpressionSyntax invocationExpression: return GetInvocationExpressionParameterLists(semanticModel, position, invocationExpression, cancellationToken);
                case ConstructorInitializerSyntax constructorInitializer: return GetConstructorInitializerParameterLists(semanticModel, position, constructorInitializer, cancellationToken);
                case ElementAccessExpressionSyntax elementAccessExpression: return GetElementAccessExpressionParameterLists(semanticModel, position, elementAccessExpression, cancellationToken);
                case ObjectCreationExpressionSyntax objectCreationExpression: return GetObjectCreationExpressionParameterLists(semanticModel, position, objectCreationExpression, cancellationToken);
                default: return null;
            }
        }

        private IEnumerable<ImmutableArray<IParameterSymbol>> GetObjectCreationExpressionParameterLists(
            SemanticModel semanticModel,
            int position,
            ObjectCreationExpressionSyntax objectCreationExpression,
            CancellationToken cancellationToken)
        {
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type is INamedTypeSymbol type && within != null && type.TypeKind != TypeKind.Delegate)
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
                    var delegateType = expressionType;
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

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult<TextChange?>(new TextChange(
                selectedItem.Span,
                // Do not insert colon on <Tab> so that user can complete out a variable name that does not currently exist.
                // ch == null is to support the old completion only.
                // Do not insert an extra colon if colon has been explicitly typed.
                (ch == null || ch == '\t' || ch == ':') ? selectedItem.DisplayText : selectedItem.GetEntireDisplayText()));
        }
    }
}

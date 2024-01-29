// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(NamedParameterCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(AttributeNamedParameterCompletionProvider))]
    [Shared]
    internal partial class NamedParameterCompletionProvider : LSPCompletionProvider, IEqualityComparer<IParameterSymbol>
    {
        private const string ColonString = ":";

        // Explicitly remove ":" from the set of filter characters because (by default)
        // any character that appears in DisplayText gets treated as a filter char.
        private static readonly CompletionItemRules s_rules = CompletionItemRules.Default
            .WithFilterCharacterRule(CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, ':'));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NamedParameterCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var cancellationToken = context.CancellationToken;

                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxTree.IsInNonUserCode(position, cancellationToken))
                {
                    return;
                }

                var token = syntaxTree
                    .FindTokenOnLeftOfPosition(position, cancellationToken)
                    .GetPreviousTokenIfTouchingWord(position);

                if (token.Kind() is not (SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken or SyntaxKind.CommaToken))
                {
                    return;
                }

                if (token.Parent is not BaseArgumentListSyntax argumentList)
                {
                    return;
                }

                var semanticModel = await document.ReuseExistingSpeculativeModelAsync(argumentList, cancellationToken).ConfigureAwait(false);
                var parameterLists = GetParameterLists(semanticModel, position, argumentList.Parent!, cancellationToken);
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
                var languageVersion = document.Project.ParseOptions!.LanguageVersion();
                if (languageVersion < LanguageVersion.CSharp7_2 && token.IsMandatoryNamedParameterPosition())
                {
                    context.IsExclusive = true;
                }

                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

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
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
            }
        }

        internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

        private static bool IsValid(ImmutableArray<IParameterSymbol> parameterList, ISet<string> existingNamedParameters)
        {
            // A parameter list is valid if it has parameters that match in name all the existing
            // named parameters that have been provided.
            return existingNamedParameters.Except(parameterList.Select(p => p.Name)).IsEmpty();
        }

        private static ISet<string> GetExistingNamedParameters(BaseArgumentListSyntax argumentList, int position)
        {
            var existingArguments = argumentList.Arguments.Where(a => a.Span.End <= position && a.NameColon != null)
                                                          .Select(a => a.NameColon!.Name.Identifier.ValueText);

            return existingArguments.ToSet();
        }

        private static IEnumerable<ImmutableArray<IParameterSymbol>>? GetParameterLists(
            SemanticModel semanticModel,
            int position,
            SyntaxNode invocableNode,
            CancellationToken cancellationToken)
        {
            return invocableNode switch
            {
                InvocationExpressionSyntax invocationExpression => GetInvocationExpressionParameterLists(semanticModel, position, invocationExpression, cancellationToken),
                ConstructorInitializerSyntax constructorInitializer => GetConstructorInitializerParameterLists(semanticModel, position, constructorInitializer, cancellationToken),
                ElementAccessExpressionSyntax elementAccessExpression => GetElementAccessExpressionParameterLists(semanticModel, position, elementAccessExpression, cancellationToken),
                BaseObjectCreationExpressionSyntax objectCreationExpression => GetObjectCreationExpressionParameterLists(semanticModel, position, objectCreationExpression, cancellationToken),
                PrimaryConstructorBaseTypeSyntax baseType => GetPrimaryConstructorParameterLists(semanticModel, baseType, cancellationToken),
                _ => null,
            };
        }

        private static IEnumerable<ImmutableArray<IParameterSymbol>>? GetObjectCreationExpressionParameterLists(
            SemanticModel semanticModel,
            int position,
            BaseObjectCreationExpressionSyntax objectCreationExpression,
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

        private static IEnumerable<ImmutableArray<IParameterSymbol>>? GetElementAccessExpressionParameterLists(
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
                    return indexers.Where(i => i.IsAccessibleWithin(within, throughType: expressionType))
                                   .Select(i => i.Parameters);
                }
            }

            return null;
        }

        private static IEnumerable<ImmutableArray<IParameterSymbol>>? GetConstructorInitializerParameterLists(
            SemanticModel semanticModel,
            int position,
            ConstructorInitializerSyntax constructorInitializer,
            CancellationToken cancellationToken)
        {
            var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
            if (within is { TypeKind: TypeKind.Struct or TypeKind.Class })
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

        private static IEnumerable<ImmutableArray<IParameterSymbol>>? GetPrimaryConstructorParameterLists(
            SemanticModel semanticModel,
            PrimaryConstructorBaseTypeSyntax baseType,
            CancellationToken cancellationToken)
        {
            var baseList = baseType.Parent;
            if (baseList?.Parent is not BaseTypeDeclarationSyntax typeDeclaration)
                return null;

            var within = semanticModel.GetRequiredDeclaredSymbol(typeDeclaration, cancellationToken);
            if (within is null)
                return null;

            var type = semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type as INamedTypeSymbol;
            return type?.InstanceConstructors
                .Where(m => m.IsAccessibleWithin(within))
                .Select(m => m.Parameters);
        }

        private static IEnumerable<ImmutableArray<IParameterSymbol>>? GetInvocationExpressionParameterLists(
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
                    return SpecializedCollections.SingletonEnumerable(delegateType.DelegateInvokeMethod!.Parameters);
                }
            }

            return null;
        }

        bool IEqualityComparer<IParameterSymbol>.Equals(IParameterSymbol? x, IParameterSymbol? y)
            => x == y || x != null && y != null && x.Name.Equals(y.Name);

        int IEqualityComparer<IParameterSymbol>.GetHashCode(IParameterSymbol obj)
            => obj.Name.GetHashCode();

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult<TextChange?>(new TextChange(
                selectedItem.Span,
                // Insert extra colon if committing with '(' only: "method(parameter:(" is preferred to "method(parameter(".
                // In all other cases, do not add extra colon. Note that colon is already added if committing with ':'.
                ch == '(' ? selectedItem.GetEntireDisplayText() : selectedItem.DisplayText));
        }
    }
}

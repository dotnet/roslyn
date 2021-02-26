﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ExplicitInterfaceTypeCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExplicitInterfaceMemberCompletionProvider))]
    [Shared]
    internal partial class ExplicitInterfaceTypeCompletionProvider : AbstractSymbolCompletionProvider<CSharpSyntaxContext>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExplicitInterfaceTypeCompletionProvider()
        {
        }

        public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, CSharpSyntaxContext context)
            => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var completionCount = context.Items.Count;
                await base.ProvideCompletionsAsync(context).ConfigureAwait(false);

                if (completionCount < context.Items.Count)
                {
                    // If we added any items, then add a suggestion mode item as this is a location 
                    // where a member name could be written, and we should not interfere with that.
                    context.SuggestionModeItem = CreateSuggestionModeItem(
                        CSharpFeaturesResources.member_name,
                        CSharpFeaturesResources.Autoselect_disabled_due_to_member_declaration);
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                // nop
            }
        }

        protected override Task<ImmutableArray<(ISymbol symbol, bool preselect)>> GetSymbolsAsync(
            CompletionContext? completionContext, CSharpSyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var targetToken = context.TargetToken;

            // Don't want to offer this after "async" (even though the compiler may parse that as a type).
            if (SyntaxFacts.GetContextualKeywordKind(targetToken.ValueText) == SyntaxKind.AsyncKeyword)
                return SpecializedTasks.EmptyImmutableArray<(ISymbol symbol, bool preselect)>();

            var typeNode = targetToken.Parent as TypeSyntax;

            while (typeNode != null)
            {
                if (typeNode.Parent is TypeSyntax parentType && parentType.Span.End < position)
                {
                    typeNode = parentType;
                }
                else
                {
                    break;
                }
            }

            if (typeNode == null)
                return SpecializedTasks.EmptyImmutableArray<(ISymbol symbol, bool preselect)>();

            // We weren't after something that looked like a type.
            var tokenBeforeType = typeNode.GetFirstToken().GetPreviousToken();

            if (!IsPreviousTokenValid(tokenBeforeType))
                return SpecializedTasks.EmptyImmutableArray<(ISymbol symbol, bool preselect)>();

            var typeDeclaration = typeNode.GetAncestor<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
                return SpecializedTasks.EmptyImmutableArray<(ISymbol symbol, bool preselect)>();

            // Looks syntactically good.  See what interfaces our containing class/struct/interface has
            Debug.Assert(IsClassOrStructOrInterfaceOrRecord(typeDeclaration));

            var semanticModel = context.SemanticModel;
            var namedType = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
            Contract.ThrowIfNull(namedType);

            using var _ = PooledHashSet<ISymbol>.GetInstance(out var interfaceSet);
            foreach (var directInterface in namedType.Interfaces)
            {
                interfaceSet.Add(directInterface);
                interfaceSet.AddRange(directInterface.AllInterfaces);
            }

            return Task.FromResult(interfaceSet.SelectAsArray(t => (t, preselect: false)));
        }

        private static bool IsPreviousTokenValid(SyntaxToken tokenBeforeType)
        {
            if (tokenBeforeType.Kind() == SyntaxKind.AsyncKeyword)
            {
                tokenBeforeType = tokenBeforeType.GetPreviousToken();
            }

            if (tokenBeforeType.Kind() == SyntaxKind.OpenBraceToken)
            {
                // Show us after the open brace for a class/struct/interface
                return IsClassOrStructOrInterfaceOrRecord(tokenBeforeType.GetRequiredParent());
            }

            if (tokenBeforeType.Kind() == SyntaxKind.CloseBraceToken ||
                tokenBeforeType.Kind() == SyntaxKind.SemicolonToken)
            {
                // Check that we're after a class/struct/interface member.
                var memberDeclaration = tokenBeforeType.GetAncestor<MemberDeclarationSyntax>();
                return memberDeclaration?.GetLastToken() == tokenBeforeType &&
                       IsClassOrStructOrInterfaceOrRecord(memberDeclaration.GetRequiredParent());
            }

            return false;
        }

        private static bool IsClassOrStructOrInterfaceOrRecord(SyntaxNode node)
            => node.Kind() == SyntaxKind.ClassDeclaration || node.Kind() == SyntaxKind.StructDeclaration ||
            node.Kind() == SyntaxKind.InterfaceDeclaration || node.Kind() == SyntaxKind.RecordDeclaration;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ExplicitInterfaceTypeCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExplicitInterfaceMemberCompletionProvider))]
    [Shared]
    internal partial class ExplicitInterfaceTypeCompletionProvider : AbstractSymbolCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExplicitInterfaceTypeCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, insertedCharacterPosition, options);

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context)
            => CompletionUtilities.GetDisplayAndSuffixAndInsertionText(symbol, context);

        protected override async Task<SyntaxContext> CreateContextAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(
                document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }

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
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }

        protected override Task<ImmutableArray<ISymbol>> GetSymbolsAsync(
            SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var targetToken = context.TargetToken;

            // Don't want to offer this after "async" (even though the compiler may parse that as a type).
            if (SyntaxFacts.GetContextualKeywordKind(targetToken.ValueText) == SyntaxKind.AsyncKeyword)
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

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
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            // We weren't after something that looked like a type.
            var tokenBeforeType = typeNode.GetFirstToken().GetPreviousToken();

            if (!IsPreviousTokenValid(tokenBeforeType))
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            var typeDeclaration = typeNode.GetAncestor<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return SpecializedTasks.EmptyImmutableArray<ISymbol>();
            }

            // Looks syntactically good.  See what interfaces our containing class/struct/interface has
            Debug.Assert(IsClassOrStructOrInterfaceOrRecord(typeDeclaration));

            var semanticModel = context.SemanticModel;
            var namedType = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

            var interfaceSet = new HashSet<INamedTypeSymbol>();
            foreach (var directInterface in namedType.Interfaces)
            {
                interfaceSet.Add(directInterface);
                interfaceSet.AddRange(directInterface.AllInterfaces);
            }

            return Task.FromResult(interfaceSet.ToImmutableArray<ISymbol>());
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
                return IsClassOrStructOrInterfaceOrRecord(tokenBeforeType.Parent);
            }

            if (tokenBeforeType.Kind() == SyntaxKind.CloseBraceToken ||
                tokenBeforeType.Kind() == SyntaxKind.SemicolonToken)
            {
                // Check that we're after a class/struct/interface member.
                var memberDeclaration = tokenBeforeType.GetAncestor<MemberDeclarationSyntax>();
                return memberDeclaration?.GetLastToken() == tokenBeforeType &&
                       IsClassOrStructOrInterfaceOrRecord(memberDeclaration.Parent);
            }

            return false;
        }

        private static bool IsClassOrStructOrInterfaceOrRecord(SyntaxNode node)
            => node.Kind() == SyntaxKind.ClassDeclaration || node.Kind() == SyntaxKind.StructDeclaration ||
            node.Kind() == SyntaxKind.InterfaceDeclaration || node.Kind() == SyntaxKind.RecordDeclaration;
    }
}

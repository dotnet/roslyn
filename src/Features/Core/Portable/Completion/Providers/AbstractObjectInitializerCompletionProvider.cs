// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractObjectInitializerCompletionProvider : CommonCompletionProvider
    {
        protected abstract Tuple<ITypeSymbol, Location> GetInitializedType(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken);
        protected abstract HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken);
        protected abstract string EscapeIdentifier(ISymbol symbol);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, length: 0), cancellationToken).ConfigureAwait(false);
            var typeAndLocation = GetInitializedType(document, semanticModel, position, cancellationToken);

            if (typeAndLocation == null)
            {
                return;
            }

            var initializerLocation = typeAndLocation.Item2;
            if (!(typeAndLocation.Item1 is INamedTypeSymbol initializedType))
            {
                return;
            }

            if (await IsExclusiveAsync(document, position, cancellationToken).ConfigureAwait(false))
            {
                context.IsExclusive = true;
            }

            var enclosing = semanticModel.GetEnclosingNamedType(position, cancellationToken);

            // Find the members that can be initialized. If we have a NamedTypeSymbol, also get the overridden members.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, initializedType.WithoutNullability());
            members = members.Where(m => IsInitializable(m, enclosing) && m is
            {
                CanBeReferencedByName: true,
                IsImplicitlyDeclared: false
            } && IsLegalFieldOrProperty(m));

            // Filter out those members that have already been typed
            var alreadyTypedMembers = GetInitializedMembers(semanticModel.SyntaxTree, position, cancellationToken);
            var uninitializedMembers = members.Where(m => !alreadyTypedMembers.Contains(m.Name));

            uninitializedMembers = uninitializedMembers.Where(m => m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            foreach (var uninitializedMember in uninitializedMembers)
            {
                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: EscapeIdentifier(uninitializedMember),
                    displayTextSuffix: "",
                    insertionText: null,
                    symbols: ImmutableArray.Create(uninitializedMember),
                    contextPosition: initializerLocation.SourceSpan.Start,
                    rules: s_rules));
            }
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        protected abstract Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken);

        private bool IsLegalFieldOrProperty(ISymbol symbol)
        {
            return symbol.IsWriteableFieldOrProperty()
                || CanSupportObjectInitializer(symbol);
        }

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        protected virtual bool IsInitializable(ISymbol member, INamedTypeSymbol containingType)
        {
            return
                !member.IsStatic &&
                member.MatchesKind(SymbolKind.Field, SymbolKind.Property) &&
                member.IsAccessibleWithin(containingType);
        }

        private static bool CanSupportObjectInitializer(ISymbol symbol)
        {
            Debug.Assert(!symbol.IsWriteableFieldOrProperty(), "Assertion failed - expected writable field/property check before calling this method.");

            if (symbol is IFieldSymbol fieldSymbol)
            {
                return !fieldSymbol.Type.IsStructType();
            }
            else if (symbol is IPropertySymbol propertySymbol)
            {
                return !propertySymbol.Type.IsStructType();
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}

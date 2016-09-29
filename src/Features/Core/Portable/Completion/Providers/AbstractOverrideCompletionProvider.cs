// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractOverrideCompletionProvider : AbstractMemberInsertingCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new SyntaxAnnotation();

        public AbstractOverrideCompletionProvider()
        {
        }

        public abstract SyntaxToken FindStartingToken(SyntaxTree tree, int position, CancellationToken cancellationToken);
        public abstract ISet<ISymbol> FilterOverrides(ISet<ISymbol> members, ITypeSymbol returnType);
        public abstract bool TryDetermineModifiers(SyntaxToken startToken, SourceText text, int startLine, out Accessibility seenAccessibility, out DeclarationModifiers modifiers);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var state = await ItemGetter.CreateAsync(this, context.Document, context.Position, context.CancellationToken).ConfigureAwait(false);
            var items = await state.GetItemsAsync().ConfigureAwait(false);

            if (items?.Any() == true)
            {
                context.IsExclusive = true;
                context.AddItems(items);
            }
        }

        protected override async Task<ISymbol> GenerateMemberAsync(ISymbol newOverriddenMember, INamedTypeSymbol newContainingType, Document newDocument, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            // Figure out what to insert, and do it. Throw if we've somehow managed to get this far and can't.
            var syntaxFactory = newDocument.GetLanguageService<SyntaxGenerator>();
            var codeGenService = newDocument.GetLanguageService<ICodeGenerationService>();

            var itemModifiers = MemberInsertionCompletionItem.GetModifiers(completionItem);
            var modifiers = itemModifiers.WithIsUnsafe(itemModifiers.IsUnsafe | newOverriddenMember.IsUnsafe());
            if (newOverriddenMember.Kind == SymbolKind.Method)
            {
                return await syntaxFactory.OverrideMethodAsync((IMethodSymbol)newOverriddenMember,
                    modifiers, newContainingType, newDocument, cancellationToken).ConfigureAwait(false);
            }
            else if (newOverriddenMember.Kind == SymbolKind.Property)
            {
                return await syntaxFactory.OverridePropertyAsync((IPropertySymbol)newOverriddenMember,
                    modifiers, newContainingType, newDocument, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return syntaxFactory.OverrideEvent((IEventSymbol)newOverriddenMember,
                    modifiers, newContainingType);
            }
        }

        public abstract bool TryDetermineReturnType(
            SyntaxToken startToken,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ITypeSymbol returnType,
            out SyntaxToken nextToken);

        public bool IsOverridable(ISymbol member, INamedTypeSymbol containingType)
        {
            if (member.IsAbstract || member.IsVirtual || member.IsOverride)
            {
                if (member.IsSealed)
                {
                    return false;
                }

                if (!member.IsAccessibleWithin(containingType))
                {
                    return false;
                }

                switch (member.Kind)
                {
                    case SymbolKind.Event:
                        return true;
                    case SymbolKind.Method:
                        return ((IMethodSymbol)member).MethodKind == MethodKind.Ordinary;
                    case SymbolKind.Property:
                        return !((IPropertySymbol)member).IsWithEvents;
                }
            }

            return false;
        }

        protected bool IsOnStartLine(int position, SourceText text, int startLine)
        {
            return text.Lines.IndexOf(position) == startLine;
        }

        protected ITypeSymbol GetReturnType(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                    return ((IEventSymbol)symbol).Type;
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).ReturnType;
                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).Type;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
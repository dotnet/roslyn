// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
        public abstract ImmutableArray<ISymbol> FilterOverrides(ImmutableArray<ISymbol> members, ITypeSymbol returnType);
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

        protected override Task<ISymbol> GenerateMemberAsync(ISymbol newOverriddenMember, INamedTypeSymbol newContainingType, Document newDocument, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            // Special case: if you are overriding object.ToString(), we will make the return value as non-nullable. The return was made nullable because
            // are implementations out there that will return null, but that's not something we really want new implementations doing. We may need to consider
            // expanding this behavior to other methods in the future; if that is the case then we would want there to be an attribute on the return type
            // rather than updating this list, but for now there is no such attribute until we find more cases for it. See
            // https://github.com/dotnet/roslyn/issues/30317 for some additional conversation about this design decision.
            //
            // We don't check if methodSymbol.ContainingType is object, in case you're overriding something that is itself an override
            if (newOverriddenMember is IMethodSymbol methodSymbol &&
                methodSymbol.Name == "ToString" &&
                methodSymbol.Parameters.Length == 0)
            {
                newOverriddenMember = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, returnType: methodSymbol.ReturnType.WithNullability(NullableAnnotation.NotAnnotated));
            }

            // Figure out what to insert, and do it. Throw if we've somehow managed to get this far and can't.
            var syntaxFactory = newDocument.GetLanguageService<SyntaxGenerator>();

            var itemModifiers = MemberInsertionCompletionItem.GetModifiers(completionItem);
            var modifiers = itemModifiers.WithIsUnsafe(itemModifiers.IsUnsafe | newOverriddenMember.IsUnsafe());

            return syntaxFactory.OverrideAsync(
                newOverriddenMember, newContainingType, newDocument, modifiers, cancellationToken);
        }

        public abstract bool TryDetermineReturnType(
            SyntaxToken startToken,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out ITypeSymbol returnType,
            out SyntaxToken nextToken);

        protected bool IsOnStartLine(int position, SourceText text, int startLine)
        {
            return text.Lines.IndexOf(position) == startLine;
        }

        protected ITypeSymbol GetReturnType(ISymbol symbol)
            => symbol.Kind switch
            {
                SymbolKind.Event => ((IEventSymbol)symbol).Type,
                SymbolKind.Method => ((IMethodSymbol)symbol).ReturnType,
                SymbolKind.Property => ((IPropertySymbol)symbol).Type,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind),
            };
    }
}

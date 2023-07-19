// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractPartialMethodCompletionProvider : AbstractMemberInsertingCompletionProvider
    {
        protected static readonly SymbolDisplayFormat SignatureDisplayFormat =
                new(
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    memberOptions:
                        SymbolDisplayMemberOptions.IncludeParameters,
                    parameterOptions:
                        SymbolDisplayParameterOptions.IncludeName |
                        SymbolDisplayParameterOptions.IncludeType |
                        SymbolDisplayParameterOptions.IncludeParamsRefOut,
                    miscellaneousOptions:
                        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                        SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected AbstractPartialMethodCompletionProvider()
        {
        }

        protected abstract bool IncludeAccessibility(IMethodSymbol method, CancellationToken cancellationToken);
        protected abstract bool IsPartialMethodCompletionContext(SyntaxTree tree, int position, CancellationToken cancellationToken, out DeclarationModifiers modifiers, out SyntaxToken token);
        protected abstract string GetDisplayText(IMethodSymbol method, SemanticModel semanticModel, int position);
        protected abstract bool IsPartial(IMethodSymbol method);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!IsPartialMethodCompletionContext(tree, position, cancellationToken, out var modifiers, out var token))
            {
                return;
            }

            var items = await CreatePartialItemsAsync(
                document, position, context.CompletionListSpan, modifiers, token, cancellationToken).ConfigureAwait(false);

            if (items?.Any() == true)
            {
                context.IsExclusive = true;
                context.AddItems(items);
            }
        }

        protected override async Task<ISymbol> GenerateMemberAsync(ISymbol member, INamedTypeSymbol containingType, Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var syntaxFactory = document.GetLanguageService<SyntaxGenerator>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var method = (IMethodSymbol)member;
            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: ImmutableArray<AttributeData>.Empty,
                accessibility: IncludeAccessibility(method, cancellationToken) ? method.DeclaredAccessibility : Accessibility.NotApplicable,
                modifiers: MemberInsertionCompletionItem.GetModifiers(item),
                returnType: method.ReturnType,
                refKind: method.RefKind,
                explicitInterfaceImplementations: default,
                name: member.Name,
                typeParameters: method.TypeParameters,
                parameters: method.Parameters.SelectAsArray(p => CodeGenerationSymbolFactory.CreateParameterSymbol(p.GetAttributes(), p.RefKind, p.IsParams, p.Type, p.Name)),
                statements: syntaxFactory.CreateThrowNotImplementedStatementBlock(semanticModel.Compilation));
        }

        protected async Task<IEnumerable<CompletionItem>?> CreatePartialItemsAsync(
            Document document, int position, TextSpan span, DeclarationModifiers modifiers, SyntaxToken token, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Only inside classes and structs
            if (semanticModel.GetEnclosingSymbol(position, cancellationToken) is not INamedTypeSymbol enclosingSymbol)
                return null;

            if (enclosingSymbol.TypeKind is not (TypeKind.Struct or TypeKind.Class))
                return null;

            var symbols = semanticModel.LookupSymbols(position, container: enclosingSymbol)
                                        .OfType<IMethodSymbol>()
                                        .Where(m => IsPartial(m) && m.PartialImplementationPart == null);

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var line = text.Lines.IndexOf(position);
            var lineSpan = text.Lines.GetLineFromPosition(position).Span;
            return symbols.Select(s => CreateItem(s, line, span, semanticModel, modifiers, token));
        }

        private CompletionItem CreateItem(IMethodSymbol method, int line, TextSpan span, SemanticModel semanticModel, DeclarationModifiers modifiers, SyntaxToken token)
        {
            modifiers = new DeclarationModifiers(method.IsStatic, isUnsafe: method.RequiresUnsafeModifier(), isPartial: true, isAsync: modifiers.IsAsync);
            var displayText = GetDisplayText(method, semanticModel, span.Start);

            return MemberInsertionCompletionItem.Create(
                displayText,
                displayTextSuffix: "",
                modifiers,
                line,
                method,
                token,
                span.Start,
                rules: GetRules());
        }
    }
}

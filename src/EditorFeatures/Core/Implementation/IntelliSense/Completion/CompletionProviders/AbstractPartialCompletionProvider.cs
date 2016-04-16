// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal abstract partial class AbstractPartialCompletionProvider : AbstractMemberInsertingCompletionProvider
    {
        protected static readonly SymbolDisplayFormat SignatureDisplayFormat =
                new SymbolDisplayFormat(
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

        public AbstractPartialCompletionProvider(IWaitIndicator waitIndicator)
            : base(waitIndicator)
        {
        }

        protected abstract bool IsPartialCompletionContext(SyntaxTree tree, int position, CancellationToken cancellationToken, out DeclarationModifiers modifiers, out SyntaxToken token);
        protected abstract string GetDisplayText(IMethodSymbol method, SemanticModel semanticModel, int position);
        protected abstract Task<TextSpan> GetTextChangeSpanAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract bool IsPartial(IMethodSymbol m);

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            DeclarationModifiers modifiers;
            SyntaxToken token;
            if (!IsPartialCompletionContext(tree, position, cancellationToken, out modifiers, out token))
            {
                return;
            }

            var items = await CreatePartialItemsAsync(document, position, modifiers, token, cancellationToken).ConfigureAwait(false);

            if (items?.Any() == true)
            {
                context.MakeExclusive(true);
                context.AddItems(items);
            }
        }

        protected override async Task<ISymbol> GenerateMemberAsync(ISymbol member, INamedTypeSymbol containingType, Document document, MemberInsertionCompletionItem item, CancellationToken cancellationToken)
        {
            var syntaxFactory = document.GetLanguageService<SyntaxGenerator>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(attributes: new List<AttributeData>(),
                accessibility: Accessibility.NotApplicable,
                modifiers: item.Modifiers,
                returnType: semanticModel.Compilation.GetSpecialType(SpecialType.System_Void),
                explicitInterfaceSymbol: null,
                name: member.Name,
                typeParameters: ((IMethodSymbol)member).TypeParameters,
                parameters: member.GetParameters().Select(p => CodeGenerationSymbolFactory.CreateParameterSymbol(p.GetAttributes(), p.RefKind, p.IsParams, p.Type, p.Name)).ToList(),
                statements: syntaxFactory.CreateThrowNotImplementedStatementBlock(semanticModel.Compilation));
        }

        protected async Task<IEnumerable<CompletionItem>> CreatePartialItemsAsync(Document document, int position, DeclarationModifiers modifiers, SyntaxToken token, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var enclosingSymbol = semanticModel.GetEnclosingSymbol(position, cancellationToken) as INamedTypeSymbol;

            // Only inside classes and structs
            if (enclosingSymbol == null || !(enclosingSymbol.TypeKind == TypeKind.Struct || enclosingSymbol.TypeKind == TypeKind.Class))
            {
                return null;
            }

            var symbols = semanticModel.LookupSymbols(position, container: enclosingSymbol)
                                        .OfType<IMethodSymbol>()
                                        .Where(m => IsPartial(m) && m.PartialImplementationPart == null);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var line = text.Lines.IndexOf(position);
            var lineSpan = text.Lines.GetLineFromPosition(position).Span;
            var span = await GetTextChangeSpanAsync(document, position, cancellationToken).ConfigureAwait(false);
            return symbols.Select(s => CreateItem(s, line, lineSpan, span, semanticModel, modifiers, document, token));
        }

        private CompletionItem CreateItem(IMethodSymbol method, int line, TextSpan lineSpan, TextSpan span, SemanticModel semanticModel, DeclarationModifiers modifiers, Document document, SyntaxToken token)
        {
            modifiers = new DeclarationModifiers(method.IsStatic, isUnsafe: method.IsUnsafe(), isPartial: true, isAsync: modifiers.IsAsync);
            var displayText = GetDisplayText(method, semanticModel, span.Start);

            return new MemberInsertionCompletionItem(
                this,
                displayText,
                span,
                CommonCompletionUtilities.CreateDescriptionFactory(document.Project.Solution.Workspace, semanticModel, span.Start, method),
                Glyph.MethodPrivate,
                modifiers,
                line,
                method.GetSymbolKey(),
                token);
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(PartialTypeCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(PartialMethodCompletionProvider))]
    [Shared]
    internal partial class PartialTypeCompletionProvider : AbstractPartialTypeCompletionProvider
    {
        private const string InsertionTextOnLessThan = nameof(InsertionTextOnLessThan);

        private static readonly SymbolDisplayFormat _symbolFormatWithGenerics =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat _symbolFormatWithoutGenerics =
            _symbolFormatWithGenerics.WithGenericsOptions(SymbolDisplayGenericsOptions.None);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PartialTypeCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            var ch = text[characterPosition];
            return ch == ' ' ||
                (CompletionUtilities.IsStartingNewWord(text, characterPosition) &&
                options.GetOption(CompletionOptions.TriggerOnTypingLetters2, LanguageNames.CSharp));
        }

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override SyntaxNode GetPartialTypeSyntaxNode(SyntaxTree tree, int position, CancellationToken cancellationToken)
            => tree.IsPartialTypeDeclarationNameContext(position, cancellationToken, out var declaration) ? declaration : null;

        protected override Task<SyntaxContext> CreateSyntaxContextAsync(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => Task.FromResult<SyntaxContext>(CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken));

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(
            INamedTypeSymbol symbol, SyntaxContext context)
        {
            var displayAndInsertionText = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position, _symbolFormatWithGenerics);
            return (displayAndInsertionText, "", displayAndInsertionText);
        }

        protected override IEnumerable<INamedTypeSymbol> LookupCandidateSymbols(SyntaxContext context, INamedTypeSymbol declaredSymbol, CancellationToken cancellationToken)
        {
            var candidates = base.LookupCandidateSymbols(context, declaredSymbol, cancellationToken);

            // The base class applies a broad filter when finding candidates, but since C# requires
            // that all parts have the "partial" modifier, the results can be trimmed further here.
            return candidates?.Where(symbol => symbol.DeclaringSyntaxReferences.Any(reference => IsPartialTypeDeclaration(reference.GetSyntax(cancellationToken))));
        }

        private static bool IsPartialTypeDeclaration(SyntaxNode syntax)
            => syntax is BaseTypeDeclarationSyntax declarationSyntax && declarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

        protected override ImmutableDictionary<string, string> GetProperties(
            INamedTypeSymbol symbol, SyntaxContext context)
        {
            return ImmutableDictionary<string, string>.Empty.Add(InsertionTextOnLessThan, symbol.Name.EscapeIdentifier());
        }

        public override async Task<TextChange?> GetTextChangeAsync(
            Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            if (ch == '<')
            {
                if (selectedItem.Properties.TryGetValue(InsertionTextOnLessThan, out var insertionText))
                {
                    return new TextChange(selectedItem.Span, insertionText);
                }
            }

            return await base.GetTextChangeAsync(document, selectedItem, ch, cancellationToken).ConfigureAwait(false);
        }
    }
}

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
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ObjectCreationCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(ExplicitInterfaceTypeCompletionProvider))]
    [Shared]
    internal partial class ObjectCreationCompletionProvider : AbstractObjectCreationCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ObjectCreationCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

        protected override SyntaxNode GetObjectCreationNewExpression(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            if (tree != null)
            {
                if (!tree.IsInNonUserCode(position, cancellationToken))
                {
                    var tokenOnLeftOfPosition = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
                    var newToken = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

                    // Only after 'new'.
                    if (newToken.Kind() == SyntaxKind.NewKeyword)
                    {
                        // Only if the 'new' belongs to an object creation expression (and isn't a 'new'
                        // modifier on a member).
                        if (tree.IsObjectCreationTypeContext(position, tokenOnLeftOfPosition, cancellationToken))
                        {
                            return newToken.Parent as ExpressionSyntax;
                        }
                    }
                }
            }

            return null;
        }

        protected override async Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            return CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }

        protected override async Task<ImmutableArray<ISymbol>> GetPreselectedSymbolsAsync(SyntaxContext context, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var result = await base.GetPreselectedSymbolsAsync(context, position, options, cancellationToken).ConfigureAwait(false);
            if (result.Any())
            {
                var type = (ITypeSymbol)result.Single();
                var alias = await type.FindApplicableAliasAsync(position, context.SemanticModel, cancellationToken).ConfigureAwait(false);
                if (alias != null)
                {
                    return ImmutableArray.Create(alias);
                }
            }

            return result;
        }

        protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, SyntaxContext context)
        {
            if (symbol is IAliasSymbol)
            {
                return (symbol.Name, "", symbol.Name);
            }

            if (symbol is ITypeSymbol typeSymbol)
            {
                // typeSymbol may be a symbol that is nullable if the place we are assigning to is null, for example
                //
                //     object? o = new |
                //
                // We strip the top-level nullability so we don't end up suggesting "new object?" here. Nested nullability would still
                // be generated.
                return base.GetDisplayAndSuffixAndInsertionText(typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated), context);
            }

            return base.GetDisplayAndSuffixAndInsertionText(symbol, context);
        }

        private static readonly CompletionItemRules s_arrayRules =
            CompletionItemRules.Create(
                commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(', '[')),
                matchPriority: MatchPriority.Default,
                selectionBehavior: CompletionItemSelectionBehavior.SoftSelection);

        private static readonly CompletionItemRules s_objectRules =
            CompletionItemRules.Create(
                commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(', '[')),
                matchPriority: MatchPriority.Preselect,
                selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

        private static readonly CompletionItemRules s_defaultRules =
            CompletionItemRules.Create(
                commitCharacterRules: ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(', '[', '{')),
                matchPriority: MatchPriority.Preselect,
                selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

        protected override CompletionItemRules GetCompletionItemRules(IReadOnlyList<ISymbol> symbols, bool preselect)
        {
            if (!preselect)
            {
                return s_arrayRules;
            }

            // SPECIAL: If the preselected symbol is System.Object, don't commit on '{'.
            // Otherwise, it is cumbersome to type an anonymous object when the target type is object.
            // The user would get 'new object {' rather than 'new {'. Since object doesn't have any
            // properties, the user never really wants to commit 'new object {' anyway.
            var namedTypeSymbol = symbols.Count > 0 ? symbols[0] as INamedTypeSymbol : null;
            if (namedTypeSymbol?.SpecialType == SpecialType.System_Object)
            {
                return s_objectRules;
            }

            return s_defaultRules;
        }
    }
}

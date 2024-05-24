// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ObjectCreationCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(ExplicitInterfaceTypeCompletionProvider))]
[Shared]
internal partial class ObjectCreationCompletionProvider : AbstractObjectCreationCompletionProvider<CSharpSyntaxContext>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ObjectCreationCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerAfterSpaceOrStartOfWordCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.SpaceTriggerCharacter;

    protected override SyntaxNode? GetObjectCreationNewExpression(SyntaxTree tree, int position, CancellationToken cancellationToken)
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
                        return newToken.Parent as ExpressionSyntax;
                }
            }
        }

        return null;
    }

    protected override async Task<ImmutableArray<SymbolAndSelectionInfo>> GetSymbolsAsync(
        CompletionContext? completionContext, CSharpSyntaxContext context, int position, CompletionOptions options, CancellationToken cancellationToken)
    {
        var result = await base.GetSymbolsAsync(completionContext, context, position, options, cancellationToken).ConfigureAwait(false);
        if (result.Any())
        {
            var type = (ITypeSymbol)result.Single().Symbol;
            var alias = await type.FindApplicableAliasAsync(position, context.SemanticModel, cancellationToken).ConfigureAwait(false);
            if (alias != null)
                return [new SymbolAndSelectionInfo(alias, result.Single().Preselect)];
        }

        return result;
    }

    protected override (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(ISymbol symbol, CSharpSyntaxContext context)
    {
        if (symbol is IAliasSymbol)
        {
            return (symbol.Name, "", symbol.Name);
        }

        // typeSymbol may be a symbol that is nullable if the place we are assigning to is null, for example
        //
        //     object? o = new |
        //
        // We strip the top-level nullability so we don't end up suggesting "new object?" here. Nested nullability would still
        // be generated.
        if (symbol is ITypeSymbol typeSymbol)
        {
            symbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        var displayString = symbol.ToMinimalDisplayString(context.SemanticModel, context.Position);
        return (displayString, suffix: "", displayString);
    }

    private static readonly CompletionItemRules s_arrayRules =
        CompletionItemRules.Create(
            commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(', '[')],
            matchPriority: MatchPriority.Default,
            selectionBehavior: CompletionItemSelectionBehavior.SoftSelection);

    private static readonly CompletionItemRules s_objectRules =
        CompletionItemRules.Create(
            commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(', '[', ';', '.')],
            matchPriority: MatchPriority.Preselect,
            selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    private static readonly CompletionItemRules s_defaultRules =
        CompletionItemRules.Create(
            commitCharacterRules: [CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, ' ', '(', '[', '{', ';', '.')],
            matchPriority: MatchPriority.Preselect,
            selectionBehavior: CompletionItemSelectionBehavior.HardSelection);

    protected override CompletionItemRules GetCompletionItemRules(ImmutableArray<SymbolAndSelectionInfo> symbols)
    {
        var preselect = symbols.Any(static t => t.Preselect);
        if (!preselect)
            return s_arrayRules;

        // SPECIAL: If the preselected symbol is System.Object, don't commit on '{'.
        // Otherwise, it is cumbersome to type an anonymous object when the target type is object.
        // The user would get 'new object {' rather than 'new {'. Since object doesn't have any
        // properties, the user never really wants to commit 'new object {' anyway.
        var namedTypeSymbol = symbols.Length > 0 ? symbols[0].Symbol as INamedTypeSymbol : null;
        if (namedTypeSymbol?.SpecialType == SpecialType.System_Object)
            return s_objectRules;

        return s_defaultRules;
    }

    protected override string GetInsertionText(CompletionItem item, char ch)
    {
        if (ch is ';' or '.')
        {
            CompletionProvidersLogger.LogCustomizedCommitToAddParenthesis(ch);
            return SymbolCompletionItem.GetInsertionText(item) + "()";
        }

        return base.GetInsertionText(item, ch);
    }
}

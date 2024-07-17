// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(PropertySubpatternCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(InternalsVisibleToCompletionProvider))]
[Shared]
internal class PropertySubpatternCompletionProvider : LSPCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PropertySubpatternCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    // Examples:
    // is { $$
    // is { Property.$$
    // is { Property.Property2.$$
    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var document = context.Document;
        var position = context.Position;
        var cancellationToken = context.CancellationToken;
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

        // For `is { Property.Property2.$$`, we get:
        // - the property pattern clause `{ ... }` and
        // - the member access before the last dot `Property.Property2` (or null)
        var (propertyPatternClause, memberAccess) = TryGetPropertyPatternClause(tree, position, cancellationToken);
        if (propertyPatternClause is null)
        {
            return;
        }

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        var propertyPatternType = semanticModel.GetTypeInfo((PatternSyntax)propertyPatternClause.Parent!, cancellationToken).ConvertedType;
        // For simple property patterns, the type we want is the "input type" of the property pattern, ie the type of `c` in `c is { $$ }`.
        // For extended property patterns, we get the type by following the chain of members that we have so far, ie
        // the type of `c.Property` for `c is { Property.$$ }` and the type of `c.Property1.Property2` for `c is { Property1.Property2.$$ }`.
        var type = GetMemberAccessType(propertyPatternType, memberAccess, document, semanticModel, position);

        if (type is null)
        {
            return;
        }

        // Find the members that can be tested.
        var members = GetCandidatePropertiesAndFields(document, semanticModel, position, type);
        members = members.WhereAsArray(m => m.IsEditorBrowsable(context.CompletionOptions.MemberDisplayOptions.HideAdvancedMembers, semanticModel.Compilation));

        if (memberAccess is null)
        {
            // Filter out those members that have already been typed as simple (not extended) properties
            var alreadyTestedMembers = new HashSet<string>(propertyPatternClause.Subpatterns.Select(
                p => p.NameColon?.Name.Identifier.ValueText).Where(s => !string.IsNullOrEmpty(s))!);

            members = members.WhereAsArray(m => !alreadyTestedMembers.Contains(m.Name));
        }

        foreach (var member in members)
        {
            context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                displayText: member.Name.EscapeIdentifier(),
                displayTextSuffix: "",
                insertionText: null,
                symbols: ImmutableArray.Create(member),
                contextPosition: context.Position,
                rules: s_rules));
        }

        return;

        // We have to figure out the type of the extended property ourselves, because
        // the semantic model could not provide the answer we want in incomplete syntax:
        // `c is { X. }`
        static ITypeSymbol? GetMemberAccessType(ITypeSymbol? type, ExpressionSyntax? expression, Document document, SemanticModel semanticModel, int position)
        {
            if (expression is null)
            {
                return type;
            }
            else if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                type = GetMemberAccessType(type, memberAccess.Expression, document, semanticModel, position);
                return GetMemberType(type, name: memberAccess.Name.Identifier.ValueText, document, semanticModel, position);
            }
            else if (expression is IdentifierNameSyntax identifier)
            {
                return GetMemberType(type, name: identifier.Identifier.ValueText, document, semanticModel, position);
            }

            throw ExceptionUtilities.Unreachable();
        }

        static ITypeSymbol? GetMemberType(ITypeSymbol? type, string name, Document document, SemanticModel semanticModel, int position)
        {
            var members = GetCandidatePropertiesAndFields(document, semanticModel, position, type);
            var matches = members.WhereAsArray(m => m.Name == name);
            if (matches.Length != 1)
            {
                return null;
            }

            return matches[0] switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                _ => throw ExceptionUtilities.Unreachable(),
            };
        }

        static ImmutableArray<ISymbol> GetCandidatePropertiesAndFields(Document document, SemanticModel semanticModel, int position, ITypeSymbol? type)
        {
            var members = semanticModel.LookupSymbols(position, type);
            return members.WhereAsArray(m => m.CanBeReferencedByName &&
                IsFieldOrReadableProperty(m) &&
                !m.IsImplicitlyDeclared &&
                !m.IsStatic);
        }
    }

    private static bool IsFieldOrReadableProperty(ISymbol symbol)
    {
        if (symbol.IsKind(SymbolKind.Field))
        {
            return true;
        }

        if (symbol.IsKind(SymbolKind.Property) && !((IPropertySymbol)symbol).IsWriteOnly)
        {
            return true;
        }

        return false;
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) || text[characterPosition] == ' ';

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters.Add(' ');

    private static (PropertyPatternClauseSyntax?, ExpressionSyntax?) TryGetPropertyPatternClause(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        if (tree.IsInNonUserCode(position, cancellationToken))
        {
            return default;
        }

        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is SyntaxKind.CommaToken or SyntaxKind.OpenBraceToken)
        {
            return token.Parent is PropertyPatternClauseSyntax { Parent: PatternSyntax } propertyPatternClause
                ? (propertyPatternClause, null)
                : default;
        }

        if (token.IsKind(SyntaxKind.DotToken))
        {
            // is { Property1.$$ }
            // is { Property1.$$  Property1.Property2: ... } // typing before an existing pattern
            return token.Parent is MemberAccessExpressionSyntax memberAccess && IsExtendedPropertyPattern(memberAccess, out var propertyPatternClause)
                ? (propertyPatternClause, memberAccess.Expression)
                : default;
        }

        return default;

        static bool IsExtendedPropertyPattern(MemberAccessExpressionSyntax memberAccess, [NotNullWhen(true)] out PropertyPatternClauseSyntax? propertyPatternClause)
        {
            while (memberAccess.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                memberAccess = (MemberAccessExpressionSyntax)memberAccess.Parent;
            }

            if (memberAccess is { Parent.Parent: SubpatternSyntax { Parent: PropertyPatternClauseSyntax found } })
            {
                propertyPatternClause = found;
                return true;
            }

            propertyPatternClause = null;
            return false;
        }
    }
}

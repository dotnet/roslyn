// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class EnumAndCompletionListTagCompletionProvider : CommonCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            // Bring up on space or at the start of a word, or after a ( or [.
            //
            // Note: we don't want to bring this up after traditional enum operators like & or |.
            // That's because we don't like the experience where the enum appears directly after the
            // operator.  Instead, the user normally types <space> and we will bring up the list
            // then.
            var ch = text[characterPosition];
            return
                ch == ' ' ||
                ch == '[' ||
                ch == '(' ||
                ch == '~' ||
                (options.GetOption(CompletionOptions.TriggerOnTypingLetters, LanguageNames.CSharp) && CompletionUtilities.IsStartingNewWord(text, characterPosition));
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var options = context.Options;
                var cancellationToken = context.CancellationToken;

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (tree.IsInNonUserCode(position, cancellationToken))
                {
                    return;
                }

                var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                .GetPreviousTokenIfTouchingWord(position);

                if (token.IsMandatoryNamedParameterPosition())
                {
                    return;
                }

                // Don't show up within member access
                // This previously worked because the type inferrer didn't work
                // in member access expressions.
                // The regular SymbolCompletionProvider will handle completion after .
                if (token.IsKind(SyntaxKind.DotToken))
                {
                    return;
                }

                var typeInferenceService = document.GetLanguageService<ITypeInferenceService>();
                Contract.ThrowIfNull(typeInferenceService, nameof(typeInferenceService));

                var span = new TextSpan(position, 0);
                var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
                var types = typeInferenceService.InferTypes(semanticModel, position,
                    cancellationToken: cancellationToken);

                if (types.Length == 0)
                {
                    types = ImmutableArray.Create<ITypeSymbol>(semanticModel.Compilation.ObjectType);
                }

                foreach (var typeIterator in types)
                {
                    var type = typeIterator;

                    // If we have a Nullable<T>, unwrap it.
                    if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        type = type.GetTypeArguments().FirstOrDefault();

                        if (type == null)
                        {
                            continue;
                        }
                    }

                    if (type.TypeKind != TypeKind.Enum)
                    {
                        type = TryGetEnumTypeInEnumInitializer(semanticModel, token, type, cancellationToken) ??
                               TryGetCompletionListType(type, semanticModel.GetEnclosingNamedType(position, cancellationToken), semanticModel.Compilation);

                        if (type == null)
                        {
                            continue;
                        }
                    }

                    if (!type.IsEditorBrowsable(options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language), semanticModel.Compilation))
                    {
                        continue;
                    }

                    // Does type have any aliases?
                    var alias = await type.FindApplicableAlias(position, semanticModel, cancellationToken).ConfigureAwait(false);

                    var displayService = document.GetLanguageService<ISymbolDisplayService>();
                    var displayText = alias != null
                        ? alias.Name
                        : displayService.ToMinimalDisplayString(semanticModel, position, type);

                    var workspace = document.Project.Solution.Workspace;
                    var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var item = SymbolCompletionItem.CreateWithSymbolId(
                        displayText: displayText,
                        displayTextSuffix: "",
                        symbols: ImmutableArray.Create(alias ?? type),
                        rules: s_rules.WithMatchPriority(MatchPriority.Preselect),
                        contextPosition: position);

                    context.AddItem(item);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private ITypeSymbol TryGetEnumTypeInEnumInitializer(
            SemanticModel semanticModel, SyntaxToken token,
            ITypeSymbol type, CancellationToken cancellationToken)
        {
            // https://github.com/dotnet/roslyn/issues/5419
            //
            // 14.3: "Within an enum member initializer, values of other enum members are always 
            // treated as having the type of their underlying type"

            // i.e. if we have "enum E { X, Y, Z = X | 
            // then we want to offer the enum after the |.  However, the compiler will report this
            // as an 'int' type, not the enum type.

            // See if we're after a common enum-combining operator.
            if (token.Kind() == SyntaxKind.BarToken ||
                token.Kind() == SyntaxKind.AmpersandToken ||
                token.Kind() == SyntaxKind.CaretToken)
            {
                // See if the type we're looking at is the underlying type for the enum we're contained in.
                var containingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);
                if (containingType?.TypeKind == TypeKind.Enum &&
                    type.Equals(containingType.EnumUnderlyingType))
                {
                    // If so, walk back to the token before the operator token and see if it binds to a member
                    // of this enum.
                    var previousToken = token.GetPreviousToken();
                    var symbol = semanticModel.GetSymbolInfo(previousToken.Parent, cancellationToken).Symbol;

                    if (symbol?.Kind == SymbolKind.Field &&
                        containingType.Equals(symbol.ContainingType))
                    {
                        // If so, then offer this as a place for enum completion for the enum we're currently 
                        // inside of.
                        return containingType;
                    }
                }
            }

            return null;
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        private static readonly CompletionItemRules s_rules =
            CompletionItemRules.Default.WithCommitCharacterRules(ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '.')))
                                       .WithMatchPriority(MatchPriority.Preselect)
                                       .WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection);

        private INamedTypeSymbol TryGetCompletionListType(ITypeSymbol type, INamedTypeSymbol within, Compilation compilation)
        {
            // PERF: None of the SpecialTypes include <completionlist> tags,
            // so we don't even need to load the documentation.
            if (type.IsSpecialType())
            {
                return null;
            }

            // PERF: Avoid parsing XML unless the text contains the word "completionlist".
            var xmlText = type.GetDocumentationCommentXml();
            if (xmlText == null || !xmlText.Contains(DocumentationCommentXmlNames.CompletionListElementName))
            {
                return null;
            }

            var documentation = Shared.Utilities.DocumentationComment.FromXmlFragment(xmlText);

            var completionListType = documentation.CompletionListCref != null
                ? DocumentationCommentId.GetSymbolsForDeclarationId(documentation.CompletionListCref, compilation).OfType<INamedTypeSymbol>().FirstOrDefault()
                : null;

            return completionListType != null && completionListType.IsAccessibleWithin(within)
                ? completionListType
                : null;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class EnumAndCompletionListTagCompletionProvider : CompletionListProvider
    {
        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
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

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
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

                var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
                if (token.IsMandatoryNamedParameterPosition())
                {
                    return;
                }

                var typeInferenceService = document.GetLanguageService<ITypeInferenceService>();

                var span = new TextSpan(position, 0);
                var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
                var type = typeInferenceService.InferType(semanticModel, position,
                    objectAsDefault: true,
                    cancellationToken: cancellationToken);

                // If we have a Nullable<T>, unwrap it.
                if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    type = type.GetTypeArguments().FirstOrDefault();

                    if (type == null)
                    {
                        return;
                    }
                }

                if (type.TypeKind != TypeKind.Enum)
                {
                    type = GetCompletionListType(type, semanticModel.GetEnclosingNamedType(position, cancellationToken), semanticModel.Compilation);
                    if (type == null)
                    {
                        return;
                    }
                }

                if (!type.IsEditorBrowsable(options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language), semanticModel.Compilation))
                {
                    return;
                }

                // Does type have any aliases?
                ISymbol alias = await type.FindApplicableAlias(position, semanticModel, cancellationToken).ConfigureAwait(false);

                var displayService = document.GetLanguageService<ISymbolDisplayService>();
                var displayText = alias != null
                    ? alias.Name
                    : displayService.ToMinimalDisplayString(semanticModel, position, type);

                var workspace = document.Project.Solution.Workspace;
                var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var textChangeSpan = CompletionUtilities.GetTextChangeSpan(text, position);

                var item = new CompletionItem(
                    this,
                    displayText: displayText,
                    filterSpan: textChangeSpan,
                    descriptionFactory: CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, position, alias ?? type),
                    glyph: (alias ?? type).GetGlyph(),
                    preselect: true,
                    rules: ItemRules.Instance);

                context.AddItem(item);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private INamedTypeSymbol GetCompletionListType(ITypeSymbol type, INamedTypeSymbol within, Compilation compilation)
        {
            // PERF: None of the SpecialTypes include <completionlist> tags,
            // so we don't even need to load the documentation.
            if (type.IsSpecialType())
            {
                return null;
            }

            // PERF: Avoid parsing XML unless the text contains the word "completionlist".
            string xmlText = type.GetDocumentationCommentXml();
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ExplicitInterfaceMemberCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    internal partial class ExplicitInterfaceMemberCompletionProvider : LSPCompletionProvider
    {
        private const string InsertionTextOnOpenParen = nameof(InsertionTextOnOpenParen);

        private static readonly SymbolDisplayFormat s_signatureDisplayFormat =
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExplicitInterfaceMemberCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => text[characterPosition] == '.';

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = ImmutableHashSet.Create('.');

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var document = context.Document;
                var position = context.Position;
                var options = context.Options;
                var cancellationToken = context.CancellationToken;

                var span = new TextSpan(position, length: 0);
                var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
                var syntaxTree = semanticModel.SyntaxTree;

                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

                if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
                    semanticFacts.IsPreProcessorDirectiveContext(semanticModel, position, cancellationToken))
                {
                    return;
                }

                var targetToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                            .GetPreviousTokenIfTouchingWord(position);

                if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken))
                {
                    return;
                }

                var node = targetToken.Parent;

                if (node.Kind() != SyntaxKind.ExplicitInterfaceSpecifier)
                {
                    return;
                }

                // Bind the interface name which is to the left of the dot
                var name = ((ExplicitInterfaceSpecifierSyntax)node).Name;

                var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol as ITypeSymbol;
                if (symbol?.TypeKind != TypeKind.Interface)
                {
                    return;
                }

                var members = symbol.GetMembers();

                // We're going to create a entry for each one, including the signature
                var namePosition = name.SpanStart;

                var text = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

                foreach (var member in members)
                {
                    if (member.IsAccessor() ||
                        member.Kind == SymbolKind.NamedType ||
                        !(member.IsAbstract || member.IsVirtual) ||
                        !semanticModel.IsAccessible(node.SpanStart, member))
                    {
                        continue;
                    }

                    var memberString = member.ToMinimalDisplayString(semanticModel, namePosition, s_signatureDisplayFormat);

                    // Split the member string into two parts (generally the name, and the signature portion). We want
                    // the split so that other features (like spell-checking), only look at the name portion.
                    var (displayText, displayTextSuffix) = SplitMemberName(memberString);

                    var item = SymbolCompletionItem.CreateWithSymbolId(
                        displayText,
                        displayTextSuffix,
                        insertionText: memberString,
                        symbols: ImmutableArray.Create(member),
                        contextPosition: position,
                        rules: CompletionItemRules.Default);
                    item = item.AddProperty(InsertionTextOnOpenParen, member.Name);

                    context.AddItem(item);
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }

        private static (string text, string suffix) SplitMemberName(string memberString)
        {
            for (var i = 0; i < memberString.Length; i++)
            {
                if (!SyntaxFacts.IsIdentifierPartCharacter(memberString[i]))
                    return (memberString[0..i], memberString[i..]);
            }

            return (memberString, "");
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        public override Task<TextChange?> GetTextChangeAsync(
            Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            var insertionText = ch == '(' && selectedItem.Properties.TryGetValue(InsertionTextOnOpenParen, out var value)
                ? value
                : SymbolCompletionItem.GetInsertionText(selectedItem);

            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }
    }
}

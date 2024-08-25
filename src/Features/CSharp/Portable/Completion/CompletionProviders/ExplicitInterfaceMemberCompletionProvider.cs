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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ExplicitInterfaceMemberCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(UnnamedSymbolCompletionProvider))]
internal partial class ExplicitInterfaceMemberCompletionProvider : LSPCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExplicitInterfaceMemberCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => text[characterPosition] == '.';

    public override ImmutableHashSet<char> TriggerCharacters { get; } = ['.'];

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
                syntaxFacts.IsPreProcessorDirectiveContext(syntaxTree, position, cancellationToken))
            {
                return;
            }

            var targetToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                        .GetPreviousTokenIfTouchingWord(position);

            if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken))
                return;

            var node = targetToken.Parent;
            if (node is not ExplicitInterfaceSpecifierSyntax specifierNode)
                return;

            // Bind the interface name which is to the left of the dot
            var name = specifierNode.Name;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol as ITypeSymbol;
            if (symbol?.TypeKind != TypeKind.Interface)
                return;

            // We're going to create a entry for each one, including the signature
            var namePosition = name.SpanStart;
            foreach (var member in symbol.GetMembers())
            {
                if (!member.IsAbstract && !member.IsVirtual)
                    continue;

                if (member.IsAccessor() ||
                    member.Kind == SymbolKind.NamedType ||
                    !semanticModel.IsAccessible(node.SpanStart, member))
                {
                    continue;
                }

                var memberString = CompletionSymbolDisplay.ToDisplayString(member);

                // Split the member string into two parts (generally the name, and the signature portion). We want
                // the split so that other features (like spell-checking), only look at the name portion.
                var (displayText, displayTextSuffix) = SplitMemberName(memberString);

                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText,
                    displayTextSuffix,
                    insertionText: memberString,
                    symbols: ImmutableArray.Create<ISymbol>(member),
                    contextPosition: position,
                    rules: CompletionItemRules.Default));
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
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

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);

    public override Task<TextChange?> GetTextChangeAsync(
        Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
    {
        // If the user is typing a punctuation portion of the signature, then just emit the name.  i.e. if the
        // member is `Contains<T>(string key)`, then typing `<` should just emit `Contains` and not
        // `Contains<T>(string key)<`
        return Task.FromResult<TextChange?>(new TextChange(
            selectedItem.Span,
            ch is '(' or '[' or '<'
                ? selectedItem.DisplayText
                : SymbolCompletionItem.GetInsertionText(selectedItem)));
    }
}

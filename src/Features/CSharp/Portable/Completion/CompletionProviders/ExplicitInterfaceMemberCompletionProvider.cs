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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ImplementInterface;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(ExplicitInterfaceMemberCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(UnnamedSymbolCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ExplicitInterfaceMemberCompletionProvider() : AbstractMemberInsertingCompletionProvider
{
    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => text[characterPosition] == '.';

    public override ImmutableHashSet<char> TriggerCharacters { get; } = ['.'];

    protected override async Task<ISymbol> GenerateMemberAsync(
        Document newDocument,
        CompletionItem completionItem,
        Compilation compilation,
        ISymbol member,
        INamedTypeSymbol newContainingType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var implementInterfaceService = newDocument.GetRequiredLanguageService<IImplementInterfaceService>();

        var baseMemberInterfaceType = member.ContainingType;
        Contract.ThrowIfFalse(baseMemberInterfaceType.TypeKind is TypeKind.Interface);

        var interfaceNode = await GetInterfaceNodeInCompletionAsync(newDocument, completionItem, baseMemberInterfaceType, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(interfaceNode);

        var state = await implementInterfaceService.AnalyzeAsync(newDocument, interfaceNode, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(state);

        var options = await newDocument.GetImplementTypeOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Implement this member explicitly in the implementing type, and return the resultant member to actually
        // generate into the right declaration location.
        return implementInterfaceService.ImplementInterfaceMember(
            newDocument, state, member, options, new() { Explicitly = true }).ConfigureAwait(false);
    }

    protected override SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken)
    {
        // Common implementation with override and partial completion providers
        var tokenSpanEnd = MemberInsertionCompletionItem.GetTokenSpanEnd(completionItem);
        return tree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
    }

    protected override SyntaxNode GetSyntax(SyntaxToken token)
    {
        var ancestor = token.Parent;
        while (ancestor is not null)
        {
            var kind = ancestor.Kind();
            switch (kind)
            {
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.MethodDeclaration:
                    return ancestor;
            }

            ancestor = ancestor.Parent;
        }

        throw ExceptionUtilities.UnexpectedValue(token);
    }

    protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
    {
        return CompletionUtilities.GetTargetCaretNodeForInsertedMember(caretTarget).GetLocation().SourceSpan.End;
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var state = await ItemGetter.CreateAsync(this, context.Document, context.Position, context.CancellationToken).ConfigureAwait(false);
        var items = await state.GetItemsAsync().ConfigureAwait(false);

        if (!items.IsDefaultOrEmpty)
        {
            context.IsExclusive = true;
            context.AddItems(items);
        }
    }

    private static (string text, string suffix) SplitMemberName(string memberString)
    {
        for (var i = 0; i < memberString.Length; i++)
        {
            if (memberString[i] is '(' or '[' or '<')
                return (memberString[0..i], memberString[i..]);
        }

        return (memberString, "");
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);
}

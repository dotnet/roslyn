// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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

    protected override async Task<ISymbol> GenerateMemberAsync(ISymbol member, INamedTypeSymbol implementingType, Document newDocument, CompletionItem completionItem, CancellationToken cancellationToken)
    {
        var implementInterfaceService = (AbstractImplementInterfaceService)newDocument.GetRequiredLanguageService<IImplementInterfaceService>();

        var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(semanticModel != null);

        var baseMemberInterfaceType = member.ContainingType;
        Debug.Assert(baseMemberInterfaceType.TypeKind is TypeKind.Interface);

        var interfaceNode = await GetInterfaceNodeInCompletionAsync(newDocument, completionItem, baseMemberInterfaceType, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(interfaceNode != null);

        var state = AbstractImplementInterfaceService.State.Generate(implementInterfaceService, newDocument, semanticModel, interfaceNode, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(state != null);

        var options = await newDocument.GetImplementTypeOptionsAsync(cancellationToken).ConfigureAwait(false);
        var implementedSymbol = await implementInterfaceService.ExplicitlyImplementSingleInterfaceMemberAsync(
            newDocument, state, member, options, cancellationToken)
                .ConfigureAwait(false);

        return implementedSymbol;
    }

    private static async Task<SyntaxNode?> GetInterfaceNodeInCompletionAsync(Document document, CompletionItem item, INamedTypeSymbol baseMemberInterfaceType, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var documentSemanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        Debug.Assert(documentSemanticModel != null, "Expected a semantic model out of the document");

        var compilation = documentSemanticModel.Compilation;
        var node = syntaxTree.FindNode(item.Span, false, true, cancellationToken);
        var primaryTypeDeclaration = node.GetAncestor<BaseTypeDeclarationSyntax>();
        Debug.Assert(primaryTypeDeclaration != null, "Expected a BaseTypeDeclarationSyntax to contain the implemented interface member");
        var interfaceNode = NodeInDeclaration(primaryTypeDeclaration);
        if (interfaceNode != null)
        {
            return interfaceNode;
        }

        var declaringSyntaxReferences = baseMemberInterfaceType.DeclaringSyntaxReferences;
        foreach (var declaring in declaringSyntaxReferences)
        {
            var declaringSyntax = await declaring.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (declaringSyntax == null)
            {
                continue;
            }

            // We have already evaluated the primary type declaration
            if (declaringSyntax == primaryTypeDeclaration)
                continue;

            if (declaringSyntax is not BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
            {
                continue;
            }

            interfaceNode = NodeInDeclaration(baseTypeDeclarationSyntax);
            if (interfaceNode != null)
                return interfaceNode;
        }

        return null;

        SyntaxNode? NodeInDeclaration(BaseTypeDeclarationSyntax declaration)
        {
            var tree = declaration.SyntaxTree;
            var baseList = declaration.BaseList;
            if (baseList is null)
                return null;

            var semanticModel = compilation.GetSemanticModel(tree)!;

            foreach (var baseType in baseList.Types)
            {
                var typeSyntax = baseType.Type;
                var typeInfo = semanticModel.GetSymbolInfo(typeSyntax, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    return null;

                var type = typeInfo.Symbol as INamedTypeSymbol;
                if (typeInfo.CandidateReason == CandidateReason.WrongArity)
                {
                    type = typeInfo.GetAnySymbol() as INamedTypeSymbol;
                }

                if (type is null)
                    continue;

                if (type.Equals(baseMemberInterfaceType, SymbolEqualityComparer.Default))
                    return typeSyntax;
            }

            return null;
        }
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
        return CompletionUtilities.GetTargetCaretPositionForInsertedMember(caretTarget);
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
            if (IsSplitterChar(memberString[i]))
                return (memberString[0..i], memberString[i..]);
        }

        return (memberString, "");

        static bool IsSplitterChar(char c)
        {
            return c is '(' or '[' or '<';
        }
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => SymbolCompletionItem.GetDescriptionAsync(item, document, displayOptions, cancellationToken);
}

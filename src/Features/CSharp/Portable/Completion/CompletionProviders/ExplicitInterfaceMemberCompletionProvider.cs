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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        // Common implementation with override and partial completion providers
        return token.GetAncestor<EventFieldDeclarationSyntax>()
            ?? token.GetAncestor<EventDeclarationSyntax>()
            ?? token.GetAncestor<PropertyDeclarationSyntax>()
            ?? token.GetAncestor<IndexerDeclarationSyntax>()
            ?? (SyntaxNode?)token.GetAncestor<MethodDeclarationSyntax>()
            ?? throw ExceptionUtilities.UnexpectedValue(token);
    }

    protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
    {
        return CompletionUtilities.GetTargetCaretPositionForInsertedMember(caretTarget);
    }

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
            // Bind the interface name which is to the left of the dot
            NameSyntax? name = null;
            switch (node)
            {
                case ExplicitInterfaceSpecifierSyntax specifierNode:
                    name = specifierNode.Name;
                    break;

                case QualifiedNameSyntax qualifiedName
                when node.Parent.IsKind(SyntaxKind.IncompleteMember):
                    name = qualifiedName.Left;
                    break;

                default:
                    return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol as ITypeSymbol;
            if (symbol?.TypeKind != TypeKind.Interface)
                return;

            // We're going to create a entry for each one, including the signature
            var namePosition = name.SpanStart;
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            text.GetLineAndOffset(namePosition, out var line, out var lineOffset);
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

                context.AddItem(MemberInsertionCompletionItem.Create(
                    displayText, displayTextSuffix, DeclarationModifiers.None, line,
                    member, targetToken, position,
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

/// <summary>
/// Implements SignatureHelp and ParameterInfo for <see cref="PrimaryConstructorBaseTypeSyntax"/>
/// such as 'record Student(int Id) : Person($$"first", "last");`.
/// </summary>
[ExportSignatureHelpProvider("PrimaryConstructorBaseTypeSignatureHelpProvider", LanguageNames.CSharp), Shared]
internal partial class PrimaryConstructorBaseTypeSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PrimaryConstructorBaseTypeSignatureHelpProvider()
    {
    }

    public override bool IsTriggerCharacter(char ch)
        => ch is '(' or ',';

    public override bool IsRetriggerCharacter(char ch)
        => ch == ')';

    private bool TryGetBaseTypeSyntax(
        SyntaxNode root,
        int position,
        ISyntaxFactsService syntaxFacts,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out PrimaryConstructorBaseTypeSyntax? expression)
    {
        if (!CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out expression))
        {
            return false;
        }

        return expression.ArgumentList != null;

        static bool IsArgumentListToken(PrimaryConstructorBaseTypeSyntax expression, SyntaxToken token)
        {
            return expression.ArgumentList != null &&
                expression.ArgumentList.Span.Contains(token.SpanStart) &&
                token != expression.ArgumentList.CloseParenToken;
        }
    }

    private bool IsTriggerToken(SyntaxToken token)
        => SignatureHelpUtilities.IsTriggerParenOrComma<PrimaryConstructorBaseTypeSyntax>(token, IsTriggerCharacter);

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (!TryGetBaseTypeSyntax(root, position, syntaxFacts, triggerInfo.TriggerReason, cancellationToken, out var baseTypeSyntax))
            return null;

        var baseList = baseTypeSyntax.Parent as BaseListSyntax;
        var namedTypeSyntax = baseList?.Parent as BaseTypeDeclarationSyntax;
        if (namedTypeSyntax is null)
            return null;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var within = semanticModel.GetRequiredDeclaredSymbol(namedTypeSyntax, cancellationToken);
        if (within is null)
            return null;

        if (semanticModel.GetTypeInfo(baseTypeSyntax.Type, cancellationToken).Type is not INamedTypeSymbol baseType)
            return null;

        var accessibleConstructors = baseType.InstanceConstructors
            .WhereAsArray(c => c.IsAccessibleWithin(within))
            .WhereAsArray(c => c.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
            .Sort(semanticModel, baseTypeSyntax.SpanStart);

        if (!accessibleConstructors.Any())
            return null;

        var structuralTypeDisplayService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(baseTypeSyntax.ArgumentList);
        var currentConstructor = semanticModel.GetSymbolInfo(baseTypeSyntax, cancellationToken).Symbol;
        var selectedItem = TryGetSelectedIndex(accessibleConstructors, currentConstructor);

        return CreateSignatureHelpItems(accessibleConstructors.SelectAsArray(c =>
            Convert(c, baseTypeSyntax.ArgumentList.OpenParenToken, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)).ToList(),
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken), selectedItem, parameterIndexOverride: -1);
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetBaseTypeSyntax(root, position, syntaxFacts, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken, out var expression) &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
        }

        return null;
    }

    private static SignatureHelpItem Convert(
        IMethodSymbol constructor,
        SyntaxToken openToken,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var position = openToken.SpanStart;
        var item = CreateItem(
            constructor, semanticModel, position,
            structuralTypeDisplayService,
            constructor.IsParams(),
            constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetPreambleParts(constructor, semanticModel, position),
            GetSeparatorParts(),
            GetPostambleParts(),
            constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService)).ToList());
        return item;

        static IList<SymbolDisplayPart> GetPreambleParts(
            IMethodSymbol method,
            SemanticModel semanticModel,
            int position)
        {
            var result = new List<SymbolDisplayPart>();

            result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
            result.Add(Punctuation(SyntaxKind.OpenParenToken));

            return result;
        }

        static IList<SymbolDisplayPart> GetPostambleParts()
        {
            return SpecializedCollections.SingletonList(Punctuation(SyntaxKind.CloseParenToken));
        }
    }
}

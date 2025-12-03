// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportSignatureHelpProvider(nameof(GenericNameFullyWrittenSignatureHelpProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class GenericNameFullyWrittenSignatureHelpProvider() : AbstractGenericNameSignatureHelpProvider
{
    protected override bool TryGetGenericIdentifier(
        SyntaxNode root, int position,
        ISyntaxFactsService syntaxFacts,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken,
        out SyntaxToken genericIdentifier,
        out SyntaxToken lessThanToken)
    {
        if (CommonSignatureHelpUtilities.TryGetSyntax(
                root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out GenericNameSyntax? name))
        {
            genericIdentifier = name.Identifier;
            lessThanToken = name.TypeArgumentList.LessThanToken;
            return true;
        }

        genericIdentifier = default;
        lessThanToken = default;
        return false;
    }

    private bool IsTriggerToken(SyntaxToken token)
    {
        return !token.IsKind(SyntaxKind.None) &&
            token.ValueText.Length == 1 &&
            TriggerCharacters.Contains(token.ValueText[0]) &&
            token.Parent is TypeArgumentListSyntax &&
            token.Parent.Parent is GenericNameSyntax;
    }

    private bool IsArgumentListToken(GenericNameSyntax node, SyntaxToken token)
    {
        return node.TypeArgumentList != null &&
            node.TypeArgumentList.Span.Contains(token.SpanStart) &&
            token != node.TypeArgumentList.GreaterThanToken;
    }

    protected override TextSpan GetTextSpan(SyntaxToken genericIdentifier, SyntaxToken lessThanToken)
    {
        Contract.ThrowIfFalse(lessThanToken.Parent is TypeArgumentListSyntax && lessThanToken.Parent.Parent is GenericNameSyntax);
        return SignatureHelpUtilities.GetSignatureHelpSpan(((GenericNameSyntax)lessThanToken.Parent.Parent).TypeArgumentList);
    }
}

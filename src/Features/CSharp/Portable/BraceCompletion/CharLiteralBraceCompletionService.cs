// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion;

[ExportBraceCompletionService(LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class CharLiteralBraceCompletionService() : AbstractCSharpBraceCompletionService
{
    protected override char OpeningBrace => SingleQuote.OpenCharacter;

    protected override char ClosingBrace => SingleQuote.CloseCharacter;

    public override bool AllowOverType(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
        => AllowOverTypeWithValidClosingToken(braceCompletionContext);

    protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CharacterLiteralToken);

    protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CharacterLiteralToken);
}

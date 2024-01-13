﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{

    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class CharLiteralBraceCompletionService : AbstractCSharpBraceCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CharLiteralBraceCompletionService()
        {
        }

        protected override char OpeningBrace => SingleQuote.OpenCharacter;

        protected override char ClosingBrace => SingleQuote.CloseCharacter;

        public override bool AllowOverType(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
            => AllowOverTypeWithValidClosingToken(braceCompletionContext);

        protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CharacterLiteralToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CharacterLiteralToken);
    }
}

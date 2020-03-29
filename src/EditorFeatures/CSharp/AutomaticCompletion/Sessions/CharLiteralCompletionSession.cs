// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion.Sessions
{
    internal class CharLiteralCompletionSession : AbstractTokenBraceCompletionSession
    {
        public CharLiteralCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.CharacterLiteralToken, (int)SyntaxKind.CharacterLiteralToken)
        {
        }

        public override bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken)
            => true;
    }
}

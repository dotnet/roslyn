// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return true;
        }
    }
}

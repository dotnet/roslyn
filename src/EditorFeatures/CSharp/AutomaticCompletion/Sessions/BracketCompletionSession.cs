// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.CSharp.AutomaticCompletion.Sessions
{
    internal class BracketCompletionSession : AbstractTokenBraceCompletionSession
    {
        public BracketCompletionSession(ISyntaxFactsService syntaxFactsService)
            : base(syntaxFactsService, (int)SyntaxKind.OpenBracketToken, (int)SyntaxKind.CloseBracketToken)
        {
        }
    }
}

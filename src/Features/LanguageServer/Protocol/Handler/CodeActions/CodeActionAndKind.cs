// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.VisualStudio.LanguageServer.Protocol;
using CodeAction = Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    internal class CodeActionAndKind
    {
        public CodeAction CodeAction { get; }

        public CodeActionKind Kind { get; }

        public CodeActionAndKind(CodeAction codeAction, CodeActionKind kind)
        {
            CodeAction = codeAction;
            Kind = kind;
        }
    }
}

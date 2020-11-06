// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal interface IEditorBraceCompletionSession : ILanguageService
    {
        bool IsValidForBraceCompletion(char brace, int openingPosition, Document document, CancellationToken cancellationToken);
        BraceCompletionResult? GetBraceCompletion(IBraceCompletionSession session, CancellationToken cancellationToken);
        BraceCompletionResult? GetChangesAfterCompletion(IBraceCompletionSession session, CancellationToken cancellationToken);
        bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken);
        BraceCompletionResult? GetChangesAfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken);
    }
}

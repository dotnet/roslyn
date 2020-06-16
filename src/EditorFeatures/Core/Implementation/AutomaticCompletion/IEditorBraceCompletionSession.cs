// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text.BraceCompletion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal interface IEditorBraceCompletionSession : ILanguageService
    {
        bool CheckOpeningPoint(IBraceCompletionSession session, CancellationToken cancellationToken);
        void AfterStart(IBraceCompletionSession session, CancellationToken cancellationToken);
        bool AllowOverType(IBraceCompletionSession session, CancellationToken cancellationToken);
        void AfterReturn(IBraceCompletionSession session, CancellationToken cancellationToken);
    }
}

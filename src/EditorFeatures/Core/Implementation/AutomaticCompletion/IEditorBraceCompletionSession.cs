// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

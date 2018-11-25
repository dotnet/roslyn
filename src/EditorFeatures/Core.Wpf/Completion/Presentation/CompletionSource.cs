// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal sealed class CompletionSource : ForegroundThreadAffinitizedObject, ICompletionSource
    {
        public CompletionSource(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        void ICompletionSource.AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            AssertIsForeground();
            if (!session.Properties.TryGetProperty<CompletionPresenterSession>(CompletionPresenterSession.Key, out var presenterSession))
            {
                return;
            }

            session.Properties.RemoveProperty(CompletionPresenterSession.Key);
            presenterSession.AugmentCompletionSession(completionSets);
        }

        void IDisposable.Dispose()
        {
        }
    }
}

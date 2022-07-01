// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract class AsynchronousTaggerProvider<TTag> : AbstractAsynchronousTaggerProvider<TTag>, ITaggerProvider
        where TTag : ITag
    {
        protected AsynchronousTaggerProvider(
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext, globalOptions, visibilityTracker, asyncListener)
        {
        }

        public ITagger<T>? CreateTagger<T>(ITextBuffer subjectBuffer) where T : ITag
        {
            if (subjectBuffer == null)
                throw new ArgumentNullException(nameof(subjectBuffer));

            return this.CreateTaggerWorker<T>(null, subjectBuffer);
        }

        ITagger<T>? ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
            => CreateTagger<T>(buffer);
    }
}

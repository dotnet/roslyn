﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal abstract class AsynchronousTaggerProvider<TTag> : AbstractAsynchronousTaggerProvider<TTag>, ITaggerProvider
        where TTag : ITag
    {
        protected AsynchronousTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
                : base(threadingContext, asyncListener, notificationService)
        {
        }

        public IAccurateTagger<T> CreateTagger<T>(ITextBuffer subjectBuffer) where T : ITag
        {
            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            return this.CreateTaggerWorker<T>(null, subjectBuffer);
        }

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
            => CreateTagger<T>(buffer);
    }
}

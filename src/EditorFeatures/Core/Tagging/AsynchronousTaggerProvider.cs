// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Computes <see cref="ITag"/>s for a text buffer in an asynchronous manner.  Computation of
    /// the tags is handled by a provided <see cref="IAsynchronousTaggerDataSource{TTag}"/>.  The
    /// <see cref="AsynchronousTaggerProvider{TTag}"/> handles the jobs of scheduling when to 
    /// compute tags, managing the collection of tags, as well as notifying and keeping the user 
    /// interface up to date with the latest tags produced.
    /// </summary>
    // Note: it might seem desirable to avoid having this class and just expose
    // AsynchronousBufferTaggerProvider and AsynchronousViewTaggerProvider.  However, those types
    // expose a lot of complexity through their protected surface area.  For example, all the code
    // to handle and manipulate complex tag sources.  For taggers that have no need to deal with 
    // that (including 3rd party taggers), this class serves as a much nicer tagging entrypoint.
    internal class AsynchronousTaggerProvider<TTag> : ITaggerProvider, IViewTaggerProvider
        where TTag : ITag
    {
        private readonly AsynchronousBufferTaggerProvider<TTag> _bufferTaggerImplementation;
        private readonly AsynchronousViewTaggerProvider<TTag> _viewTaggerImplementation;

        /// <summary>
        /// Creates a new <see cref="AsynchronousTaggerProvider{TTag}"/> using the provided 
        /// <see cref="IAsynchronousTaggerDataSource{TTag}"/> to determine when to compute tags 
        /// and to produce tags when appropriate.
        /// </summary>
        internal AsynchronousTaggerProvider(
            IAsynchronousTaggerDataSource<TTag> dataSource,
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException("dataSource");
            }

            _bufferTaggerImplementation = new AsynchronousBufferTaggerProvider<TTag>(dataSource, asyncListener, notificationService, createTagSource: null);
            _viewTaggerImplementation = new AsynchronousViewTaggerProvider<TTag>(dataSource, asyncListener, notificationService, createTagSource: null);
        }

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
        {
            return _bufferTaggerImplementation.CreateTagger<T>(buffer);
        }

        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        {
            return _viewTaggerImplementation.CreateTagger<T>(textView, buffer);
        }
    }
}
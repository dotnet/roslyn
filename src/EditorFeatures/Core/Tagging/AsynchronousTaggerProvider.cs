// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    /// compute tags, managing the collection of tags, and notifying the and keeping the user 
    /// interface up to date with the latest tags produced.
    /// </summary>
    internal class AsynchronousTaggerProvider<TTag> : ITaggerProvider
        where TTag : ITag
    {
        private readonly AsynchronousTaggerProviderImpl _taggerImplementation;

        /// <summary>
        /// Creates a new <see cref="AsynchronousTaggerProvider{TTag}"/> using the provided 
        /// <see cref="IAsynchronousTaggerDataSource{TTag}"/> to determine when to compute tags 
        /// and to produce tags when appropriate.
        /// </summary>
        internal AsynchronousTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService,
            IAsynchronousTaggerDataSource<TTag> dataSource)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            _taggerImplementation = new AsynchronousTaggerProviderImpl(asyncListener, notificationService, dataSource);
        }

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
        {
            return _taggerImplementation.CreateTagger<T>(buffer);
        }

        private class AsynchronousTaggerProviderImpl : AbstractAsynchronousBufferTaggerProvider<TTag>
        {
            private readonly IAsynchronousTaggerDataSource<TTag> _dataSource;

            public AsynchronousTaggerProviderImpl(
                IAsynchronousOperationListener asyncListener,
                IForegroundNotificationService notificationService,
                IAsynchronousTaggerDataSource<TTag> dataSource)
                : base(asyncListener, notificationService)
            {
                _dataSource = dataSource;
            }

            protected override bool RemoveTagsThatIntersectEdits => _dataSource.RemoveTagsThatIntersectEdits;

            protected override SpanTrackingMode SpanTrackingMode => _dataSource.SpanTrackingMode;

            protected override TaggerDelay UIUpdateDelay => _dataSource.UIUpdateDelay;

            protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
            {
                return _dataSource.CreateEventSource(textViewOpt, subjectBuffer);
            }

            protected override ITagProducer<TTag> CreateTagProducer()
            {
                return _dataSource.CreateTagProducer();
            }
        }
    }
}

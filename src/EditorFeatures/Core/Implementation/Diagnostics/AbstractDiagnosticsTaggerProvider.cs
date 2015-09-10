// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    /// <summary>
    /// Diagnostics works slightly differently than the rest of the taggers.  For diagnostics,
    /// we want to try to have an individual tagger per diagnostic producer per buffer.  
    /// However, the editor only allows a single tagger provider per buffer.  So in order to
    /// get the abstraction we want, we create one outer tagger provider that is associated
    /// with the buffer.  Then, under the covers, we create individual async taggers for each
    /// diagnostic producer we hear about for that buffer.   
    /// 
    /// In essence, we have one tagger that wraps a multitude of taggers it delegates to.
    /// Each of these taggers is nicely asynchronous and properly works within the async
    /// tagging infrastructure. 
    /// </summary>
    internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag> :
        ForegroundThreadAffinitizedObject,
        ITaggerProvider
        where TTag : ITag
    {
        private readonly object _uniqueKey = new object();
        private readonly IDiagnosticService _diagnosticService;
        private readonly IForegroundNotificationService _notificationService;
        private readonly IAsynchronousOperationListener _listener;

        protected AbstractDiagnosticsTaggerProvider(
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListener listener)
        {
            _diagnosticService = diagnosticService;
            _notificationService = notificationService;
            _listener = listener;
        }

        protected internal abstract IEnumerable<Option<bool>> Options { get; }
        protected internal abstract bool IsEnabled { get; }
        protected internal abstract bool IncludeDiagnostic(DiagnosticData data);
        protected internal abstract ITagSpan<TTag> CreateTagSpan(bool isLiveUpdate, SnapshotSpan span, DiagnosticData data);

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
        {
            return CreateTagger<T>(buffer);
        }

        public IAccurateTagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagger = buffer.Properties.GetOrCreateSingletonProperty(
                _uniqueKey, () => new AggregatingTagger(this, buffer));
            tagger.OnTaggerCreated();
            return tagger as IAccurateTagger<T>;
        }

        private void RemoveTagger(AggregatingTagger tagger, ITextBuffer buffer)
        {
            buffer.Properties.RemoveProperty(_uniqueKey);
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract class AbstractDiagnosticsTaggerProvider<TTag> :
        AbstractAsynchronousTaggerProvider<AbstractAggregatedDiagnosticsTagSource<TTag>, TTag>, ITaggerProvider
        where TTag : ITag
    {
        protected readonly DiagnosticService DiagnosticService;

        public AbstractDiagnosticsTaggerProvider(
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListener listener)
            : base(listener, notificationService)
        {
            this.DiagnosticService = diagnosticService as DiagnosticService;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer subjectBuffer) where T : ITag
        {
            if (subjectBuffer == null)
            {
                throw new ArgumentNullException("subjectBuffer");
            }

            return this.GetOrCreateTagger<T>(null, subjectBuffer);
        }

        protected sealed override bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out AbstractAggregatedDiagnosticsTagSource<TTag> tagSource)
        {
            return subjectBuffer.Properties.TryGetProperty(UniqueKey, out tagSource);
        }

        protected sealed override void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, AbstractAggregatedDiagnosticsTagSource<TTag> tagSource)
        {
            subjectBuffer.Properties.AddProperty(UniqueKey, tagSource);
        }

        protected sealed override void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            subjectBuffer.Properties.RemoveProperty(UniqueKey);
        }
    }
}

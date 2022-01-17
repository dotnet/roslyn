// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    [Export(typeof(IPasteTrackingService))]
    [Shared]
    internal sealed class RemotePasteTrackingService : IPasteTrackingService
    {
        private readonly Dictionary<SourceTextContainer, (TextSpan? span, int referenceCount)> _trackedSpans = new Dictionary<SourceTextContainer, (TextSpan? span, int referenceCount)>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemotePasteTrackingService()
        {
        }

        public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
        {
            lock (_trackedSpans)
            {
                var result = _trackedSpans.TryGetValue(sourceTextContainer, out var spanAndCount);
                if (!result || spanAndCount.referenceCount == 0 || spanAndCount.span is null)
                {
                    textSpan = default;
                    return false;
                }

                textSpan = spanAndCount.span.Value;
                return result;
            }
        }

        private void AddPastedTextSpanReference(SourceTextContainer container, TextSpan? pastedTextSpan)
        {
            lock (_trackedSpans)
            {
                if (!_trackedSpans.TryGetValue(container, out var spanAndCount))
                {
                    _trackedSpans[container] = (pastedTextSpan, 1);
                    return;
                }

                if (spanAndCount.span != pastedTextSpan)
                {
                    // Cannot track two different pasted spans for the same container
                    throw ExceptionUtilities.UnexpectedValue(pastedTextSpan);
                }

                Debug.Assert(spanAndCount.referenceCount > 0);
                _trackedSpans[container] = (spanAndCount.span, spanAndCount.referenceCount + 1);
            }
        }

        private void ReleasePastedTextSpanReference(SourceTextContainer container)
        {
            lock (_trackedSpans)
            {
                if (!_trackedSpans.TryGetValue(container, out var spanAndCount))
                    throw ExceptionUtilities.UnexpectedValue(container);

                Debug.Assert(spanAndCount.referenceCount > 0);
                if (spanAndCount.referenceCount <= 1)
                    _trackedSpans.Remove(container);
                else
                    _trackedSpans[container] = (spanAndCount.span, spanAndCount.referenceCount - 1);
            }
        }

        internal Releaser SetPastedTextSpanForRemoteCall(SourceTextContainer container, TextSpan? pastedTextSpan)
        {
            AddPastedTextSpanReference(container, pastedTextSpan);
            return new Releaser(this, container);
        }

        internal readonly struct Releaser : IDisposable
        {
            private readonly RemotePasteTrackingService _service;
            private readonly SourceTextContainer _container;

            public Releaser(RemotePasteTrackingService service, SourceTextContainer container)
            {
                _service = service;
                _container = container;
            }

            public void Dispose()
            {
                _service.ReleasePastedTextSpanReference(_container);
            }
        }
    }
}

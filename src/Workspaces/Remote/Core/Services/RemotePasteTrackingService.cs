// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    [Export(typeof(IPasteTrackingService))]
    [Shared]
    internal sealed class RemotePasteTrackingService : IPasteTrackingService
    {
        private readonly Dictionary<SourceTextContainer, TextSpan> _trackedSpans = new Dictionary<SourceTextContainer, TextSpan>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemotePasteTrackingService()
        {
        }

        public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
        {
            lock (_trackedSpans)
            {
                return _trackedSpans.TryGetValue(sourceTextContainer, out textSpan);
            }
        }

        internal void ClearPastedTextSpan(SourceTextContainer container)
        {
            lock (_trackedSpans)
            {
                _trackedSpans.Remove(container);
            }
        }

        internal void SetPastedTextSpan(SourceTextContainer container, TextSpan? pastedTextSpan)
        {
            lock (_trackedSpans)
            {
                if (pastedTextSpan is object)
                {
                    _trackedSpans[container] = pastedTextSpan.Value;
                }
                else
                {
                    _trackedSpans.Remove(container);
                }
            }
        }
    }
}

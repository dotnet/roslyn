// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
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
        private static ReferenceCountedDisposable<Callback>.WeakReference s_weakCallback;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemotePasteTrackingService()
        {
        }

        public static IReferenceCountedDisposable<IDisposable> RegisterCallback(IPasteTrackingService implementation)
        {
            var callback = new ReferenceCountedDisposable<Callback>(new Callback(implementation));
            s_weakCallback = new ReferenceCountedDisposable<Callback>.WeakReference(callback);
            return callback;
        }

        public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
        {
            using var callback = s_weakCallback.TryAddReference();
            if (callback is object)
            {
                return callback.Target.TryGetPastedTextSpan(sourceTextContainer, out textSpan);
            }

            textSpan = default;
            return false;
        }

        private sealed class Callback : IPasteTrackingService, IDisposable
        {
            private readonly IPasteTrackingService _implementation;

            public Callback(IPasteTrackingService implementation)
            {
                _implementation = implementation;
            }

            public bool TryGetPastedTextSpan(SourceTextContainer sourceTextContainer, out TextSpan textSpan)
                => _implementation.TryGetPastedTextSpan(sourceTextContainer, out textSpan);

            public void Dispose()
                => s_weakCallback = default;
        }
    }
}

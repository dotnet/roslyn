// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal partial class TaggerEventSources
{
    private class CompositionEventSource : ITaggerEventSource
    {
        private readonly ITaggerEventSource[] _providers;

        public CompositionEventSource(ITaggerEventSource[] providers)
        {
            Contract.ThrowIfNull(providers);

            _providers = providers;
        }

        public void Connect()
            => _providers.Do(p => p.Connect());

        public void Disconnect()
            => _providers.Do(p => p.Disconnect());

        public void Pause()
            => _providers.Do(p => p.Pause());

        public void Resume()
            => _providers.Do(p => p.Resume());

        public event EventHandler<TaggerEventArgs> Changed
        {
            add => _providers.Do(p => p.Changed += value);
            remove => _providers.Do(p => p.Changed -= value);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
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
            {
                _providers.Do(p => p.Connect());
            }

            public void Disconnect()
            {
                _providers.Do(p => p.Disconnect());
            }

            public event EventHandler<TaggerEventArgs> Changed
            {
                add
                {
                    _providers.Do(p => p.Changed += value);
                }

                remove
                {
                    _providers.Do(p => p.Changed -= value);
                }
            }

            public event EventHandler UIUpdatesPaused
            {
                add
                {
                    _providers.Do(p => p.UIUpdatesPaused += value);
                }

                remove
                {
                    _providers.Do(p => p.UIUpdatesPaused -= value);
                }
            }

            public event EventHandler UIUpdatesResumed
            {
                add
                {
                    _providers.Do(p => p.UIUpdatesResumed += value);
                }

                remove
                {
                    _providers.Do(p => p.UIUpdatesResumed -= value);
                }
            }
        }
    }
}

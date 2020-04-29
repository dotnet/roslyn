// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal abstract class AbstractTaggerEventSource : ITaggerEventSource
    {
        private readonly TaggerDelay _delay;

        protected AbstractTaggerEventSource(TaggerDelay delay)
            => _delay = delay;

        public abstract void Connect();
        public abstract void Disconnect();

        public event EventHandler<TaggerEventArgs> Changed;
        public event EventHandler UIUpdatesPaused;
        public event EventHandler UIUpdatesResumed;

        protected virtual void RaiseChanged()
            => this.Changed?.Invoke(this, new TaggerEventArgs(_delay));

        protected virtual void RaiseUIUpdatesPaused()
            => this.UIUpdatesPaused?.Invoke(this, EventArgs.Empty);

        protected virtual void RaiseUIUpdatesResumed()
            => this.UIUpdatesResumed?.Invoke(this, EventArgs.Empty);
    }
}

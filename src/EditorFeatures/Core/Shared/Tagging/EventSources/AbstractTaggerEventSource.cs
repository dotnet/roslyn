// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal abstract class AbstractTaggerEventSource : ITaggerEventSource
    {
        private readonly TaggerDelay _delay;

        protected AbstractTaggerEventSource(TaggerDelay delay)
        {
            _delay = delay;
        }

        public abstract void Connect();
        public abstract void Disconnect();

        public event EventHandler<TaggerEventArgs> Changed;
        public event EventHandler UIUpdatesPaused;
        public event EventHandler UIUpdatesResumed;

        protected virtual void RaiseChanged()
        {
            var changed = this.Changed;
            if (changed != null)
            {
                changed(this, new TaggerEventArgs(_delay));
            }
        }

        protected virtual void RaiseUIUpdatesPaused()
        {
            var uiUpdatesPaused = this.UIUpdatesPaused;
            if (uiUpdatesPaused != null)
            {
                uiUpdatesPaused(this, EventArgs.Empty);
            }
        }

        protected virtual void RaiseUIUpdatesResumed()
        {
            var uiUpdatesResumed = this.UIUpdatesResumed;
            if (uiUpdatesResumed != null)
            {
                uiUpdatesResumed(this, EventArgs.Empty);
            }
        }
    }
}

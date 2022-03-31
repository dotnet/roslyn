// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal abstract class AbstractTaggerEventSource : ITaggerEventSource
    {
        protected AbstractTaggerEventSource()
        {
        }

        public abstract void Connect();
        public abstract void Disconnect();

        public event EventHandler<TaggerEventArgs>? Changed;

        protected void RaiseChanged()
        {
            this.Changed?.Invoke(this, new TaggerEventArgs());
        }
    }
}

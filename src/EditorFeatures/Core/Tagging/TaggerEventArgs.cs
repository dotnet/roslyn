// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Information provided to the <see cref="AbstractAsynchronousTaggerProvider{TTag}"/> when 
    /// <see cref="ITaggerEventSource.Changed"/> fires.
    /// </summary>
    internal class TaggerEventArgs : EventArgs
    {
        /// <summary>
        /// They amount of time to wait before the <see cref="AbstractAsynchronousTaggerProvider{TTag}"/>
        /// checks for new tags and updates the user interface.
        /// </summary>
        public TaggerDelay Delay { get; }

        /// <summary>
        /// Creates a new <see cref="TaggerEventArgs"/>
        /// </summary>
        public TaggerEventArgs(TaggerDelay delay)
            => this.Delay = delay;
    }
}

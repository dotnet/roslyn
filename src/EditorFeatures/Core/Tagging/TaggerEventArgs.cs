// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            this.Delay = delay;
        }
    }
}
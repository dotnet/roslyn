// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// The events that the <see cref="AbstractAsynchronousTaggerProvider{TTag}"/> listens to, to know when 
    /// to request more tags.  For example, an <see cref="ITaggerEventSource"/> may listen to text 
    /// buffer changes, and can tell the <see cref="AbstractAsynchronousTaggerProvider{TTag}"/> that it needs
    /// to recompute tags.
    /// </summary>
    internal interface ITaggerEventSource
    {
        /// <summary>
        /// Let event source know that it should start sending out events.  Implementation can use
        /// that as a point to attach to events and perform other initialization. This will only be
        /// called once. 
        /// </summary>
        void Connect();

        /// <summary>
        /// Let event source know that it is no longer needed.  Implementations can use this as a
        /// point to detach from events and perform other cleanup.  This will only be called once.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Pauses this event source and prevents it from firing the <see cref="Changed"/> event. Can be called many
        /// times (but subsequence calls have no impact if already paused).  Must be called on the UI thread.
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes this event source and allows firing the <see cref="Changed"/> event. Can be called many times (but
        /// subsequence calls have no impact if already resumed).  Must be called on the UI thread.
        /// </summary>
        void Resume();

        /// <summary>
        /// An event has happened on the thing the tagger is attached to.  The tagger should
        /// recompute tags.
        /// </summary>
        event EventHandler<TaggerEventArgs> Changed;
    }
}

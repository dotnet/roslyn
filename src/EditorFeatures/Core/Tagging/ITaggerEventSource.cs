// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// An event has happened on the thing the tagger is attached to.  The tagger should
        /// recompute tags.
        /// </summary>
        event EventHandler<TaggerEventArgs> Changed;

        /// <summary>
        /// The tagger should stop updating the UI with the tags it's produced.
        /// </summary>
        event EventHandler UIUpdatesPaused;

        /// <summary>
        /// The tagger can start notifying the UI about its tags again.
        /// </summary>
        event EventHandler UIUpdatesResumed;
    }
}

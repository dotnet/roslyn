// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// How quickly the <see cref="AbstractAsynchronousTaggerProvider{TTag}"/> should update tags after
    /// receiving an <see cref="ITaggerEventSource.Changed"/> notification.
    /// </summary>
    internal enum TaggerDelay
    {
        /// <summary>
        /// Indicates that the tagger should retag after a short, but imperceptible delay.  This is
        /// for features that want to appear instantaneous to the user, but which can wait a short
        /// while until a batch of changes has occurred before processing.  Specifically, if a user
        /// expects the tag immediately after typing a character or moving the caret, then this
        /// delay should be used.
        /// </summary>
        NearImmediate,

        /// <summary>
        /// Not as fast as NearImmediate.  A user typing quickly or navigating quickly should not
        /// trigger this.  However, any sort of pause will cause it to trigger
        /// </summary>
        Short,

        /// <summary>
        /// Not as fast as 'Short'. The user's pause should be more significant until the tag
        /// appears.
        /// </summary>
        Medium,

        /// <summary>
        /// Indicates that the tagger should run when the user appears to be idle.  
        /// </summary>
        OnIdle,

        /// <summary>
        /// Indicates that the tagger is not view, and should be on a very delayed update cadence.
        /// </summary>
        NonFocus,
    }
}

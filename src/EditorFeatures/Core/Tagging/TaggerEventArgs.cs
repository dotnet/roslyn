// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Information provided to the <see cref="AsynchronousTaggerProvider{TTag}"/> when 
    /// <see cref="ITaggerEventSource.Changed"/> fires.
    /// </summary>
    internal class TaggerEventArgs : EventArgs
    {
        /// <summary>
        /// The kind of this tagger event, likely a member of <see cref="PredefinedChangedEventKinds" />.
        /// </summary>
        internal string Kind { get; }

        /// <summary>
        /// They amount of time to wait before the <see cref="AsynchronousTaggerProvider{TTag}"/>
        /// checks for new tags and updates the user interface.
        /// </summary>
        public TaggerDelay Delay { get; }
        internal TextContentChangedEventArgs TextChangeEventArgs { get; }

        /// <summary>
        /// Creates a new <see cref="TaggerEventArgs"/>
        /// </summary>
        public TaggerEventArgs(TaggerDelay delay) : this("", delay)
        {
        }

        internal TaggerEventArgs(
            string kind,
            TaggerDelay delay,
            TextContentChangedEventArgs textChangeEventArgs = null)
        {
            this.Kind = kind;
            this.Delay = delay;
            this.TextChangeEventArgs = textChangeEventArgs;
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// An object that contains an instance of an SourceText and raises events when its current instance
    /// changes.
    /// </summary>
    public abstract class SourceTextContainer
    {
        /// <summary>
        /// The current text instance.
        /// </summary>
        public abstract SourceText CurrentText { get; }

        /// <summary>
        /// Raised when the current text instance changes.
        /// </summary>
        public abstract event EventHandler<TextChangeEventArgs> TextChanged;
    }
}

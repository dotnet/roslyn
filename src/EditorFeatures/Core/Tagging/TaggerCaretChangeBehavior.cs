// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Flags that affect how the tagger infrastructure responds to caret changes.
    /// </summary>
    [Flags]
    internal enum TaggerCaretChangeBehavior
    {
        /// <summary>
        /// No special caret change behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// If the caret moves outside of a tag, immediately remove all existing tags.
        /// </summary>
        RemoveAllTagsOnCaretMoveOutsideOfTag = 1 << 0,
    }
}

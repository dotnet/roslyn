// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
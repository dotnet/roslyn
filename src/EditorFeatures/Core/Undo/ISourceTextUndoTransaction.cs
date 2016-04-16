// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    /// <summary>
    /// Represents undo transaction for a <see cref="Microsoft.CodeAnalysis.Text.SourceText"/>
    /// with a display string by which the IDE's undo stack UI refers to the transaction.
    /// </summary>
    internal interface ISourceTextUndoTransaction : IDisposable
    {
        /// <summary>
        /// The <see cref="Microsoft.CodeAnalysis.Text.SourceText"/> for this undo transaction.
        /// </summary>
        SourceText SourceText { get; }

        /// <summary>
        /// The display string by which the IDE's undo stack UI refers to the transaction.
        /// </summary>
        string Description { get; }
    }
}

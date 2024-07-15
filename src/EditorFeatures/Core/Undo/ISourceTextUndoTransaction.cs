// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Undo;

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

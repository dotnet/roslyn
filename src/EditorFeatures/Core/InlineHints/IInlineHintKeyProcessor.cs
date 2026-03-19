// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

internal interface IInlineHintKeyProcessor
{
    /// <summary>
    /// The current state of the keyprocessor.  i.e. whether or not the key binding is currently being held down or
    /// not.  Can be read on any thread.
    /// </summary>
    bool State { get; }

    /// <summary>
    /// Called when the state of the keyprocessor changes.  Only fired on UI thread.
    /// </summary>
    event Action StateChanged;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

public sealed class TextDocumentEventArgs(TextDocument document) : EventArgs
{
    public TextDocument Document { get; } = document ?? throw new ArgumentNullException(nameof(document));
}

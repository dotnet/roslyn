// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static class TextChangeExtensions
{
    public static TextChangeRange ToTextChangeRange(this ITextChange textChange)
        => new(textChange.OldSpan.ToTextSpan(), textChange.NewLength);
}

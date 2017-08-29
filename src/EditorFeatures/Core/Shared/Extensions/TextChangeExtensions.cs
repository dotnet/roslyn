// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class TextChangeExtensions
    {
        public static TextChangeRange ToTextChangeRange(this ITextChange textChange)
        {
            return new TextChangeRange(textChange.OldSpan.ToTextSpan(), textChange.NewLength);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct DebugDataTipInfo
    {
        public readonly TextSpan Span;
        public readonly string Text;

        public DebugDataTipInfo(TextSpan span, string text)
        {
            Span = span;
            Text = text;
        }

        public bool IsDefault
            => Span.Length == 0 && Span.Start == 0 && Text == null;
    }
}

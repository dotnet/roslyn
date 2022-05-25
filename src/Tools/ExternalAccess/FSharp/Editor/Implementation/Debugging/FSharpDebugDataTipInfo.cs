// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal readonly struct FSharpDebugDataTipInfo
    {
        internal readonly DebugDataTipInfo UnderlyingObject;

        public FSharpDebugDataTipInfo(TextSpan span, string text)
            => UnderlyingObject = new DebugDataTipInfo(span, text);

        public readonly TextSpan Span => UnderlyingObject.Span;
        public readonly string Text => UnderlyingObject.Text;
        public bool IsDefault => UnderlyingObject.IsDefault;
    }
}

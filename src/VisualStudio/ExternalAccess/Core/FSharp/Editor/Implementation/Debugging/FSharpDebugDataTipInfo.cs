// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor.Implementation.Debugging;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging;
#endif

internal readonly struct FSharpDebugDataTipInfo(TextSpan span, string text)
{
    internal readonly DebugDataTipInfo UnderlyingObject = new(span, text);

    public readonly TextSpan Span => UnderlyingObject.Span;
    public readonly string Text => UnderlyingObject.Text!;
    public bool IsDefault => UnderlyingObject.IsDefault;
}

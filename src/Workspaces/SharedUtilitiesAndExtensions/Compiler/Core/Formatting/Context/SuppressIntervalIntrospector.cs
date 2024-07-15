// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Formatting;

internal readonly struct SuppressIntervalIntrospector :
    IIntervalIntrospector<SuppressSpacingData>,
    IIntervalIntrospector<SuppressWrappingData>
{
    int IIntervalIntrospector<SuppressSpacingData>.GetStart(SuppressSpacingData value)
        => value.TextSpan.Start;

    int IIntervalIntrospector<SuppressSpacingData>.GetLength(SuppressSpacingData value)
        => value.TextSpan.Length;

    int IIntervalIntrospector<SuppressWrappingData>.GetStart(SuppressWrappingData value)
        => value.TextSpan.Start;

    int IIntervalIntrospector<SuppressWrappingData>.GetLength(SuppressWrappingData value)
        => value.TextSpan.Length;
}

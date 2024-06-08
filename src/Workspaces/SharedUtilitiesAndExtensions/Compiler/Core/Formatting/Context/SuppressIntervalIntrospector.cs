// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal readonly struct SuppressIntervalIntrospector :
    IIntervalIntrospector<SuppressSpacingData>,
    IIntervalIntrospector<SuppressWrappingData>
{
    TextSpan IIntervalIntrospector<SuppressSpacingData>.GetSpan(SuppressSpacingData value)
        => value.TextSpan;

    TextSpan IIntervalIntrospector<SuppressWrappingData>.GetSpan(SuppressWrappingData value)
        => value.TextSpan;
}

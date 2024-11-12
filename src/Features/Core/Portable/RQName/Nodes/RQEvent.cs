// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal sealed class RQEvent(RQUnconstructedType containingType, RQMethodPropertyOrEventName memberName) : RQMethodPropertyOrEvent(containingType, memberName)
{
    protected override string RQKeyword
    {
        get { return RQNameStrings.Event; }
    }
}

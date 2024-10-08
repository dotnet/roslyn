// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal sealed class RQIndexer(
    RQUnconstructedType containingType,
    RQMethodPropertyOrEventName memberName,
    int typeParameterCount,
    IList<RQParameter> parameters) : RQPropertyBase(containingType, memberName, typeParameterCount, parameters)
{
}

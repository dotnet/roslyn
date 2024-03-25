// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Features.RQName.SimpleTree;

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes;

internal abstract class RQMethodPropertyOrEventName
{
    /// <summary>
    /// Methods, Properties, or Events either have "ordinary" names,
    /// or explicit interface names. But even explicit names have an
    /// underlying ordinary name as well. This is just the value for
    /// ordinary names, or the underlying ordinary name if this is an
    /// explicit name.
    /// </summary>
    public abstract string OrdinaryNameValue { get; }

    public abstract SimpleGroupNode ToSimpleTree();
}

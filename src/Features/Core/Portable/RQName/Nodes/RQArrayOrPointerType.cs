// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Features.RQName.Nodes
{
    internal abstract class RQArrayOrPointerType(RQType elementType) : RQType
    {
        public readonly RQType ElementType = elementType;
    }
}

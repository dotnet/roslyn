// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis;

[CollectionBuilder(typeof(SeparatedSyntaxListWrapper), nameof(SeparatedSyntaxListWrapper.Create))]
internal readonly struct SeparatedSyntaxListWrapper<TNode> : IEnumerable<TNode> //IEquatable<SeparatedSyntaxListWrapper<TNode>>, IReadOnlyList<TNode>
{
    IEnumerator<TNode> IEnumerable<TNode>.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

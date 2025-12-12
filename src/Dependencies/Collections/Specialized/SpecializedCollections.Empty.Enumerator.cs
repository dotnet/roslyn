// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class Enumerator : IEnumerator
            {
                public static readonly IEnumerator Instance = new Enumerator();

                protected Enumerator()
                {
                }

                public object? Current => throw new InvalidOperationException();

                public bool MoveNext()
                {
                    return false;
                }

                public void Reset()
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}

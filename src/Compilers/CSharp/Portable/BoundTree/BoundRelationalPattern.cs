// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundRelationalPattern
    {
        private partial void Validate()
        {
            if (IsUnionMatching)
            {
                Debug.Assert(NarrowedType.IsObjectType() ||
                             NarrowedType.Equals(Value.Type, TypeCompareKind.AllIgnoreOptions));
            }
            else
            {
                Debug.Assert(NarrowedType.Equals(InputType, TypeCompareKind.AllIgnoreOptions) ||
                             NarrowedType.Equals(Value.Type, TypeCompareKind.AllIgnoreOptions));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundITuplePattern
    {
        private partial void Validate()
        {
            Debug.Assert(NarrowedType.IsCompilerServicesTopLevelType() && NarrowedType.Name == "ITuple");
        }

        internal BoundITuplePattern WithSubpatterns(ImmutableArray<BoundPositionalSubpattern> subpatterns)
        {
            return this.Update(this.GetLengthMethod, this.GetItemMethod, subpatterns, this.InputType, this.NarrowedType);
        }
    }
}

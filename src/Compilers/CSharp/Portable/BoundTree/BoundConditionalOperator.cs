// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundConditionalOperator
    {
        private partial void Validate()
        {
            if (!IsRef)
            {
                bool refersToLocation;
                Debug.Assert(!CodeGenerator.IsPointerIndirection(Consequence, out refersToLocation) || !refersToLocation);
                Debug.Assert(!CodeGenerator.IsPointerIndirection(Alternative, out refersToLocation) || !refersToLocation);
            }
        }
    }
}


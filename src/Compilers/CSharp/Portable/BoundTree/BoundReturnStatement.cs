// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundReturnStatement
    {
        private partial void Validate()
        {
            Debug.Assert(RefKind is RefKind.None or RefKind.Ref); // We assume that 'ref readonly' cannot be result of a return inference for a lambda.
        }
    }
}

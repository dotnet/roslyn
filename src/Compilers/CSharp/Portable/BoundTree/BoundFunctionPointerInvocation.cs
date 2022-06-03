// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundFunctionPointerInvocation
    {
        public FunctionPointerTypeSymbol FunctionPointer
        {
            get
            {
                Debug.Assert(InvokedExpression.Type is FunctionPointerTypeSymbol);
                return (FunctionPointerTypeSymbol)InvokedExpression.Type;
            }
        }
    }
}

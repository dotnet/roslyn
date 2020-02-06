// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

namespace Microsoft.CodeAnalysis
{
    // PROTOTYPE(func-ptr): Document
    // PROTOTYPE(func-ptr): Expose calling convention on either this or IMethodSymbol in general
    public interface IFunctionPointerTypeSymbol : ISymbol
    {
        public IMethodSymbol Signature { get; }
    }
}

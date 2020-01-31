// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a function pointer type such as "delegate*&lt;void&gt;".
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    // https://github.com/dotnet/roslyn/issues/39865: Expose calling convention on either this or IMethodSymbol in general
    public interface IFunctionPointerTypeSymbol : ITypeSymbol
    {
        /// <summary>
        /// Gets the signature of the function pointed to by an instance of the function pointer type.
        /// </summary>
        public IMethodSymbol Signature { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Symbols
{
    /// <summary>
    /// Synthesized symbol that implements a method body feature (iterator, async, lambda, etc.)
    /// </summary>
    internal interface ISynthesizedMethodBodyImplementationSymbol : ISymbolInternal
    {
        /// <summary>
        /// The symbol whose body lowering produced this synthesized symbol, 
        /// or null if the symbol is synthesized based on declaration.
        /// </summary>
        IMethodSymbolInternal? Method { get; }

        /// <summary>
        /// True if this symbol body needs to be updated when the <see cref="Method"/> body is updated.
        /// False if <see cref="Method"/> is null.
        /// </summary>
        bool HasMethodBodyDependency { get; }
    }
}

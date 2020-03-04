// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface IMethodSymbolInternal : ISymbolInternal
    {
        /// <summary>
        /// True if the method is a source method implemented as an iterator.
        /// </summary>
        bool IsIterator { get; }

        /// <summary>
        /// Returns true if this method is an async method
        /// </summary>
        bool IsAsync { get; }

        int CalculateLocalSyntaxOffset(int declaratorPosition, SyntaxTree declaratorTree);

        /// <summary>
        /// Returns a constructed method given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the method.</param>
        IMethodSymbolInternal Construct(params ITypeSymbolInternal[] typeArguments);
    }
}

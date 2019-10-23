// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface IMethodSymbolInternal : IMethodSymbol
    {
        /// <summary>
        /// True if the method is a source method implemented as an iterator.
        /// </summary>
        bool IsIterator { get; }

        int CalculateLocalSyntaxOffset(int declaratorPosition, SyntaxTree declaratorTree);
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a using alias (Imports alias in Visual Basic).
    /// </summary>
    public interface IAliasSymbol : ISymbol
    {
        /// <summary>
        /// Gets the <see cref="INamespaceOrTypeSymbol"/> for the
        /// namespace or type referenced by the alias.
        /// </summary>
        INamespaceOrTypeSymbol Target { get; }
    }
}
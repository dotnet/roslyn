// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a label in method body
    /// </summary>
    public interface ILabelSymbol : ISymbol
    {
        /// <summary>
        /// Gets the immediately containing <see cref="IMethodSymbol"/> of this <see cref="ILocalSymbol"/>.
        /// </summary>
        IMethodSymbol ContainingMethod { get; }
    }
}
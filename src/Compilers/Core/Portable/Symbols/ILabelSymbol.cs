// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a label in method body
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILabelSymbol : ISymbol
    {
        /// <summary>
        /// Gets the immediately containing <see cref="IMethodSymbol"/> of this <see cref="ILocalSymbol"/>.
        /// </summary>
        IMethodSymbol ContainingMethod { get; }
    }
}

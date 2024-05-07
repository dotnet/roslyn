// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// This interface provides a subset of <see cref="IPointerTypeSymbol"/> for use in code generation scenarios.
/// </summary>
internal interface ICodeGenerationPointerTypeSymbol : ICodeGenerationTypeSymbol
{
    /// <summary>
    /// Gets the type of the storage location that an instance of the pointer type points to.
    /// </summary>
    ITypeSymbol PointedAtType { get; }

    /// <summary>
    /// Custom modifiers associated with the pointer type, or an empty array if there are none.
    /// </summary>
    /// <remarks>
    /// Some managed languages may represent special information about the pointer type
    /// as a custom modifier on either the pointer type or the element type, or
    /// both.
    /// </remarks>
    ImmutableArray<CustomModifier> CustomModifiers { get; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// This interface provides a subset of <see cref="IParameterSymbol"/> for use in code generation scenarios.
/// </summary>
internal interface ICodeGenerationParameterSymbol : ICodeGenerationSymbol
{
    /// <summary>
    /// Whether the parameter passed by value or by reference.
    /// </summary>
    RefKind RefKind { get; }

    /// <summary>
    /// Returns the scoped kind of the parameter.
    /// </summary>
    ScopedKind ScopedKind { get; }

    /// <summary>
    /// Returns true if the parameter was declared as a parameter array or as a parameter collection. 
    /// </summary>
    bool IsParams { get; }

    /// <summary>
    /// Returns true if the parameter was declared as a parameter array. 
    /// </summary>
    bool IsParamsArray { get; }

    /// <summary>
    /// Returns true if the parameter was declared as a parameter collection. 
    /// </summary>
    bool IsParamsCollection { get; }

    /// <summary>
    /// Returns true if the parameter is optional.
    /// </summary>
    bool IsOptional { get; }

    /// <summary>
    /// Returns true if the parameter is the hidden 'this' ('Me' in Visual Basic) parameter.
    /// </summary>
    bool IsThis { get; }

    /// <summary>
    /// Returns true if the parameter is a discard parameter.
    /// </summary>
    bool IsDiscard { get; }

    /// <summary>
    /// Gets the type of the parameter.
    /// </summary>
    ITypeSymbol Type { get; }

    /// <summary>
    /// Gets the top-level nullability of the parameter.
    /// </summary>
    NullableAnnotation NullableAnnotation { get; }

    /// <summary>
    /// Custom modifiers associated with the parameter type, or an empty array if there are none.
    /// </summary>
    ImmutableArray<CustomModifier> CustomModifiers { get; }

    /// <summary>
    /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
    /// </summary>
    ImmutableArray<CustomModifier> RefCustomModifiers { get; }

    /// <summary>
    /// Gets the ordinal position of the parameter. The first parameter has ordinal zero.
    /// The 'this' parameter ('Me' in Visual Basic) has ordinal -1.
    /// </summary>
    int Ordinal { get; }

    /// <summary>
    /// Returns true if the parameter specifies a default value to be passed
    /// when no value is provided as an argument to a call. The default value
    /// can be obtained with the <see cref="ExplicitDefaultValue"/> property.
    /// </summary>
    bool HasExplicitDefaultValue { get; }

    /// <summary>
    /// Returns the default value of the parameter. 
    /// </summary>
    /// <remarks>
    /// Returns null if the parameter type is a struct and the default value of the parameter
    /// is the default value of the struct type.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">The parameter has no default value.</exception>
    object? ExplicitDefaultValue { get; }

    /// <summary>
    /// Get the original definition of this symbol. If this symbol is derived from another
    /// symbol by (say) type substitution, this gets the original symbol, as it was defined in
    /// source or metadata.
    /// </summary>
    new IParameterSymbol OriginalDefinition { get; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a field in a class, struct or enum.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IFieldSymbol : ISymbol
    {
        /// <summary>
        /// If this field serves as a backing variable for an automatically generated
        /// property or a field-like event, returns that 
        /// property/event. Otherwise returns null.
        /// Note, the set of possible associated symbols might be expanded in the future to 
        /// reflect changes in the languages.
        /// </summary>
        ISymbol? AssociatedSymbol { get; }

        /// <summary>
        /// Returns true if this field was declared as "const" (i.e. is a constant declaration).
        /// Also returns true for an enum member.
        /// </summary>
        bool IsConst { get; }

        /// <summary>
        /// Returns true if this field was declared as "readonly". 
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Returns true if this field was declared as "volatile". 
        /// </summary>
        bool IsVolatile { get; }

        /// <summary>
        /// True if this field is required to be set in an object initializer during construction.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Returns true if this field was declared as "fixed".
        /// Note that for a fixed-size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        bool IsFixedSizeBuffer { get; }

        /// <summary>
        /// If IsFixedSizeBuffer is true, the value between brackets in the fixed-size-buffer declaration.
        /// If IsFixedSizeBuffer is false or there is an error (such as a bad constant value in source), FixedSize is 0.
        /// Note that for fixed-size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        int FixedSize { get; }

        /// <summary>
        /// Returns the RefKind of the field.
        /// </summary>
        RefKind RefKind { get; }

        /// <summary>
        /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
        /// </summary>
        ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        /// <summary>
        /// Gets the type of this field.
        /// </summary>
        ITypeSymbol Type { get; }

        /// <summary>
        /// Gets the top-level nullability of this field.
        /// </summary>
        NullableAnnotation NullableAnnotation { get; }

        /// <summary>
        /// Returns false if the field wasn't declared as "const", or constant value was omitted or erroneous.
        /// True otherwise.
        /// </summary>
        [MemberNotNullWhen(true, nameof(ConstantValue))]
        bool HasConstantValue { get; }

        /// <summary>
        /// Gets the constant value of this field
        /// </summary>
        object? ConstantValue { get; }

        /// <summary>
        /// Returns custom modifiers associated with the field, or an empty array if there are none.
        /// </summary>
        ImmutableArray<CustomModifier> CustomModifiers { get; }

        /// <summary>
        /// Get the original definition of this symbol. If this symbol is derived from another
        /// symbol by (say) type substitution, this gets the original symbol, as it was defined in
        /// source or metadata.
        /// </summary>
        new IFieldSymbol OriginalDefinition { get; }

        /// <summary>
        /// If this field represents a tuple element, returns a corresponding default element field.
        /// Otherwise returns null.
        /// </summary>
        /// <remarks>
        /// A tuple type will always have default elements such as Item1, Item2, Item3...
        /// This API allows matching a field that represents a named element, such as "Alice" 
        /// to the corresponding default element field such as "Item1"
        /// </remarks>
        IFieldSymbol? CorrespondingTupleField { get; }

        /// <summary>
        /// Returns true if this field represents a tuple element which was given an explicit name.
        /// </summary>
        bool IsExplicitlyNamedTupleElement { get; }
    }
}

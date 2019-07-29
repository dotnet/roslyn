// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

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
        ISymbol AssociatedSymbol { get; }

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
        /// Returns true if this field was declared as "fixed".
        /// Note that for a fixed-size buffer declaration, this.Type will be a pointer type, of which
        /// the pointed-to type will be the declared element type of the fixed-size buffer.
        /// </summary>
        bool IsFixedSizeBuffer { get; }

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
        bool HasConstantValue { get; }

        /// <summary>
        /// Gets the constant value of this field
        /// </summary>
        object ConstantValue { get; }

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
        IFieldSymbol CorrespondingTupleField { get; }
    }
}

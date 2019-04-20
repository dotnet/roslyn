// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a property or indexer.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPropertySymbol : ISymbol
    {
        /// <summary>
        /// Returns whether the property is really an indexer.
        /// </summary>
        bool IsIndexer { get; }

        /// <summary>
        /// True if this is a read-only property; that is, a property with no set accessor.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// True if this is a write-only property; that is, a property with no get accessor.
        /// </summary>
        bool IsWriteOnly { get; }

        /// <summary>
        /// Returns true if this property is an auto-created WithEvents property that takes place of
        /// a field member when the field is marked as WithEvents.
        /// </summary>
        bool IsWithEvents { get; }

        /// <summary>
        /// Returns true if this property returns by reference.
        /// </summary>
        bool ReturnsByRef { get; }

        /// <summary>
        /// Returns true if this property returns by reference a readonly variable.
        /// </summary>
        bool ReturnsByRefReadonly { get; }

        /// <summary>
        /// Returns the RefKind of the property.
        /// </summary>
        RefKind RefKind { get; }

        /// <summary>
        /// The type of the property. 
        /// </summary>
        ITypeSymbol Type { get; }

        NullableAnnotation NullableAnnotation { get; }

        /// <summary>
        /// The parameters of this property. If this property has no parameters, returns
        /// an empty list. Parameters are only present on indexers, or on some properties
        /// imported from a COM interface.
        /// </summary>
        ImmutableArray<IParameterSymbol> Parameters { get; }

        /// <summary>
        /// The 'get' accessor of the property, or null if the property is write-only.
        /// </summary>
        IMethodSymbol GetMethod { get; }

        /// <summary>
        /// The 'set' accessor of the property, or null if the property is read-only.
        /// </summary>
        IMethodSymbol SetMethod { get; }

        /// <summary>
        /// The original definition of the property. If the property is constructed from another
        /// symbol by type substitution, OriginalDefinition gets the original symbol, as it was 
        /// defined in source or metadata.
        /// </summary>
        new IPropertySymbol OriginalDefinition { get; }

        /// <summary>
        /// Returns the overridden property, or null.
        /// </summary>
        IPropertySymbol OverriddenProperty { get; }

        /// <summary>
        /// Returns interface properties explicitly implemented by this property.
        /// </summary>
        /// <remarks>
        /// Properties imported from metadata can explicitly implement more than one property.
        /// </remarks>
        ImmutableArray<IPropertySymbol> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
        /// </summary>
        ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        /// <summary>
        /// The list of custom modifiers, if any, associated with the type of the property. 
        /// </summary>
        ImmutableArray<CustomModifier> TypeCustomModifiers { get; }
    }
}

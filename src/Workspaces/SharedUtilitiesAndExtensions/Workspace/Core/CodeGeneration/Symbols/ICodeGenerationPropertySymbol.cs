// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// This interface provides a subset of <see cref="IPropertySymbol"/> for use in code generation scenarios.
/// </summary>
internal interface ICodeGenerationPropertySymbol : ICodeGenerationSymbol
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
    /// True if this property is required to be set in an object initializer during construction.
    /// </summary>
    bool IsRequired { get; }

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
    IMethodSymbol? GetMethod { get; }

    /// <summary>
    /// The 'set' accessor of the property, or null if the property is read-only.
    /// </summary>
    IMethodSymbol? SetMethod { get; }

    /// <summary>
    /// The original definition of the property. If the property is constructed from another
    /// symbol by type substitution, OriginalDefinition gets the original symbol, as it was 
    /// defined in source or metadata.
    /// </summary>
    new IPropertySymbol OriginalDefinition { get; }

    /// <summary>
    /// Returns the overridden property, or null.
    /// </summary>
    IPropertySymbol? OverriddenProperty { get; }

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

    /// <summary>
    /// If this is a partial property implementation part, returns the corresponding
    /// definition part.  Otherwise null.
    /// </summary>
    IPropertySymbol? PartialDefinitionPart { get; }

    /// <summary>
    /// If this is a partial property definition part, returns the corresponding
    /// implementation part.  Otherwise null.
    /// </summary>
    IPropertySymbol? PartialImplementationPart { get; }

    /// <summary>
    /// Returns true if this is a partial definition part.  Otherwise false.
    /// </summary>
    bool IsPartialDefinition { get; }
}

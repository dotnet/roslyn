// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a type other than an array, a pointer, a type parameter.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface INamedTypeSymbol : ITypeSymbol
    {
        /// <summary>
        /// Returns the arity of this type, or the number of type parameters it takes.
        /// A non-generic type has zero arity.
        /// </summary>
        int Arity { get; }

        /// <summary>
        /// True if this type or some containing type has type parameters.
        /// </summary>
        bool IsGenericType { get; }

        /// <summary>
        /// True if this is a reference to an <em>unbound</em> generic type. A generic type is
        /// considered <em>unbound</em> if all of the type argument lists in its fully qualified
        /// name are empty. Note that the type arguments of an unbound generic type will be
        /// returned as error types because they do not really have type arguments.  An unbound
        /// generic type yields null for its BaseType and an empty result for its Interfaces.
        /// </summary>
        bool IsUnboundGenericType { get; }

        /// <summary>
        /// Returns true if the type is a Script class. 
        /// It might be an interactive submission class or a Script class in a csx file.
        /// </summary>
        bool IsScriptClass { get; }

        /// <summary>
        /// Returns true if the type is the implicit class that holds onto invalid global members (like methods or
        /// statements in a non script file).
        /// </summary>
        bool IsImplicitClass { get; }

        /// <summary>
        /// Specifies that the class or interface is imported from another module.  See
        /// <see cref="TypeAttributes.Import"/> and <see cref="ComImportAttribute"/>
        /// </summary>
        bool IsComImport { get; }

        /// <summary>
        /// Returns collection of names of members declared within this type.
        /// </summary>
        IEnumerable<string> MemberNames { get; }

        /// <summary>
        /// Returns the type parameters that this type has. If this is a non-generic type,
        /// returns an empty ImmutableArray.  
        /// </summary>
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters. 
        /// If nothing has been substituted for a given type parameter,
        /// then the type parameter itself is considered the type argument.
        /// </summary>
        ImmutableArray<ITypeSymbol> TypeArguments { get; }

        /// <summary>
        /// Returns the top-level nullability of the type arguments that have been substituted
        /// for the type parameters. If nothing has been substituted for a given type parameter,
        /// then <see cref="NullableAnnotation.None"/> is returned for that type argument.
        /// </summary>
        ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations { get; }

        /// <summary>
        /// Returns custom modifiers for the type argument that has been substituted for the type parameter. 
        /// The modifiers correspond to the type argument at the same ordinal within the <see cref="TypeArguments"/>
        /// array. Returns an empty array if there are no modifiers.
        /// </summary>
        ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal);

        /// <summary>
        /// Get the original definition of this type symbol. If this symbol is derived from another
        /// symbol by (say) type substitution, this gets the original symbol, as it was defined in
        /// source or metadata.
        /// </summary>
        new INamedTypeSymbol OriginalDefinition { get; }

        /// <summary>
        /// For delegate types, gets the delegate's invoke method.  Returns null on
        /// all other kinds of types.  Note that it is possible to have an ill-formed
        /// delegate type imported from metadata which does not have an Invoke method.
        /// Such a type will be classified as a delegate but its DelegateInvokeMethod
        /// would be null.
        /// </summary>
        IMethodSymbol DelegateInvokeMethod { get; }

        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        INamedTypeSymbol EnumUnderlyingType { get; }

        /// <summary>
        /// Returns the type symbol that this type was constructed from. This type symbol
        /// has the same containing type (if any), but has type arguments that are the same
        /// as the type parameters (although its containing type might not).
        /// </summary>
        INamedTypeSymbol ConstructedFrom { get; }

        /// <summary>
        /// Returns a constructed type given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the type.</param>
        INamedTypeSymbol Construct(params ITypeSymbol[] typeArguments);

        /// <summary>
        /// Returns a constructed type given its type arguments and type argument nullable annotations.
        /// </summary>
        INamedTypeSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations);

        /// <summary>
        /// Returns an unbound generic type of this named type.
        /// </summary>
        INamedTypeSymbol ConstructUnboundGenericType();

        /// <summary>
        /// Get the instance constructors for this type.
        /// </summary>
        ImmutableArray<IMethodSymbol> InstanceConstructors { get; }

        /// <summary>
        /// Get the static constructors for this type.
        /// </summary>
        ImmutableArray<IMethodSymbol> StaticConstructors { get; }

        /// <summary>
        /// Get the both instance and static constructors for this type.
        /// </summary>
        ImmutableArray<IMethodSymbol> Constructors { get; }

        /// <summary>
        /// For implicitly declared delegate types returns the EventSymbol that caused this
        /// delegate type to be generated.
        /// For all other types returns null.
        /// Note, the set of possible associated symbols might be expanded in the future to 
        /// reflect changes in the languages.
        /// </summary>
        ISymbol AssociatedSymbol { get; }

        /// <summary>
        /// Determines if the symbol might contain extension methods. 
        /// If false, the symbol does not contain extension methods. 
        /// </summary>
        bool MightContainExtensionMethods { get; }

        /// <summary>
        /// If this is a tuple type symbol, returns the symbol for its underlying type.
        /// Otherwise, returns null.
        /// The type argument corresponding to the type of the extension field (VT[8].Rest),
        /// which is at the 8th (one based) position is always a symbol for another tuple, 
        /// rather than its underlying type.
        /// </summary>
        INamedTypeSymbol TupleUnderlyingType { get; }

        /// <summary>
        /// Returns fields that represent tuple elements for types that are tuples.
        ///
        /// If this type is not a tuple, then returns default.
        /// </summary>
        ImmutableArray<IFieldSymbol> TupleElements { get; }

        /// <summary>
        /// True if the type is serializable (has Serializable metadata flag).
        /// </summary>
        bool IsSerializable { get; }

        /// <summary>
        /// True if the type is a natively-sized integer.
        /// </summary>
        bool IsNativeInt { get; }
    }
}

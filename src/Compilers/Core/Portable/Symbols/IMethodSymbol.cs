// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a method or method-like symbol (including constructor,
    /// destructor, operator, or property/event accessor).
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMethodSymbol : ISymbol
    {
        /// <summary>
        /// Gets what kind of method this is. There are several different kinds of things in the
        /// C# language that are represented as methods. This property allow distinguishing those things
        /// without having to decode the name of the method.
        /// </summary>
        MethodKind MethodKind { get; }

        /// <summary>
        /// Returns the arity of this method. Arity is the number of type parameters a method declares.
        /// A non-generic method has zero arity.
        /// </summary>
        int Arity { get; }

        /// <summary>
        /// Returns whether this method is generic; i.e., does it have any type parameters?
        /// </summary>
        bool IsGenericMethod { get; }

        /// <summary>
        /// Returns true if this method is a "classic" extension method (using the <see langword="this"/>
        /// modifier in C# or <see cref="System.Runtime.CompilerServices.ExtensionAttribute"/> in VB).
        /// </summary>
        /// <remarks>
        /// Returns false for methods in <c>extension()</c> blocks.
        /// To check if a method is a "new" extension method (a member of an <c>extension()</c> block),
        /// check <see cref="INamedTypeSymbol.IsExtension"/> on the method's <see cref="ISymbol.ContainingType"/>.
        /// </remarks>
        bool IsExtensionMethod { get; }

        /// <summary>
        /// Returns true if this method is an async method
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Returns whether this method is using CLI VARARG calling convention. This is used for
        /// C-style variable argument lists. This is used extremely rarely in C# code and is
        /// represented using the undocumented "__arglist" keyword.
        ///
        /// Note that methods with "params" on the last parameter are indicated with the "IsParams"
        /// property on ParameterSymbol, and are not represented with this property.
        /// </summary>
        bool IsVararg { get; }

        /// <summary>
        /// Returns whether this built-in operator checks for integer overflow.
        /// </summary>
        bool IsCheckedBuiltin { get; }

        /// <summary>
        /// Returns true if this method hides base methods by name. This cannot be specified directly
        /// in the C# language, but can be true for methods defined in other languages imported from
        /// metadata. The equivalent of the "hidebyname" flag in metadata. 
        /// </summary>
        bool HidesBaseMethodsByName { get; }

        /// <summary>
        /// Returns true if this method has no return type; i.e., returns "void".
        /// </summary>
        bool ReturnsVoid { get; }

        /// <summary>
        /// Returns true if this method returns by reference.
        /// </summary>
        bool ReturnsByRef { get; }

        /// <summary>
        /// Returns true if this method returns by ref readonly.
        /// </summary>
        bool ReturnsByRefReadonly { get; }

        /// <summary>
        /// Returns the RefKind of the method.
        /// </summary>
        RefKind RefKind { get; }

        /// <summary>
        /// Gets the return type of the method.
        /// </summary>
        ITypeSymbol ReturnType { get; }

        /// <summary>
        /// Gets the top-level nullability of the return type of the method.
        /// </summary>
        NullableAnnotation ReturnNullableAnnotation { get; }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters. 
        /// If nothing has been substituted for a given type parameter,
        /// then the type parameter itself is consider the type argument.
        /// </summary>
        ImmutableArray<ITypeSymbol> TypeArguments { get; }

        /// <summary>
        /// Returns the top-level nullability of the type arguments that have been substituted
        /// for the type parameters. If nothing has been substituted for a given type parameter,
        /// then <see cref="NullableAnnotation.None"/> is returned.
        /// </summary>
        ImmutableArray<NullableAnnotation> TypeArgumentNullableAnnotations { get; }

        /// <summary>
        /// Get the type parameters on this method. If the method has not generic,
        /// returns an empty list.
        /// </summary>
        ImmutableArray<ITypeParameterSymbol> TypeParameters { get; }

        /// <summary>
        /// Gets the parameters of this method. If this method has no parameters, returns
        /// an empty list.
        /// </summary>
        ImmutableArray<IParameterSymbol> Parameters { get; }

        /// <summary>
        /// Returns the method symbol that this method was constructed from. The resulting
        /// method symbol
        /// has the same containing type (if any), but has type arguments that are the same
        /// as the type parameters (although its containing type might not).
        /// </summary>
        IMethodSymbol ConstructedFrom { get; }

        /// <summary>
        /// Indicates whether the method is readonly,
        /// i.e. whether the 'this' receiver parameter is 'ref readonly'.
        /// Returns true for readonly instance methods and accessors
        /// and for reduced extension methods with a 'this in' parameter.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Returns true for 'init' set accessors, and false otherwise.
        /// </summary>
        bool IsInitOnly { get; }

        /// <summary>
        /// Get the original definition of this symbol. If this symbol is derived from another
        /// symbol by (say) type substitution, this gets the original symbol, as it was defined in
        /// source or metadata.
        /// </summary>
        new IMethodSymbol OriginalDefinition { get; }

        /// <summary>
        /// If this method overrides another method (because it both had the override modifier
        /// and there correctly was a method to override), returns the overridden method.
        /// </summary>
        IMethodSymbol? OverriddenMethod { get; }

        /// <summary>
        /// If this method can be applied to an object, returns the type of object it is applied to.
        /// </summary>
        ITypeSymbol? ReceiverType { get; }

        /// <summary>
        /// If this method can be applied to an object, returns the top-level nullability of the object it is applied to.
        /// </summary>
        NullableAnnotation ReceiverNullableAnnotation { get; }

        /// <summary>
        /// If this method is a reduced extension method, returns the definition of extension
        /// method from which this was reduced. Otherwise, returns null.
        /// </summary>
        IMethodSymbol? ReducedFrom { get; }

        /// <summary>
        /// If this method is a reduced extension method, returns a type inferred during reduction process for the type parameter. 
        /// </summary>
        /// <param name="reducedFromTypeParameter">Type parameter of the corresponding <see cref="ReducedFrom"/> method.</param>
        /// <returns>Inferred type or Nothing if nothing was inferred.</returns>
        /// <exception cref="System.InvalidOperationException">If this is not a reduced extension method.</exception>
        /// <exception cref="System.ArgumentNullException">If <paramref name="reducedFromTypeParameter"/> is null.</exception>
        /// <exception cref="System.ArgumentException">If <paramref name="reducedFromTypeParameter"/> doesn't belong to the corresponding <see cref="ReducedFrom"/> method.</exception>
        ITypeSymbol? GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter);

        /// <summary>
        /// If this is an extension method that can be applied to a receiver of the given type,
        /// returns a reduced extension method symbol thus formed. Otherwise, returns null.
        /// </summary>
        IMethodSymbol? ReduceExtensionMethod(ITypeSymbol receiverType);

        /// <summary>
        /// If this is a method of an extension block that can be applied to a receiver of the given type,
        /// returns the method symbol in the substituted extension for that receiver type. Otherwise, returns null.
        /// </summary>
        IMethodSymbol? ReduceExtensionMember(ITypeSymbol receiverType);

        /// <summary>
        /// Returns interface methods explicitly implemented by this method.
        /// </summary>
        /// <remarks>
        /// Methods imported from metadata can explicitly implement more than one method, 
        /// that is why return type is ImmutableArray.
        /// </remarks>
        ImmutableArray<IMethodSymbol> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Returns the list of custom modifiers, if any, associated with the return type. 
        /// </summary>
        ImmutableArray<CustomModifier> ReturnTypeCustomModifiers { get; }

        /// <summary>
        /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
        /// </summary>
        ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        /// <summary>
        /// Returns the list of custom attributes, if any, associated with the returned value. 
        /// </summary>
        ImmutableArray<AttributeData> GetReturnTypeAttributes();

        /// <summary>
        /// The calling convention enum of the method symbol.
        /// </summary>
        SignatureCallingConvention CallingConvention { get; }

        /// <summary>
        /// Modifier types that are considered part of the calling convention of this method, if the <see cref="MethodKind"/> is <see cref="MethodKind.FunctionPointerSignature"/>
        /// and the <see cref="CallingConvention"/> is <see cref="SignatureCallingConvention.Unmanaged"/>. If this is not a function pointer signature or the calling convention is
        /// not unmanaged, this is an empty array. Order and duplication of these modifiers reflect source/metadata order and duplication, whichever this symbol came from.
        /// </summary>
        ImmutableArray<INamedTypeSymbol> UnmanagedCallingConventionTypes { get; }

        /// <summary>
        /// Returns a symbol (e.g. property, event, etc.) associated with the method.
        /// </summary>
        /// <remarks>
        /// If this method has <see cref="MethodKind"/> of <see cref="MethodKind.PropertyGet"/> or <see cref="MethodKind.PropertySet"/>,
        /// returns the property that this method is the getter or setter for.
        /// If this method has <see cref="MethodKind"/> of <see cref="MethodKind.EventAdd"/> or <see cref="MethodKind.EventRemove"/>,
        /// returns the event that this method is the adder or remover for.
        /// Note, the set of possible associated symbols might be expanded in the future to 
        /// reflect changes in the languages.
        /// </remarks>
        ISymbol? AssociatedSymbol { get; }

        /// <summary>
        /// Returns a constructed method given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the method.</param>
        IMethodSymbol Construct(params ITypeSymbol[] typeArguments);

        /// <summary>
        /// Returns a constructed method given its type arguments and type argument nullable annotations.
        /// </summary>
        IMethodSymbol Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<NullableAnnotation> typeArgumentNullableAnnotations);

        /// <summary>
        /// If this is a partial method implementation part, returns the corresponding
        /// definition part.  Otherwise null.
        /// </summary>
        IMethodSymbol? PartialDefinitionPart { get; }

        /// <summary>
        /// If this is a partial method declaration without a body, and the method is
        /// implemented with a body, returns that implementing definition.  Otherwise
        /// null.
        /// </summary>
        IMethodSymbol? PartialImplementationPart { get; }

        /// <summary>
        /// Returns the implementation flags for the given method symbol.
        /// </summary>
        MethodImplAttributes MethodImplementationFlags { get; }

        /// <summary>
        /// Return true if this is a partial method definition without a body. If there
        /// is an implementing body, it can be retrieved with <see cref="PartialImplementationPart"/>.
        /// </summary>
        bool IsPartialDefinition { get; }

        /// <summary>
        /// Platform invoke information, or null if the method isn't a P/Invoke.
        /// </summary>
        DllImportData? GetDllImportData();

        /// <summary>
        /// If this method is a Lambda method (MethodKind = MethodKind.LambdaMethod) and 
        /// there is an anonymous delegate associated with it, returns this delegate.
        /// 
        /// Returns null if the symbol is not a lambda or if it does not have an
        /// anonymous delegate associated with it.
        /// </summary>
        INamedTypeSymbol? AssociatedAnonymousDelegate { get; }

        /// <summary>
        /// Returns a flag indicating whether this symbol has at least one applied/inherited conditional attribute.
        /// </summary>
        bool IsConditional { get; }

        /// <summary>
        /// Returns <see langword="true"/> if this method is a source method implemented as an iterator (either sync or async)
        /// </summary>
        bool IsIterator { get; }

        /// <summary>
        /// For a method/accessor/operator in an extension block, returns the corresponding implementation method if one exists.
        /// Returns null otherwise.
        /// 
        /// For example, considering:
        /// <code>
        /// static class E
        /// {
        ///     extension(int i)
        ///     {
        ///         public void M() { }
        ///     }
        /// }
        /// </code>
        /// When given the method symbol for <c>E.extension(int i).M()</c>,
        /// it returns the corresponding static implementation method <c>E.M(this int i)</c>.
        ///
        /// When given a generic extension member definition, it returns an implementation method constructed
        /// with the extension member's type parameters.
        /// </summary>
        IMethodSymbol? AssociatedExtensionImplementation { get; }
    }
}

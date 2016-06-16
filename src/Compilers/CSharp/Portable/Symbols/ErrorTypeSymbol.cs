// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An ErrorSymbol is used when the compiler cannot determine a symbol object to return because
    /// of an error. For example, if a field is declared "Foo x;", and the type "Foo" cannot be
    /// found, an ErrorSymbol is returned when asking the field "x" what it's type is.
    /// </summary>
    internal abstract partial class ErrorTypeSymbol : NamedTypeSymbol, IErrorTypeSymbol
    {
        internal static readonly ErrorTypeSymbol UnknownResultType = new UnsupportedMetadataTypeSymbol();

        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        /// <summary>
        /// The underlying error.
        /// </summary>
        internal abstract DiagnosticInfo ErrorInfo { get; }

        /// <summary>
        /// Summary of the reason why the type is bad.
        /// </summary>
        internal virtual LookupResultKind ResultKind { get { return LookupResultKind.Empty; } }

        /// <summary>
        /// Called by <see cref="AbstractTypeMap.SubstituteType"/> to perform substitution
        /// on types with TypeKind ErrorType.  The general pattern is to use the type map
        /// to perform substitution on the wrapped type, if any, and then construct a new
        /// error type symbol from the result (if there was a change).
        /// </summary>
        internal virtual TypeWithModifiers Substitute(AbstractTypeMap typeMap)
        {
            return new TypeWithModifiers((ErrorTypeSymbol)typeMap.SubstituteNamedType(this));
        }

        /// <summary>
        /// When constructing this ErrorTypeSymbol, there may have been symbols that seemed to
        /// be what the user intended, but were unsuitable. For example, a type might have been
        /// inaccessible, or ambiguous. This property returns the possible symbols that the user
        /// might have intended. It will return no symbols if no possible symbols were found.
        /// See the CandidateReason property to understand why the symbols were unsuitable.
        /// </summary>
        public virtual ImmutableArray<Symbol> CandidateSymbols
        {
            get
            {
                return ImmutableArray<Symbol>.Empty;
            }
        }

        ///<summary>
        /// If CandidateSymbols returns one or more symbols, returns the reason that those
        /// symbols were not chosen. Otherwise, returns None.
        /// </summary>
        public CandidateReason CandidateReason
        {
            get
            {
                if (!CandidateSymbols.IsEmpty)
                {
                    Debug.Assert(ResultKind != LookupResultKind.Viable, "Shouldn't have viable result kind on error symbol");
                    return ResultKind.ToCandidateReason();
                }
                else
                {
                    return CandidateReason.None;
                }
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return this.ErrorInfo;
        }

        /// <summary>
        /// Returns true if this type is known to be a reference type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public override bool IsReferenceType
        {
            // TODO: Consider returning False.
            get { return true; }
        }

        /// <summary>
        /// Returns true if this type is known to be a value type. It is never the case that
        /// IsReferenceType and IsValueType both return true. However, for an unconstrained type
        /// parameter, IsReferenceType and IsValueType will both return false.
        /// </summary>
        public override bool IsValueType
        {
            get { return false; }
        }

        /// <summary>
        /// Collection of names of members declared within this type.
        /// </summary>
        public override IEnumerable<string> MemberNames
        {
            get
            {
                return SpecializedCollections.EmptyEnumerable<string>();
            }
        }

        /// <summary>
        /// Get all the members of this symbol.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol. If this symbol has no members,
        /// returns an empty ImmutableArray. Never returns Null.</returns>
        public override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.Empty;
        }

        /// <summary>
        /// Get all the members of this symbol that have a particular name.
        /// </summary>
        /// <returns>An ImmutableArray containing all the members of this symbol with the given name. If there are
        /// no members with this name, returns an empty ImmutableArray. Never returns Null.</returns>
        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return ImmutableArray<Symbol>.Empty;
        }

        internal sealed override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembersUnordered();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
        }

        /// <summary>
        /// Get all the members of this symbol that are types.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol. If this symbol has no type members,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name, of any arity.
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name.
        /// If this symbol has no type members with this name,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        /// <summary>
        /// Get all the members of this symbol that are types that have a particular name and arity
        /// </summary>
        /// <returns>An ImmutableArray containing all the types that are members of this symbol with the given name and arity.
        /// If this symbol has no type members with this name and arity,
        /// returns an empty ImmutableArray. Never returns null.</returns>
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.ErrorType;
            }
        }

        /// <summary>
        /// Gets the kind of this type.
        /// </summary>
        public sealed override TypeKind TypeKind
        {
            get
            {
                return TypeKind.Error;
            }
        }

        internal sealed override bool IsInterface
        {
            get { return false; }
        }

        /// <summary>
        /// Get the symbol that logically contains this symbol. 
        /// </summary>
        public override Symbol ContainingSymbol
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the locations where this symbol was originally defined, either in source or
        /// metadata. Some symbols (for example, partial classes) may be defined in more than one
        /// location.
        /// </summary>
        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray<Location>.Empty;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        /// <summary>
        /// Returns the arity of this type, or the number of type parameters it takes.
        /// A non-generic type has zero arity.
        /// </summary>
        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the name of this symbol. Symbols without a name return the empty string; null is
        /// never returned.
        /// </summary>
        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters. 
        /// If nothing has been substituted for a give type parameters,
        /// then the type parameter itself is consider the type argument.
        /// </summary>
        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
            }
        }

        internal override bool HasTypeArgumentsCustomModifiers
        {
            get
            {
                return false;
            }
        }

        internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers
        {
            get
            {
                return CreateEmptyTypeArgumentsCustomModifiers();
            }
        }

        /// <summary>
        /// Returns the type parameters that this type has. If this is a non-generic type,
        /// returns an empty ImmutableArray.  
        /// </summary>
        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (_lazyTypeParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters,
                        GetTypeParameters(),
                        default(ImmutableArray<TypeParameterSymbol>));
                }
                return _lazyTypeParameters;
            }
        }

        private ImmutableArray<TypeParameterSymbol> GetTypeParameters()
        {
            int arity = this.Arity;
            if (arity == 0)
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                var @params = new TypeParameterSymbol[arity];
                for (int i = 0; i < arity; i++)
                {
                    @params[i] = new ErrorTypeParameterSymbol(this, string.Empty, i);
                }
                return @params.AsImmutableOrNull();
            }
        }

        /// <summary>
        /// Returns the type symbol that this type was constructed from. This type symbol
        /// has the same containing type (if any), but has type arguments that are the same
        /// as the type parameters (although its containing type might not).
        /// </summary>
        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Implements visitor pattern.
        /// </summary>
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitErrorType(this, argument);
        }

        // Only the compiler should create error symbols.
        internal ErrorTypeSymbol()
        {
        }

        /// <summary>
        /// Get this accessibility that was declared on this symbol. For symbols that do not have
        /// accessibility declared on them, returns NotApplicable.
        /// </summary>
        public sealed override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        /// <summary>
        /// Returns true if this symbol is "static"; i.e., declared with the "static" modifier or
        /// implicitly static.
        /// </summary>
        public sealed override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared as requiring an override; i.e., declared with
        /// the "abstract" modifier. Also returns true on a type declared as "abstract", all
        /// interface types, and members of interface types.
        /// </summary>
        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this symbol was declared to override a base class member and was also
        /// sealed from further overriding; i.e., declared with the "sealed" modifier.  Also set for
        /// types that do not allow a derived class (declared with "sealed" or "static" or "struct"
        /// or "enum" or "delegate").
        /// </summary>
        public sealed override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        internal sealed override bool HasSpecialName
        {
            get { return false; }
        }

        public sealed override bool MightContainExtensionMethods
        {
            get
            {
                return false;
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { return null; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return null;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        protected override NamedTypeSymbol ConstructCore(ImmutableArray<TypeWithModifiers> typeArguments, bool unbound)
        {
            return new ConstructedErrorTypeSymbol(this, typeArguments);
        }

        internal override NamedTypeSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return newOwner.IsDefinition ? this : new SubstitutedNestedErrorTypeSymbol(newOwner, this);
        }

        internal sealed override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        internal sealed override bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        internal sealed override TypeLayout Layout
        {
            get { return default(TypeLayout); }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return DefaultMarshallingCharSet; }
        }

        internal sealed override bool IsSerializable
        {
            get { return false; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal sealed override bool IsComImport
        {
            get { return false; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return AttributeUsageInfo.Null;
        }

        internal virtual bool Unreported
        {
            get { return false; }
        }

        #region IErrorTypeSymbol Members

        ImmutableArray<ISymbol> IErrorTypeSymbol.CandidateSymbols
        {
            get
            {
                return StaticCast<ISymbol>.From(CandidateSymbols);
            }
        }

        #endregion IErrorTypeSymbol Members
    }

    internal abstract class SubstitutedErrorTypeSymbol : ErrorTypeSymbol
    {
        private readonly ErrorTypeSymbol _originalDefinition;
        private int _hashCode;

        protected SubstitutedErrorTypeSymbol(ErrorTypeSymbol originalDefinition)
        {
            _originalDefinition = originalDefinition;
        }

        public override NamedTypeSymbol OriginalDefinition
        {
            get { return _originalDefinition; }
        }

        internal override bool MangleName
        {
            get { return _originalDefinition.MangleName; }
        }

        internal override DiagnosticInfo ErrorInfo
        {
            get { return _originalDefinition.ErrorInfo; }
        }

        public override int Arity
        {
            get { return _originalDefinition.Arity; }
        }

        public override string Name
        {
            get { return _originalDefinition.Name; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _originalDefinition.Locations; }
        }

        public override ImmutableArray<Symbol> CandidateSymbols
        {
            get { return _originalDefinition.CandidateSymbols; }
        }

        internal override LookupResultKind ResultKind
        {
            get { return _originalDefinition.ResultKind; }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            return _originalDefinition.GetUseSiteDiagnostic();
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = this.ComputeHashCode();
            }
            return _hashCode;
        }
    }

    internal sealed class ConstructedErrorTypeSymbol : SubstitutedErrorTypeSymbol
    {
        private readonly ErrorTypeSymbol _constructedFrom;
        private readonly ImmutableArray<TypeSymbol> _typeArguments;
        private readonly bool _hasTypeArgumentsCustomModifiers;
        private readonly TypeMap _map;

        public ConstructedErrorTypeSymbol(ErrorTypeSymbol constructedFrom, ImmutableArray<TypeWithModifiers> typeArguments) :
            base((ErrorTypeSymbol)constructedFrom.OriginalDefinition)
        {
            _constructedFrom = constructedFrom;
            _typeArguments = typeArguments.ToTypes(out _hasTypeArgumentsCustomModifiers);
            _map = new TypeMap(constructedFrom.ContainingType, constructedFrom.OriginalDefinition.TypeParameters, typeArguments);
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _constructedFrom.TypeParameters; }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get { return _typeArguments; }
        }

        internal override bool HasTypeArgumentsCustomModifiers
        {
            get
            {
                return _hasTypeArgumentsCustomModifiers;
            }
        }

        internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers
        {
            get
            {
                if (_hasTypeArgumentsCustomModifiers)
                {
                    return _map.GetTypeArgumentsCustomModifiersFor(_constructedFrom.OriginalDefinition);
                }

                return CreateEmptyTypeArgumentsCustomModifiers();
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return _constructedFrom; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _constructedFrom.ContainingSymbol; }
        }

        internal override TypeMap TypeSubstitution
        {
            get { return _map; }
        }
    }

    internal sealed class SubstitutedNestedErrorTypeSymbol : SubstitutedErrorTypeSymbol
    {
        private readonly NamedTypeSymbol _containingSymbol;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly TypeMap _map;

        public SubstitutedNestedErrorTypeSymbol(NamedTypeSymbol containingSymbol, ErrorTypeSymbol originalDefinition) :
            base(originalDefinition)
        {
            _containingSymbol = containingSymbol;
            _map = containingSymbol.TypeSubstitution.WithAlphaRename(originalDefinition, this, out _typeParameters);
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get { return this.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>(); }
        }

        internal override bool HasTypeArgumentsCustomModifiers
        {
            get
            {
                return false;
            }
        }

        internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers
        {
            get
            {
                return CreateEmptyTypeArgumentsCustomModifiers();
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        internal override TypeMap TypeSubstitution
        {
            get { return _map; }
        }
    }
}

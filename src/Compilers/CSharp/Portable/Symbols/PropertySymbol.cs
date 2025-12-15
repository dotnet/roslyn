// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a property or indexer.
    /// </summary>
    internal abstract partial class PropertySymbol : Symbol, IPropertySymbolInternal
    {
        /// <summary>
        /// As a performance optimization, cache parameter types and refkinds - overload resolution uses them a lot.
        /// </summary>
        private ParameterSignature _lazyParameterSignature;

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        internal PropertySymbol()
        {
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual PropertySymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        protected sealed override Symbol OriginalSymbolDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        /// <summary>
        /// If a property is annotated with `[MemberNotNull(...)]` attributes, returns the list of members
        /// listed in those attributes.
        /// Otherwise, an empty array.
        /// </summary>
        internal virtual ImmutableArray<string> NotNullMembers => ImmutableArray<string>.Empty;

        internal virtual ImmutableArray<string> NotNullWhenTrueMembers => ImmutableArray<string>.Empty;

        internal virtual ImmutableArray<string> NotNullWhenFalseMembers => ImmutableArray<string>.Empty;

        /// <summary>
        /// Indicates whether or not the property returns by reference
        /// </summary>
        public bool ReturnsByRef { get { return this.RefKind == RefKind.Ref; } }

        /// <summary>
        /// Indicates whether or not the property returns a readonly reference
        /// </summary>
        public bool ReturnsByRefReadonly { get { return this.RefKind == RefKind.RefReadOnly; } }

        /// <summary>
        /// Gets the ref kind of the property.
        /// </summary>
        public abstract RefKind RefKind { get; }

        /// <summary>
        /// The type of the property along with its annotations.
        /// </summary>
        public abstract TypeWithAnnotations TypeWithAnnotations { get; }

        /// <summary>
        /// The type of the property.
        /// </summary>
        public TypeSymbol Type => TypeWithAnnotations.Type;

        /// <summary>
        /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
        /// </summary>
        public abstract ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        /// <summary>
        /// The parameters of this property. If this property has no parameters, returns
        /// an empty list. Parameters are only present on indexers, or on some properties
        /// imported from a COM interface.
        /// </summary>
        public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

        /// <summary>
        /// Optimization: in many cases, the parameter count (fast) is sufficient and we
        /// don't need the actual parameter symbols (slow).
        /// </summary>
        internal int ParameterCount
        {
            get
            {
                return this.Parameters.Length;
            }
        }

        internal ImmutableArray<TypeWithAnnotations> ParameterTypesWithAnnotations
        {
            get
            {
                ParameterSignature.PopulateParameterSignature(this.Parameters, ref _lazyParameterSignature);
                return _lazyParameterSignature.parameterTypesWithAnnotations;
            }
        }

        internal ImmutableArray<RefKind> ParameterRefKinds
        {
            get
            {
                ParameterSignature.PopulateParameterSignature(this.Parameters, ref _lazyParameterSignature);
                return _lazyParameterSignature.parameterRefKinds;
            }
        }

        /// <summary>
        /// Returns true if this symbol requires an instance reference as the implicit receiver. This is false if the symbol is static.
        /// </summary>
        public virtual bool RequiresInstanceReceiver => !IsStatic;

        /// <summary>
        /// Returns whether the property is really an indexer.
        /// </summary>
        /// <remarks>
        /// In source, we regard a property as an indexer if it is declared with an IndexerDeclarationSyntax.
        /// From metadata, we regard a property if it has parameters and is a default member of the containing
        /// type.
        /// CAVEAT: To ensure that this property (and indexer Names) roundtrip, source properties are not
        /// indexers if they are explicit interface implementations (since they will not be marked as default
        /// members in metadata).
        /// </remarks>
        public abstract bool IsIndexer { get; }

        /// <summary>
        /// True if this an indexed property; that is, a property with parameters
        /// within a [ComImport] type.
        /// </summary>
        public virtual bool IsIndexedProperty
        {
            get { return false; }
        }

        /// <summary>
        /// True if this is a read-only property; that is, a property with no set accessor.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                var property = (PropertySymbol)this.GetLeastOverriddenMember(this.ContainingType);
                return (object)property.SetMethod == null;
            }
        }

        /// <summary>
        /// True if this is a write-only property; that is, a property with no get accessor.
        /// </summary>
        public bool IsWriteOnly
        {
            get
            {
                var property = (PropertySymbol)this.GetLeastOverriddenMember(this.ContainingType);
                return (object)property.GetMethod == null;
            }
        }

        /// <summary>
        /// Returns true if this property is required to be set in an object initializer on object creation.
        /// </summary>
        internal abstract bool IsRequired { get; }

        /// <summary>
        /// True if the property itself is excluded from code coverage instrumentation.
        /// True for source properties marked with <see cref="AttributeDescription.ExcludeFromCodeCoverageAttribute"/>.
        /// </summary>
        internal virtual bool IsDirectlyExcludedFromCodeCoverage { get => false; }

        /// <summary>
        /// True if this symbol has a special name (metadata flag SpecialName is set).
        /// </summary>
        internal abstract bool HasSpecialName { get; }

        /// <summary>
        /// The 'get' accessor of the property, or null if the property is write-only.
        /// </summary>
        public abstract MethodSymbol GetMethod
        {
            get;
        }

        /// <summary>
        /// The 'set' accessor of the property, or null if the property is read-only.
        /// </summary>
        public abstract MethodSymbol SetMethod
        {
            get;
        }

        internal abstract Cci.CallingConvention CallingConvention { get; }

        internal abstract bool MustCallMethodsDirectly { get; }

        internal abstract bool HasUnscopedRefAttribute { get; }

        internal abstract override bool IsCallerUnsafe { get; }

        /// <summary>
        /// Returns the overridden property, or null.
        /// </summary>
        public PropertySymbol OverriddenProperty
        {
            get
            {
                if (this.IsOverride)
                {
                    if (IsDefinition)
                    {
                        return (PropertySymbol)OverriddenOrHiddenMembers.GetOverriddenMember();
                    }

                    return (PropertySymbol)OverriddenOrHiddenMembersResult.GetOverriddenMember(this, OriginalDefinition.OverriddenProperty);
                }
                return null;
            }
        }

        internal virtual OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                return this.MakeOverriddenOrHiddenMembers();
            }
        }

        internal bool HidesBasePropertiesByName
        {
            get
            {
                // Dev10 gives preference to the getter.
                MethodSymbol accessor = GetMethod ?? SetMethod;

                // false is a reasonable default if there are no accessors (e.g. not done typing).
                return (object)accessor != null && accessor.HidesBaseMethodsByName;
            }
        }

        internal PropertySymbol GetLeastOverriddenProperty(NamedTypeSymbol accessingTypeOpt)
        {
            accessingTypeOpt = accessingTypeOpt?.OriginalDefinition;
            PropertySymbol p = this;
            while (p.IsOverride && !p.HidesBasePropertiesByName)
            {
                // We might not be able to access the overridden method. For example,
                // 
                //   .assembly A
                //   {
                //      InternalsVisibleTo("B")
                //      public class A { internal virtual int P { get; } }
                //   }
                // 
                //   .assembly B
                //   {
                //      InternalsVisibleTo("C")
                //      public class B : A { internal override int P { get; } }
                //   }
                // 
                //   .assembly C
                //   {
                //      public class C : B { ... new B().P ... }       // A.P is not accessible from here
                //   }
                //
                // See InternalsVisibleToAndStrongNameTests: IvtVirtualCall1, IvtVirtualCall2, IvtVirtual_ParamsAndDynamic.
                PropertySymbol overridden = p.OverriddenProperty;
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                if ((object)overridden == null ||
                    (accessingTypeOpt is { } && !AccessCheck.IsSymbolAccessible(overridden, accessingTypeOpt, ref discardedUseSiteInfo)))
                {
                    break;
                }

                p = overridden;
            }

            return p;
        }

        /// <summary>
        /// Source: Was the member name qualified with a type name?
        /// Metadata: Is the member an explicit implementation?
        /// </summary>
        /// <remarks>
        /// Will not always agree with ExplicitInterfaceImplementations.Any()
        /// (e.g. if binding of the type part of the name fails).
        /// </remarks>
        internal virtual bool IsExplicitInterfaceImplementation
        {
            get { return ExplicitInterfaceImplementations.Any(); }
        }

        /// <summary>
        /// Returns interface properties explicitly implemented by this property.
        /// </summary>
        /// <remarks>
        /// Properties imported from metadata can explicitly implement more than one property.
        /// </remarks>
        public abstract ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations { get; }

#nullable enable
        internal virtual PropertySymbol? PartialImplementationPart => null;
        internal virtual PropertySymbol? PartialDefinitionPart => null;

        IPropertySymbolInternal? IPropertySymbolInternal.PartialImplementationPart => PartialImplementationPart;
        IPropertySymbolInternal? IPropertySymbolInternal.PartialDefinitionPart => PartialDefinitionPart;
#nullable disable

        /// <summary>
        /// Gets the kind of this symbol.
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Property;
            }
        }

        internal int OverloadResolutionPriority
        {
            get
            {
                if (!CanHaveOverloadResolutionPriority)
                {
                    return 0;
                }

                return TryGetOverloadResolutionPriority();
            }
        }

        internal abstract int TryGetOverloadResolutionPriority();

        internal bool CanHaveOverloadResolutionPriority
            => !IsOverride && !IsExplicitInterfaceImplementation && (IsIndexer || IsIndexedProperty || this.IsExtensionBlockMember());

        /// <summary>
        /// Implements visitor pattern.
        /// </summary>
        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitProperty(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitProperty(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitProperty(this);
        }

        internal PropertySymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return newOwner.IsDefinition ? this : new SubstitutedPropertySymbol(newOwner as SubstitutedNamedTypeSymbol, this);
        }

        #region Use-Site Diagnostics

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (this.IsDefinition)
            {
                return new UseSiteInfo<AssemblySymbol>(PrimaryDependency);
            }

            return this.OriginalDefinition.GetUseSiteInfo();
        }

        internal bool CalculateUseSiteDiagnostic(ref UseSiteInfo<AssemblySymbol> result)
        {
            Debug.Assert(this.IsDefinition);

            // Check return type, custom modifiers and parameters:
            if (DeriveUseSiteInfoFromType(ref result, this.TypeWithAnnotations, AllowedRequiredModifierType.None) ||
                DeriveUseSiteInfoFromCustomModifiers(ref result, this.RefCustomModifiers, AllowedRequiredModifierType.System_Runtime_InteropServices_InAttribute) ||
                DeriveUseSiteInfoFromParameters(ref result, this.Parameters))
            {
                return true;
            }

            // If the member is in an assembly with unified references, 
            // we check if its definition depends on a type from a unified reference.
            if (this.ContainingModule.HasUnifiedReferences)
            {
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                DiagnosticInfo diagnosticInfo = result.DiagnosticInfo;
                if (this.TypeWithAnnotations.GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this.RefCustomModifiers, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this.Parameters, this, ref unificationCheckedTypes))
                {
                    result = result.AdjustDiagnosticInfo(diagnosticInfo);
                    return true;
                }

                result = result.AdjustDiagnosticInfo(diagnosticInfo);
            }

            return false;
        }

        /// <summary>
        /// Returns true if the error code is highest priority while calculating use site error for this symbol. 
        /// </summary>
        protected sealed override bool IsHighestPriorityUseSiteErrorCode(int code) => code is (int)ErrorCode.ERR_UnsupportedCompilerFeature or (int)ErrorCode.ERR_BindToBogus;

        public sealed override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = GetUseSiteInfo().DiagnosticInfo;
                return (object)info != null && info.Code is (int)ErrorCode.ERR_BindToBogus or (int)ErrorCode.ERR_UnsupportedCompilerFeature;
            }
        }

        #endregion

        protected sealed override ISymbol CreateISymbol()
        {
            return new PublicModel.PropertySymbol(this);
        }

        #region Equality

        public override bool Equals(Symbol symbol, TypeCompareKind compareKind)
        {
            PropertySymbol other = symbol as PropertySymbol;

            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is NativeIntegerPropertySymbol nps)
            {
                return nps.Equals(this, compareKind);
            }

            // This checks if the property have the same definition and the type parameters on the containing types have been
            // substituted in the same way.
            return TypeSymbol.Equals(this.ContainingType, other.ContainingType, compareKind) && ReferenceEquals(this.OriginalDefinition, other.OriginalDefinition);
        }

        public override int GetHashCode()
        {
            int hash = 1;
            hash = Hash.Combine(this.ContainingType, hash);
            hash = Hash.Combine(this.Name, hash);
            hash = Hash.Combine(hash, this.ParameterCount);
            return hash;
        }

        #endregion Equality
    }
}

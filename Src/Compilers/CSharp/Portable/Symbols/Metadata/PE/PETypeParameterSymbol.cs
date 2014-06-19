// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all generic type parameters imported from a PE/module.
    /// </summary>
    /// <remarks></remarks>
    internal sealed class PETypeParameterSymbol
        : TypeParameterSymbol
    {
        private readonly Symbol containingSymbol; // Could be PENamedType or a PEMethod
        private readonly GenericParameterHandle handle;

        #region Metadata
        private readonly string name;
        private readonly ushort ordinal; // 0 for first, 1 for second, ...
        private readonly GenericParameterAttributes flags;
        #endregion

        private TypeParameterBounds lazyBounds = TypeParameterBounds.Unset;

        /// <summary>
        /// First error calculating bounds.
        /// </summary>
        private DiagnosticInfo lazyBoundsErrorInfo = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state.

        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        internal PETypeParameterSymbol(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol definingNamedType,
            ushort ordinal,
            GenericParameterHandle handle)
            : this(moduleSymbol, (Symbol)definingNamedType, ordinal, handle)
        {
        }

        internal PETypeParameterSymbol(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol definingMethod,
            ushort ordinal,
            GenericParameterHandle handle)
            : this(moduleSymbol, (Symbol)definingMethod, ordinal, handle)
        {
        }

        private PETypeParameterSymbol(
            PEModuleSymbol moduleSymbol,
            Symbol definingSymbol,
            ushort ordinal,
            GenericParameterHandle handle)
        {
            Debug.Assert((object)moduleSymbol != null);
            Debug.Assert((object)definingSymbol != null);
            Debug.Assert(ordinal >= 0);
            Debug.Assert(!handle.IsNil);

            this.containingSymbol = definingSymbol;

            GenericParameterAttributes flags = 0;

            try
            {
                moduleSymbol.Module.GetGenericParamPropsOrThrow(handle, out this.name, out flags);
            }
            catch (BadImageFormatException)
            {
                if ((object)this.name == null)
                {
                    this.name = string.Empty;
                }

                lazyBoundsErrorInfo = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            // Clear the '.ctor' flag if both '.ctor' and 'valuetype' are
            // set since '.ctor' is redundant in that case.
            this.flags = ((flags & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0) ? flags : (flags & ~GenericParameterAttributes.DefaultConstructorConstraint);

            this.ordinal = ordinal;
            this.handle = handle;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return this.ContainingSymbol.Kind == SymbolKind.Method
                    ? TypeParameterKind.Method
                    : TypeParameterKind.Type;
            }
        }

        public override int Ordinal
        {
            get { return this.ordinal; }
        }

        public override string Name
        {
            get
            {
                return this.name;
            }
        }

        internal GenericParameterHandle Handle
        {
            get
            {
                return this.handle;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.containingSymbol; }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.containingSymbol.ContainingAssembly;
            }
        }

        private ImmutableArray<TypeSymbol> GetDeclaredConstraintTypes()
        {
            PEMethodSymbol containingMethod = null;
            PENamedTypeSymbol containingType;

            if (containingSymbol.Kind == SymbolKind.Method)
            {
                containingMethod = (PEMethodSymbol)containingSymbol;
                containingType = (PENamedTypeSymbol)containingMethod.ContainingSymbol;
            }
            else
            {
                containingType = (PENamedTypeSymbol)containingSymbol;
            }

            var moduleSymbol = containingType.ContainingPEModule;

            Handle[] constraints;

            try
            {
                constraints = moduleSymbol.Module.GetGenericParamConstraintsOrThrow(this.handle);
            }
            catch (BadImageFormatException)
            {
                constraints = null;
                Interlocked.CompareExchange(ref lazyBoundsErrorInfo, new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this), CSDiagnosticInfo.EmptyErrorInfo);
            }

            if (constraints != null && constraints.Length > 0)
            {
                var symbolsBuilder = ArrayBuilder<TypeSymbol>.GetInstance();
                MetadataDecoder tokenDecoder;

                if ((object)containingMethod != null)
                {
                    tokenDecoder = new MetadataDecoder(moduleSymbol, containingMethod);
                }
                else
                {
                    tokenDecoder = new MetadataDecoder(moduleSymbol, containingType);
                }

                foreach (var constraint in constraints)
                {
                    TypeSymbol typeSymbol = tokenDecoder.GetTypeOfToken(constraint);

                    // Drop 'System.Object' constraint type.
                    if (typeSymbol.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Object)
                    {
                        continue;
                    }

                    // Drop 'System.ValueType' constraint type if the 'valuetype' constraint was also specified.
                    if (((this.flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) &&
                        (typeSymbol.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_ValueType))
                    {
                        continue;
                    }

                    symbolsBuilder.Add(typeSymbol);
                }

                return symbolsBuilder.ToImmutableAndFree();
            }
            else
            {
                return ImmutableArray<TypeSymbol>.Empty;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return containingSymbol.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                return (this.flags & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return (this.flags & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return (this.flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return (VarianceKind)(this.flags & GenericParameterAttributes.VarianceMask);
            }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            if (ReferenceEquals(this.lazyBounds, TypeParameterBounds.Unset))
            {
                var typeParameters = (this.containingSymbol.Kind == SymbolKind.Method) ?
                    ((PEMethodSymbol)this.containingSymbol).TypeParameters :
                    ((PENamedTypeSymbol)this.containingSymbol).TypeParameters;
                EnsureAllConstraintsAreResolved(typeParameters);
            }
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.ConstraintTypes : ImmutableArray<TypeSymbol>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.Interfaces : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.EffectiveBaseClass : this.GetDefaultBaseType();
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.DeducedBaseType : this.GetDefaultBaseType();
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (this.lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                containingPEModuleSymbol.LoadCustomAttributes(this.Handle, ref this.lazyCustomAttributes);
            }
            return this.lazyCustomAttributes;
        }

        private TypeParameterBounds GetBounds(ConsList<TypeParameterSymbol> inProgress)
        {
            Debug.Assert(!inProgress.ContainsReference(this));
            Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            if (ReferenceEquals(this.lazyBounds, TypeParameterBounds.Unset))
            {
                var constraintTypes = GetDeclaredConstraintTypes();
                Debug.Assert(!constraintTypes.IsDefault);

                var diagnostics = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
                bool inherited = (this.containingSymbol.Kind == SymbolKind.Method) && ((MethodSymbol)this.containingSymbol).IsOverride;
                var bounds = this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, inherited, currentCompilation: null,
                                                diagnosticsBuilder: diagnostics, useSiteDiagnosticsBuilder: ref useSiteDiagnosticsBuilder);
                DiagnosticInfo errorInfo = null;

                if (diagnostics.Count > 0)
                {
                    errorInfo = diagnostics[0].DiagnosticInfo;
                }
                else if (useSiteDiagnosticsBuilder != null && useSiteDiagnosticsBuilder.Count > 0)
                {
                    foreach (var diag in useSiteDiagnosticsBuilder)
                    {
                        if (diag.DiagnosticInfo.Severity == DiagnosticSeverity.Error)
                        {
                            errorInfo = diag.DiagnosticInfo;
                            break;
                        }
                        else if ((object)errorInfo == null)
                        {
                            errorInfo = diag.DiagnosticInfo;
                        }
                    }
                }

                diagnostics.Free();

                Interlocked.CompareExchange(ref this.lazyBoundsErrorInfo, errorInfo, CSDiagnosticInfo.EmptyErrorInfo);
                Interlocked.CompareExchange(ref this.lazyBounds, bounds, TypeParameterBounds.Unset);
            }

            Debug.Assert(!ReferenceEquals(this.lazyBoundsErrorInfo, CSDiagnosticInfo.EmptyErrorInfo));
            return this.lazyBounds;
        }

        internal override DiagnosticInfo GetConstraintsUseSiteErrorInfo()
        {
            EnsureAllConstraintsAreResolved();
            Debug.Assert(!ReferenceEquals(this.lazyBoundsErrorInfo, CSDiagnosticInfo.EmptyErrorInfo));
            return lazyBoundsErrorInfo;
        }

        private NamedTypeSymbol GetDefaultBaseType()
        {
            return this.ContainingAssembly.GetSpecialType(SpecialType.System_Object);
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Symbol _containingSymbol; // Could be PENamedType or a PEMethod
        private readonly GenericParameterHandle _handle;

        #region Metadata
        private readonly string _name;
        private readonly ushort _ordinal; // 0 for first, 1 for second, ...
        private readonly GenericParameterAttributes _flags;
        #endregion

        private TypeParameterBounds _lazyBounds = TypeParameterBounds.Unset;

        /// <summary>
        /// First error calculating bounds.
        /// </summary>
        private DiagnosticInfo _lazyBoundsErrorInfo = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state.

        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

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

            _containingSymbol = definingSymbol;

            GenericParameterAttributes flags = 0;

            try
            {
                moduleSymbol.Module.GetGenericParamPropsOrThrow(handle, out _name, out flags);
            }
            catch (BadImageFormatException)
            {
                if ((object)_name == null)
                {
                    _name = string.Empty;
                }

                _lazyBoundsErrorInfo = new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this);
            }

            // Clear the '.ctor' flag if both '.ctor' and 'valuetype' are
            // set since '.ctor' is redundant in that case.
            _flags = ((flags & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0) ? flags : (flags & ~GenericParameterAttributes.DefaultConstructorConstraint);

            _ordinal = ordinal;
            _handle = handle;
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
            get { return _ordinal; }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal GenericParameterHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _containingSymbol.ContainingAssembly;
            }
        }

        private ImmutableArray<TypeSymbolWithAnnotations> GetDeclaredConstraintTypes()
        {
            PEMethodSymbol containingMethod = null;
            PENamedTypeSymbol containingType;

            if (_containingSymbol.Kind == SymbolKind.Method)
            {
                containingMethod = (PEMethodSymbol)_containingSymbol;
                containingType = (PENamedTypeSymbol)containingMethod.ContainingSymbol;
            }
            else
            {
                containingType = (PENamedTypeSymbol)_containingSymbol;
            }

            var moduleSymbol = containingType.ContainingPEModule;

            EntityHandle[] constraints;

            try
            {
                constraints = moduleSymbol.Module.GetGenericParamConstraintsOrThrow(_handle);
            }
            catch (BadImageFormatException)
            {
                constraints = null;
                Interlocked.CompareExchange(ref _lazyBoundsErrorInfo, new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, this), CSDiagnosticInfo.EmptyErrorInfo);
            }

            if (constraints != null && constraints.Length > 0)
            {
                var symbolsBuilder = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
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
                    if (((_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) &&
                        (typeSymbol.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_ValueType))
                    {
                        continue;
                    }

                    symbolsBuilder.Add(TypeSymbolWithAnnotations.Create(typeSymbol));
                }

                return symbolsBuilder.ToImmutableAndFree();
            }
            else
            {
                return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _containingSymbol.Locations;
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
                return (_flags & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return (_flags & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return (VarianceKind)(_flags & GenericParameterAttributes.VarianceMask);
            }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            if (ReferenceEquals(_lazyBounds, TypeParameterBounds.Unset))
            {
                var typeParameters = (_containingSymbol.Kind == SymbolKind.Method) ?
                    ((PEMethodSymbol)_containingSymbol).TypeParameters :
                    ((PENamedTypeSymbol)_containingSymbol).TypeParameters;
                EnsureAllConstraintsAreResolved(typeParameters);
            }
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.ConstraintTypes : ImmutableArray<TypeSymbolWithAnnotations>.Empty;
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
            if (_lazyCustomAttributes.IsDefault)
            {
                var containingPEModuleSymbol = (PEModuleSymbol)this.ContainingModule;
                containingPEModuleSymbol.LoadCustomAttributes(this.Handle, ref _lazyCustomAttributes);
            }
            return _lazyCustomAttributes;
        }

        private TypeParameterBounds GetBounds(ConsList<TypeParameterSymbol> inProgress)
        {
            Debug.Assert(!inProgress.ContainsReference(this));
            Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            if (ReferenceEquals(_lazyBounds, TypeParameterBounds.Unset))
            {
                var constraintTypes = GetDeclaredConstraintTypes();
                Debug.Assert(!constraintTypes.IsDefault);

                var diagnostics = ArrayBuilder<TypeParameterDiagnosticInfo>.GetInstance();
                ArrayBuilder<TypeParameterDiagnosticInfo> useSiteDiagnosticsBuilder = null;
                bool inherited = (_containingSymbol.Kind == SymbolKind.Method) && ((MethodSymbol)_containingSymbol).IsOverride;
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

                Interlocked.CompareExchange(ref _lazyBoundsErrorInfo, errorInfo, CSDiagnosticInfo.EmptyErrorInfo);
                Interlocked.CompareExchange(ref _lazyBounds, bounds, TypeParameterBounds.Unset);
            }

            Debug.Assert(!ReferenceEquals(_lazyBoundsErrorInfo, CSDiagnosticInfo.EmptyErrorInfo));
            return _lazyBounds;
        }

        internal override DiagnosticInfo GetConstraintsUseSiteErrorInfo()
        {
            EnsureAllConstraintsAreResolved();
            Debug.Assert(!ReferenceEquals(_lazyBoundsErrorInfo, CSDiagnosticInfo.EmptyErrorInfo));
            return _lazyBoundsErrorInfo;
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

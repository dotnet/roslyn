// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a method in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another MethodSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying MethodSymbol, cannot be another RetargetingMethodSymbol.
        /// </summary>
        private readonly MethodSymbol _underlyingMethod;

        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        private ImmutableArray<ParameterSymbol> _lazyParameters;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        /// <summary>
        /// Retargeted return type custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyReturnTypeCustomAttributes;

        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private TypeSymbolWithAnnotations _lazyReturnType;

        public RetargetingMethodSymbol(RetargetingModuleSymbol retargetingModule, MethodSymbol underlyingMethod)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingMethod != null);
            Debug.Assert(!(underlyingMethod is RetargetingMethodSymbol));

            _retargetingModule = retargetingModule;
            _underlyingMethod = underlyingMethod;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return _retargetingModule.RetargetingTranslator;
            }
        }

        public MethodSymbol UnderlyingMethod
        {
            get
            {
                return _underlyingMethod;
            }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override bool IsVararg
        {
            get
            {
                return _underlyingMethod.IsVararg;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return _underlyingMethod.IsGenericMethod;
            }
        }

        public override int Arity
        {
            get
            {
                return _underlyingMethod.Arity;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (_lazyTypeParameters.IsDefault)
                {
                    if (!IsGenericMethod)
                    {
                        _lazyTypeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters,
                            this.RetargetingTranslator.Retarget(_underlyingMethod.TypeParameters), default(ImmutableArray<TypeParameterSymbol>));
                    }
                }

                return _lazyTypeParameters;
            }
        }

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get
            {
                if (IsGenericMethod)
                {
                    return this.TypeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations);
                }
                else
                {
                    return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
                }
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return _underlyingMethod.ReturnsVoid;
            }
        }

        internal override RefKind RefKind
        {
            get
            {
                return _underlyingMethod.RefKind;
            }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get
            {
                if ((object)_lazyReturnType == null)
                {
                    _lazyReturnType = this.RetargetingTranslator.Retarget(_underlyingMethod.ReturnType, RetargetOptions.RetargetPrimitiveTypesByTypeCode, this.ContainingType);
                }
                return _lazyReturnType;
            }
        }

        internal override int ParameterCount
        {
            get { return _underlyingMethod.ParameterCount; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyParameters, this.RetargetParameters(), default(ImmutableArray<ParameterSymbol>));
                }

                return _lazyParameters;
            }
        }

        private ImmutableArray<ParameterSymbol> RetargetParameters()
        {
            var list = _underlyingMethod.Parameters;
            int count = list.Length;

            if (count == 0)
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }
            else
            {
                ParameterSymbol[] parameters = new ParameterSymbol[count];

                for (int i = 0; i < count; i++)
                {
                    parameters[i] = new RetargetingMethodParameterSymbol(this, list[i]);
                }

                return parameters.AsImmutableOrNull();
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                var associatedPropertyOrEvent = _underlyingMethod.AssociatedSymbol;
                return (object)associatedPropertyOrEvent == null ? null : this.RetargetingTranslator.Retarget(associatedPropertyOrEvent);
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return _underlyingMethod.IsExtensionMethod;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return _underlyingMethod.HidesBaseMethodsByName;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingMethod.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingMethod.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingMethod.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingMethod.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingMethod.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _underlyingMethod.IsVirtual;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return _underlyingMethod.IsAsync;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return _underlyingMethod.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingMethod.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return _underlyingMethod.IsExtern;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _underlyingMethod.IsImplicitlyDeclared;
            }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return _underlyingMethod.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return _underlyingMethod.IsMetadataFinal;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return _underlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return _underlyingMethod.RequiresSecurityObject;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return _underlyingMethod.GetDllImportData();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return _retargetingModule.RetargetingTranslator.Retarget(_underlyingMethod.ReturnValueMarshallingInformation);
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return _underlyingMethod.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return _underlyingMethod.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _underlyingMethod.GetAppliedConditionalSymbols();
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingMethod.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingMethod.GetCustomAttributesToEmit(compilationState));
        }

        // Get return type attributes
        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingMethod.GetReturnTypeAttributes(), ref _lazyReturnTypeCustomAttributes);
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingMethod.ObsoleteAttributeData;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingMethod.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingMethod.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return _underlyingMethod.ImplementationAttributes;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return _underlyingMethod.MethodKind;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return _underlyingMethod.CallingConvention;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return _underlyingMethod.IsExplicitInterfaceImplementation; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<MethodSymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<MethodSymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = _underlyingMethod.ExplicitInterfaceImplementations;

            if (impls.IsEmpty)
            {
                return impls;
            }

            // CONSIDER: we could skip the builder until the first time we see a different method after retargeting

            var builder = ArrayBuilder<MethodSymbol>.GetInstance();

            for (int i = 0; i < impls.Length; i++)
            {
                var retargeted = this.RetargetingTranslator.Retarget(impls[i], MemberSignatureComparer.RetargetedExplicitImplementationComparer);
                if ((object)retargeted != null)
                {
                    builder.Add(retargeted);
                }
            }

            return builder.ToImmutableAndFree();
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                _lazyUseSiteDiagnostic = result;
            }

            return _lazyUseSiteDiagnostic;
        }

        internal override bool IsAccessCheckedOnOverride
        {
            get
            {
                return _underlyingMethod.IsAccessCheckedOnOverride;
            }
        }

        internal override bool IsExternal
        {
            get
            {
                return _underlyingMethod.IsExternal;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingMethod.HasRuntimeSpecialName;
            }
        }


        internal override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return _underlyingMethod.ReturnValueIsMarshalledExplicitly;
            }
        }

        internal override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return _underlyingMethod.ReturnValueMarshallingDescriptor;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            // retargeting symbols refer to a symbol from another compilation, they don't define locals in the current compilation
            throw ExceptionUtilities.Unreachable;
        }
    }
}

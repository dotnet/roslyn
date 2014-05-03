// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly RetargetingModuleSymbol retargetingModule;

        /// <summary>
        /// The underlying MethodSymbol, cannot be another RetargetingMethodSymbol.
        /// </summary>
        private readonly MethodSymbol underlyingMethod;

        private ImmutableArray<TypeParameterSymbol> lazyTypeParameters;

        private ImmutableArray<ParameterSymbol> lazyParameters;

        private ImmutableArray<CustomModifier> lazyCustomModifiers;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        /// <summary>
        /// Retargeted return type custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyReturnTypeCustomAttributes;

        private ImmutableArray<MethodSymbol> lazyExplicitInterfaceImplementations;
        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        private TypeSymbol lazyReturnType;

        public RetargetingMethodSymbol(RetargetingModuleSymbol retargetingModule, MethodSymbol underlyingMethod)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert((object)underlyingMethod != null);
            Debug.Assert(!(underlyingMethod is RetargetingMethodSymbol));

            this.retargetingModule = retargetingModule;
            this.underlyingMethod = underlyingMethod;
        }

        private RetargetingModuleSymbol.RetargetingSymbolTranslator RetargetingTranslator
        {
            get
            {
                return retargetingModule.RetargetingTranslator;
            }
        }

        public MethodSymbol UnderlyingMethod
        {
            get
            {
                return this.underlyingMethod;
            }
        }

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override bool IsVararg
        {
            get
            {
                return this.underlyingMethod.IsVararg;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return this.underlyingMethod.IsGenericMethod;
            }
        }

        public override int Arity
        {
            get
            {
                return this.underlyingMethod.Arity;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (lazyTypeParameters.IsDefault)
                {
                    if (!IsGenericMethod)
                    {
                        lazyTypeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
                    }
                    else
                    {
                        ImmutableInterlocked.InterlockedCompareExchange(ref lazyTypeParameters,
                            this.RetargetingTranslator.Retarget(this.underlyingMethod.TypeParameters), default(ImmutableArray<TypeParameterSymbol>));
                    }
                }

                return lazyTypeParameters;
            }
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                if (IsGenericMethod)
                {
                    return StaticCast<TypeSymbol>.From(this.TypeParameters);
                }
                else
                {
                    return ImmutableArray<TypeSymbol>.Empty;
                }
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return this.underlyingMethod.ReturnsVoid;
            }
        }

        public override TypeSymbol ReturnType
        {
            get
            {
                if ((object)this.lazyReturnType == null)
                {
                    var type = this.RetargetingTranslator.Retarget(this.underlyingMethod.ReturnType, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                    this.lazyReturnType = type.AsDynamicIfNoPia(this.ContainingType);
                }
                return this.lazyReturnType;
            }
        }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get
            {
                return RetargetingTranslator.RetargetModifiers(
                    underlyingMethod.ReturnTypeCustomModifiers,
                    ref lazyCustomModifiers);
            }
        }

        internal override int ParameterCount
        {
            get { return underlyingMethod.ParameterCount; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref lazyParameters, this.RetargetParameters(), default(ImmutableArray<ParameterSymbol>));
                }

                return lazyParameters;
            }
        }

        private ImmutableArray<ParameterSymbol> RetargetParameters()
        {
            var list = this.underlyingMethod.Parameters;
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
                var associatedPropertyOrEvent = this.underlyingMethod.AssociatedSymbol;
                return (object)associatedPropertyOrEvent == null ? null : this.RetargetingTranslator.Retarget(associatedPropertyOrEvent);
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return this.underlyingMethod.IsExtensionMethod;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return this.underlyingMethod.HidesBaseMethodsByName;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(this.underlyingMethod.ContainingSymbol);
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.underlyingMethod.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return this.underlyingMethod.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return this.underlyingMethod.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return this.underlyingMethod.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return this.underlyingMethod.IsVirtual;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return this.underlyingMethod.IsAsync;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return this.underlyingMethod.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return this.underlyingMethod.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return this.underlyingMethod.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return this.underlyingMethod.IsExtern;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return underlyingMethod.IsImplicitlyDeclared;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get
            {
                return underlyingMethod.GenerateDebugInfo;
            }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return this.underlyingMethod.IsMetadataVirtual(ignoreInterfaceImplementationChanges);
        }

        internal sealed override bool IsMetadataFinal()
        {
            return this.underlyingMethod.IsMetadataFinal();
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return underlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges);
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return this.underlyingMethod.RequiresSecurityObject;
            }
        }

        public override DllImportData GetDllImportData()
        {
            return this.underlyingMethod.GetDllImportData();
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return this.retargetingModule.RetargetingTranslator.Retarget(this.underlyingMethod.ReturnValueMarshallingInformation);
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return this.underlyingMethod.HasDeclarativeSecurity; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return this.underlyingMethod.GetSecurityInformation();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return this.underlyingMethod.GetAppliedConditionalSymbols();
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(this.underlyingMethod.GetAttributes(), ref this.lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingTranslator.RetargetAttributes(this.underlyingMethod.GetCustomAttributesToEmit(compilationState));
        }

        // Get return type attributes
        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(this.underlyingMethod.GetReturnTypeAttributes(), ref this.lazyReturnTypeCustomAttributes);
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return underlyingMethod.ObsoleteAttributeData;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.retargetingModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return this.retargetingModule;
            }
        }

        public override string Name
        {
            get
            {
                return this.underlyingMethod.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return this.underlyingMethod.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.underlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get
            {
                return this.underlyingMethod.ImplementationAttributes;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return this.underlyingMethod.MethodKind;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return this.underlyingMethod.CallingConvention;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return this.underlyingMethod.IsExplicitInterfaceImplementation; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<MethodSymbol>));
                }
                return lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<MethodSymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = this.underlyingMethod.ExplicitInterfaceImplementations;

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
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                DiagnosticInfo result = null;
                CalculateUseSiteDiagnostic(ref result);
                lazyUseSiteDiagnostic = result;
            }

            return lazyUseSiteDiagnostic;
        }

        internal override bool IsAccessCheckedOnOverride
        {
            get
            {
                return this.underlyingMethod.IsAccessCheckedOnOverride;
            }
        }

        internal override bool IsExternal
        {
            get
            {
                return this.underlyingMethod.IsExternal;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return this.underlyingMethod.HasRuntimeSpecialName;
            }
        }

        internal override bool HasFinalFlag
        {
            get
            {
                return this.underlyingMethod.HasFinalFlag;
            }
        }

        internal override bool ReturnValueIsMarshalledExplicitly
        {
            get
            {
                return this.underlyingMethod.ReturnValueIsMarshalledExplicitly;
            }
        }

        internal override ImmutableArray<byte> ReturnValueMarshallingDescriptor
        {
            get
            {
                return this.underlyingMethod.ReturnValueMarshallingDescriptor;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}

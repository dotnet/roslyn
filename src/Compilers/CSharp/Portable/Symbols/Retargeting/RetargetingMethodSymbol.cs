// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a method in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    /// another MethodSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal sealed class RetargetingMethodSymbol : WrappedMethodSymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        /// <summary>
        /// The underlying MethodSymbol.
        /// </summary>
        private readonly MethodSymbol _underlyingMethod;

        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        private ImmutableArray<ParameterSymbol> _lazyParameters;

        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;

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

        private TypeWithAnnotations.Boxed _lazyReturnType;

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

        public RetargetingModuleSymbol RetargetingModule
        {
            get
            {
                return _retargetingModule;
            }
        }

        public override MethodSymbol UnderlyingMethod
        {
            get
            {
                return _underlyingMethod;
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

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get
            {
                if (IsGenericMethod)
                {
                    return GetTypeParametersAsTypeArguments();
                }
                else
                {
                    return ImmutableArray<TypeWithAnnotations>.Empty;
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

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                if (_lazyReturnType is null)
                {
                    Interlocked.CompareExchange(ref _lazyReturnType,
                                                new TypeWithAnnotations.Boxed(this.RetargetingTranslator.Retarget(_underlyingMethod.ReturnTypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode, this.ContainingType)),
                                                null);
                }
                return _lazyReturnType.Value;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return RetargetingTranslator.RetargetModifiers(_underlyingMethod.RefCustomModifiers, ref _lazyRefCustomModifiers);
            }
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

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingMethod.ContainingSymbol);
            }
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get
            {
                return _retargetingModule.RetargetingTranslator.Retarget(_underlyingMethod.ReturnValueMarshallingInformation);
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingMethod.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingMethod.GetCustomAttributesToEmit(moduleBuilder));
        }

        // Get return type attributes
        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingMethod.GetReturnTypeAttributes(), ref _lazyReturnTypeCustomAttributes);
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

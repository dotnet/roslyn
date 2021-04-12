// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    internal sealed class RetargetingPropertySymbol : WrappedPropertySymbol
    {
        /// <summary>
        /// Owning RetargetingModuleSymbol.
        /// </summary>
        private readonly RetargetingModuleSymbol _retargetingModule;

        //we want to compute this lazily since it may be expensive for the underlying symbol
        private ImmutableArray<PropertySymbol> _lazyExplicitInterfaceImplementations;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        private CachedUseSiteInfo<AssemblySymbol> _lazyCachedUseSiteInfo = CachedUseSiteInfo<AssemblySymbol>.Uninitialized;

        private TypeWithAnnotations.Boxed _lazyType;

        public RetargetingPropertySymbol(RetargetingModuleSymbol retargetingModule, PropertySymbol underlyingProperty)
            : base(underlyingProperty)
        {
            Debug.Assert((object)retargetingModule != null);
            Debug.Assert(!(underlyingProperty is RetargetingPropertySymbol));

            _retargetingModule = retargetingModule;
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

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                if (_lazyType is null)
                {
                    var type = this.RetargetingTranslator.Retarget(_underlyingProperty.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
                    if (type.Type.TryAsDynamicIfNoPia(this.ContainingType, out TypeSymbol asDynamic))
                    {
                        type = TypeWithAnnotations.Create(asDynamic);
                    }
                    Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(type), null);
                }
                return _lazyType.Value;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return RetargetingTranslator.RetargetModifiers(_underlyingProperty.RefCustomModifiers, ref _lazyRefCustomModifiers);
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
            var list = _underlyingProperty.Parameters;
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
                    parameters[i] = new RetargetingPropertyParameterSymbol(this, list[i]);
                }

                return parameters.AsImmutableOrNull();
            }
        }

        public override MethodSymbol GetMethod
        {
            get
            {
                return (object)_underlyingProperty.GetMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingProperty.GetMethod);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                return (object)_underlyingProperty.SetMethod == null
                    ? null
                    : this.RetargetingTranslator.Retarget(_underlyingProperty.SetMethod);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return _underlyingProperty.IsExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                if (_lazyExplicitInterfaceImplementations.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazyExplicitInterfaceImplementations,
                        this.RetargetExplicitInterfaceImplementations(),
                        default(ImmutableArray<PropertySymbol>));
                }
                return _lazyExplicitInterfaceImplementations;
            }
        }

        private ImmutableArray<PropertySymbol> RetargetExplicitInterfaceImplementations()
        {
            var impls = _underlyingProperty.ExplicitInterfaceImplementations;

            if (impls.IsEmpty)
            {
                Debug.Assert(!impls.IsDefault);
                return impls;
            }

            // CONSIDER: we could skip the builder until the first time we see a different method after retargeting

            var builder = ArrayBuilder<PropertySymbol>.GetInstance();

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

        public override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingTranslator.Retarget(_underlyingProperty.ContainingSymbol);
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

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingTranslator.GetRetargetedAttributes(_underlyingProperty.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return this.RetargetingTranslator.RetargetAttributes(_underlyingProperty.GetCustomAttributesToEmit(moduleBuilder));
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return _underlyingProperty.MustCallMethodsDirectly;
            }
        }

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            AssemblySymbol primaryDependency = PrimaryDependency;

            if (!_lazyCachedUseSiteInfo.IsInitialized)
            {
                var result = new UseSiteInfo<AssemblySymbol>(primaryDependency);
                CalculateUseSiteDiagnostic(ref result);
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, result);
            }

            return _lazyCachedUseSiteInfo.ToUseSiteInfo(primaryDependency);
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}

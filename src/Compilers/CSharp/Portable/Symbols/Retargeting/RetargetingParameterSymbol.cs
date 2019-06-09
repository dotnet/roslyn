// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a parameter of a RetargetingMethodSymbol. Essentially this is a wrapper around 
    /// another ParameterSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal abstract class RetargetingParameterSymbol : WrappedParameterSymbol
    {
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        protected RetargetingParameterSymbol(ParameterSymbol underlyingParameter)
            : base(underlyingParameter)
        {
            Debug.Assert(!(underlyingParameter is RetargetingParameterSymbol));
        }

        protected abstract RetargetingModuleSymbol RetargetingModule
        {
            get;
        }

        public sealed override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                return this.RetargetingModule.RetargetingTranslator.Retarget(_underlyingParameter.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
            }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return RetargetingModule.RetargetingTranslator.RetargetModifiers(_underlyingParameter.RefCustomModifiers, ref _lazyRefCustomModifiers);
            }
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingModule.RetargetingTranslator.Retarget(_underlyingParameter.ContainingSymbol);
            }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingModule.RetargetingTranslator.GetRetargetedAttributes(_underlyingParameter.GetAttributes(), ref _lazyCustomAttributes);
        }

        internal sealed override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return this.RetargetingModule.RetargetingTranslator.RetargetAttributes(_underlyingParameter.GetCustomAttributesToEmit(moduleBuilder));
        }

        public sealed override AssemblySymbol ContainingAssembly
        {
            get
            {
                return this.RetargetingModule.ContainingAssembly;
            }
        }

        internal sealed override ModuleSymbol ContainingModule
        {
            get
            {
                return this.RetargetingModule;
            }
        }

        internal sealed override bool HasMetadataConstantValue
        {
            get
            {
                return _underlyingParameter.HasMetadataConstantValue;
            }
        }

        internal sealed override bool IsMarshalledExplicitly
        {
            get
            {
                return _underlyingParameter.IsMarshalledExplicitly;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return this.RetargetingModule.RetargetingTranslator.Retarget(_underlyingParameter.MarshallingInformation);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return _underlyingParameter.MarshallingDescriptor;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }

    internal sealed class RetargetingMethodParameterSymbol : RetargetingParameterSymbol
    {
        /// <summary>
        /// Owning RetargetingMethodSymbol.
        /// </summary>
        private readonly RetargetingMethodSymbol _retargetingMethod;

        public RetargetingMethodParameterSymbol(RetargetingMethodSymbol retargetingMethod, ParameterSymbol underlyingParameter)
            : base(underlyingParameter)
        {
            Debug.Assert((object)retargetingMethod != null);
            _retargetingMethod = retargetingMethod;
        }

        protected override RetargetingModuleSymbol RetargetingModule
        {
            get { return _retargetingMethod.RetargetingModule; }
        }
    }

    internal sealed class RetargetingPropertyParameterSymbol : RetargetingParameterSymbol
    {
        /// <summary>
        /// Owning RetargetingPropertySymbol.
        /// </summary>
        private readonly RetargetingPropertySymbol _retargetingProperty;

        public RetargetingPropertyParameterSymbol(RetargetingPropertySymbol retargetingProperty, ParameterSymbol underlyingParameter)
            : base(underlyingParameter)
        {
            Debug.Assert((object)retargetingProperty != null);
            _retargetingProperty = retargetingProperty;
        }

        protected override RetargetingModuleSymbol RetargetingModule
        {
            get { return _retargetingProperty.RetargetingModule; }
        }
    }
}

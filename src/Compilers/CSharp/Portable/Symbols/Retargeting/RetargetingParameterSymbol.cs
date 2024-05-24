// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
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

        private TypeWithAnnotations.Boxed? _lazyTypeWithAnnotations;

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
                if (_lazyTypeWithAnnotations is null)
                {
                    Interlocked.CompareExchange(ref _lazyTypeWithAnnotations,
                        new TypeWithAnnotations.Boxed(this.RetargetingModule.RetargetingTranslator.Retarget(_underlyingParameter.TypeWithAnnotations, RetargetOptions.RetargetPrimitiveTypesByTypeCode)),
                        null);
                }

                return _lazyTypeWithAnnotations.Value;
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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <remarks>
        /// This override is done for performance reasons. Lacking the override this would redirect to 
        /// <see cref="RetargetingModuleSymbol.DeclaringCompilation"/> which returns null. The override 
        /// short circuits the overhead in <see cref="Symbol.DeclaringCompilation"/> and the extra virtual
        /// dispatch and just returns null.
        /// </remarks>
        internal sealed override CSharpCompilation? DeclaringCompilation
        {
            get { return null; }
        }

        internal sealed override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => _underlyingParameter.InterpolatedStringHandlerArgumentIndexes;

        internal override bool HasInterpolatedStringHandlerArgumentError => _underlyingParameter.HasInterpolatedStringHandlerArgumentError;
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

        internal override bool IsCallerLineNumber
        {
            get { return _underlyingParameter.IsCallerLineNumber; }
        }

        internal override bool IsCallerFilePath
        {
            get { return _underlyingParameter.IsCallerFilePath; }
        }

        internal override bool IsCallerMemberName
        {
            get { return _underlyingParameter.IsCallerMemberName; }
        }

        internal override int CallerArgumentExpressionParameterIndex
        {
            get { return _underlyingParameter.CallerArgumentExpressionParameterIndex; }
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

        internal override bool IsCallerLineNumber
        {
            get { return _underlyingParameter.IsCallerLineNumber; }
        }

        internal override bool IsCallerFilePath
        {
            get { return _underlyingParameter.IsCallerFilePath; }
        }

        internal override bool IsCallerMemberName
        {
            get { return _underlyingParameter.IsCallerMemberName; }
        }

        internal override int CallerArgumentExpressionParameterIndex
        {
            get { return _underlyingParameter.CallerArgumentExpressionParameterIndex; }
        }
    }
}

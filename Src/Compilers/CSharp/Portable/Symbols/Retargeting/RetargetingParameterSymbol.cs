// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a parameter of a RetargetingMethodSymbol. Essentially this is a wrapper around 
    /// another ParameterSymbol that is responsible for retargeting symbols from one assembly to another. 
    /// It can retarget symbols for multiple assemblies at the same time.
    /// </summary>
    internal abstract class RetargetingParameterSymbol : ParameterSymbol
    {
        private readonly ParameterSymbol underlyingParameter;
        private ImmutableArray<CustomModifier> lazyCustomModifiers;

        /// <summary>
        /// Retargeted custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> lazyCustomAttributes;

        protected RetargetingParameterSymbol(ParameterSymbol underlyingParameter)
        {
            Debug.Assert(!(underlyingParameter is RetargetingParameterSymbol));
            this.underlyingParameter = underlyingParameter;
        }

        // test only
        internal ParameterSymbol UnderlyingParameter
        {
            get
            {
                return this.underlyingParameter;
            }
        }

        protected abstract RetargetingModuleSymbol RetargetingModule
        {
            get;
        }

        public sealed override TypeSymbol Type
        {
            get
            {
                return this.RetargetingModule.RetargetingTranslator.Retarget(this.underlyingParameter.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
            }
        }

        public sealed override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                return RetargetingModule.RetargetingTranslator.RetargetModifiers(
                    underlyingParameter.CustomModifiers,
                    ref lazyCustomModifiers);
            }
        }

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return this.RetargetingModule.RetargetingTranslator.Retarget(this.underlyingParameter.ContainingSymbol);
            }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.RetargetingModule.RetargetingTranslator.GetRetargetedAttributes(this.underlyingParameter.GetAttributes(), ref this.lazyCustomAttributes);
        }

        internal sealed override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return this.RetargetingModule.RetargetingTranslator.RetargetAttributes(this.underlyingParameter.GetCustomAttributesToEmit(compilationState));
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
                return this.underlyingParameter.HasMetadataConstantValue;
            }
        }

        internal sealed override bool IsMarshalledExplicitly
        {
            get
            {
                return this.underlyingParameter.IsMarshalledExplicitly;
            }
        }

        internal override MarshalPseudoCustomAttributeData MarshallingInformation
        {
            get
            {
                return this.RetargetingModule.RetargetingTranslator.Retarget(this.underlyingParameter.MarshallingInformation);
            }
        }

        internal override ImmutableArray<byte> MarshallingDescriptor
        {
            get
            {
                return this.underlyingParameter.MarshallingDescriptor;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        #region Forwarded

        internal sealed override ConstantValue ExplicitDefaultConstantValue
        {
            get { return underlyingParameter.ExplicitDefaultConstantValue; }
        }

        public sealed override RefKind RefKind
        {
            get { return underlyingParameter.RefKind; }
        }

        internal sealed override bool IsMetadataIn
        {
            get { return underlyingParameter.IsMetadataIn; }
        }

        internal sealed override bool IsMetadataOut
        {
            get { return underlyingParameter.IsMetadataOut; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return underlyingParameter.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return underlyingParameter.DeclaringSyntaxReferences; }
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            underlyingParameter.AddSynthesizedAttributes(compilationState, ref attributes);
        }

        public override int Ordinal
        {
            get { return underlyingParameter.Ordinal; }
        }

        public override bool IsParams
        {
            get { return underlyingParameter.IsParams; }
        }

        internal override bool IsMetadataOptional
        {
            get { return underlyingParameter.IsMetadataOptional; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return underlyingParameter.IsImplicitlyDeclared; }
        }

        public sealed override string Name
        {
            get { return underlyingParameter.Name; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return underlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal sealed override UnmanagedType MarshallingType
        {
            get { return underlyingParameter.MarshallingType; }
        }

        internal sealed override bool IsIDispatchConstant
        {
            get { return underlyingParameter.IsIDispatchConstant; }
        }

        internal sealed override bool IsIUnknownConstant
        {
            get { return underlyingParameter.IsIUnknownConstant; }
        }

        internal sealed override bool IsCallerLineNumber
        {
            get { return underlyingParameter.IsCallerLineNumber; }
        }

        internal sealed override bool IsCallerFilePath
        {
            get { return underlyingParameter.IsCallerFilePath; }
        }

        internal sealed override bool IsCallerMemberName
        {
            get { return underlyingParameter.IsCallerMemberName; }
        }

        internal sealed override bool HasByRefBeforeCustomModifiers
        {
            get { return underlyingParameter.HasByRefBeforeCustomModifiers; }
        }

        #endregion
    }

    internal sealed class RetargetingMethodParameterSymbol : RetargetingParameterSymbol
    {
        /// <summary>
        /// Owning RetargetingMethodSymbol.
        /// </summary>
        private readonly RetargetingMethodSymbol retargetingMethod;

        public RetargetingMethodParameterSymbol(RetargetingMethodSymbol retargetingMethod, ParameterSymbol underlyingParameter)
            : base(underlyingParameter)
        {
            Debug.Assert((object)retargetingMethod != null);
            this.retargetingMethod = retargetingMethod;
        }

        protected override RetargetingModuleSymbol RetargetingModule
        {
            get { return this.retargetingMethod.RetargetingModule; }
        }
    }

    internal sealed class RetargetingPropertyParameterSymbol : RetargetingParameterSymbol
    {
        /// <summary>
        /// Owning RetargetingPropertySymbol.
        /// </summary>
        private readonly RetargetingPropertySymbol retargetingProperty;

        public RetargetingPropertyParameterSymbol(RetargetingPropertySymbol retargetingProperty, ParameterSymbol underlyingParameter)
            : base(underlyingParameter)
        {
            Debug.Assert((object)retargetingProperty != null);
            this.retargetingProperty = retargetingProperty;
        }

        protected override RetargetingModuleSymbol RetargetingModule
        {
            get { return this.retargetingProperty.RetargetingModule; }
        }
    }
}

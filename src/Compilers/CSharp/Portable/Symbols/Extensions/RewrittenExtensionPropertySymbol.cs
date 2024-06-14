// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class RewrittenExtensionPropertySymbol : WrappedPropertySymbol
    {
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        protected RewrittenExtensionPropertySymbol(PropertySymbol originalProperty) : base(originalProperty)
        {
            Debug.Assert(originalProperty.IsDefinition);
            Debug.Assert(originalProperty.ExplicitInterfaceImplementations.IsEmpty);
        }

        public sealed override bool IsVirtual => false;

        public sealed override bool IsOverride => false;
        public sealed override bool IsAbstract => false;
        public sealed override bool IsSealed => false;

        // PROTOTYPE(roles): Do we want to support extern/external instance properties
        public sealed override bool IsExtern => false;

        // PROTOTYPE(roles): How doc comments are supposed to work? GetDocumentationCommentXml

        internal sealed override bool MustCallMethodsDirectly => UnderlyingProperty.MustCallMethodsDirectly;

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            UnderlyingProperty.AddSynthesizedAttributes(moduleBuilder, ref attributes);
        }

        internal sealed override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return UnderlyingProperty.GetUseSiteInfo();
        }

        public sealed override Symbol ContainingSymbol => UnderlyingProperty.ContainingSymbol;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, this.MakeParameters());
                }
                return _lazyParameters;
            }
        }

        protected abstract ImmutableArray<ParameterSymbol> MakeParameters();

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingProperty.RefCustomModifiers;

        public override TypeWithAnnotations TypeWithAnnotations => UnderlyingProperty.TypeWithAnnotations;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class RewrittenExtensionEventSymbol : WrappedEventSymbol
    {
        protected RewrittenExtensionEventSymbol(EventSymbol originalEvent) : base(originalEvent)
        {
            Debug.Assert(originalEvent.IsDefinition);
            Debug.Assert(originalEvent.ExplicitInterfaceImplementations.IsEmpty);
        }

        public sealed override bool IsVirtual => false;

        public sealed override bool IsOverride => false;
        public sealed override bool IsAbstract => false;
        public sealed override bool IsSealed => false;

        // PROTOTYPE(roles): Do we want to support extern/external instance events
        public sealed override bool IsExtern => false;

        // PROTOTYPE(roles): How doc comments are supposed to work? GetDocumentationCommentXml

        internal sealed override bool MustCallMethodsDirectly => UnderlyingEvent.MustCallMethodsDirectly;

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            UnderlyingEvent.AddSynthesizedAttributes(moduleBuilder, ref attributes);
        }

        internal sealed override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            return UnderlyingEvent.GetUseSiteInfo();
        }

        public sealed override Symbol ContainingSymbol => UnderlyingEvent.ContainingSymbol;

        public override ImmutableArray<EventSymbol> ExplicitInterfaceImplementations => ImmutableArray<EventSymbol>.Empty;

        public override TypeWithAnnotations TypeWithAnnotations => UnderlyingEvent.TypeWithAnnotations;
    }
}

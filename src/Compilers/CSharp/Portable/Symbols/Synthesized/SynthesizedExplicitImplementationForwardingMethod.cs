// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// When C# interface implementation differs from CLR interface implementation,
    /// we insert a synthesized explicit interface implementation that delegates
    /// to the method that C# considers an implicit implementation.
    /// There are two key scenarios for this:
    /// 1) A single source method is implicitly implementing one or more interface
    ///    methods from metadata and the interface methods have different custom
    ///    modifiers.  In this case, we explicitly implement the interface methods
    ///    and have (all) implementations delegate to the source method.
    /// 2) A non-virtual, non-source method in a base type is implicitly implementing
    ///    an interface method.  Since we can't change the "virtualness" of the 
    ///    non-source method, we introduce an explicit implementation that delegates
    ///    to it instead.
    /// </summary>
    internal sealed partial class SynthesizedExplicitImplementationForwardingMethod : SynthesizedImplementationMethod
    {
        private readonly MethodSymbol _implementingMethod;

        public SynthesizedExplicitImplementationForwardingMethod(MethodSymbol interfaceMethod, MethodSymbol implementingMethod, NamedTypeSymbol implementingType)
            : base(interfaceMethod, implementingType, generateDebugInfo: false)
        {
            _implementingMethod = implementingMethod;
        }

        public MethodSymbol ImplementingMethod
        {
            get { return _implementingMethod; }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return _implementingMethod.IsAccessor() ?
                    _implementingMethod.MethodKind :
                    MethodKind.ExplicitInterfaceImplementation;
            }
        }

        public override bool IsStatic => _implementingMethod.IsStatic;

        internal override bool HasSpecialName => false;

        internal sealed override bool HasRuntimeSpecialName => false;
    }
}

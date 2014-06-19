// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;

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
        private readonly MethodSymbol implementingMethod;

        public SynthesizedExplicitImplementationForwardingMethod(
            MethodSymbol interfaceMethod,
            MethodSymbol implementingMethod,
            NamedTypeSymbol implementingType) : base(interfaceMethod, implementingType)
        {
            this.implementingMethod = implementingMethod;
        }

        public MethodSymbol ImplementingMethod
        {
            get { return this.implementingMethod; }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return implementingMethod.IsAccessor() ?
                    implementingMethod.MethodKind :
                    MethodKind.ExplicitInterfaceImplementation;
            }
        }
    }
}

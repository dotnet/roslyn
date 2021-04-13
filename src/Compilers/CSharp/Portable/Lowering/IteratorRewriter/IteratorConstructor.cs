// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The constructor of the class that is the translation of an iterator method.
    /// </summary>
    internal sealed class IteratorConstructor : SynthesizedInstanceConstructor, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal IteratorConstructor(StateMachineTypeSymbol container)
            : base(container)
        {
            var intType = container.DeclaringCompilation.GetSpecialType(SpecialType.System_Int32);
            _parameters = ImmutableArray.Create<ParameterSymbol>(
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(intType), 0, RefKind.None, GeneratedNames.MakeStateMachineStateFieldName()));
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Public; }
        }

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)this.ContainingSymbol).Method; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return false; }
        }
    }
}

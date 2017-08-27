﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The constructor of the class that is the translation of an iterator method.
    /// </summary>
    internal sealed class IteratorConstructor : SynthesizedInstanceConstructor, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal IteratorConstructor(IteratorStateMachine container)
            : base(container)
        {
            var intType = container.DeclaringCompilation.GetSpecialType(SpecialType.System_Int32);
            _parameters = ImmutableArray.Create<ParameterSymbol>(
                SynthesizedParameterSymbol.Create(this, intType, 0, RefKind.None, GeneratedNames.MakeStateMachineStateFieldName()));
        }

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

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

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)this.ContainingSymbol).Method; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return false; }
        }
    }
}

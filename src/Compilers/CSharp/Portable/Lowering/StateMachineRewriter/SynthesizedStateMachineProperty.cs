// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// State machine interface property implementation.
    /// </summary>
    internal class SynthesizedStateMachineProperty : PropertySymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly SynthesizedStateMachineMethod _getter;
        private readonly string _name;

        internal SynthesizedStateMachineProperty(
            MethodSymbol interfacePropertyGetter,
            StateMachineTypeSymbol stateMachineType)
        {
            _name = ExplicitInterfaceHelpers.GetMemberName(interfacePropertyGetter.AssociatedSymbol.Name, interfacePropertyGetter.ContainingType, aliasQualifierOpt: null);
            var getterName = ExplicitInterfaceHelpers.GetMemberName(interfacePropertyGetter.Name, interfacePropertyGetter.ContainingType, aliasQualifierOpt: null);

            _getter = new SynthesizedStateMachineDebuggerHiddenMethod(
                getterName,
                interfacePropertyGetter,
                stateMachineType,
                associatedProperty: this,
                hasMethodBodyDependency: false);
        }

        public override string Name
        {
            get { return _name; }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get { return _getter.ReturnTypeWithAnnotations; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _getter.RefCustomModifiers; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _getter.Parameters; }
        }

        public override bool IsIndexer
        {
            get { return !_getter.Parameters.IsEmpty; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        public override MethodSymbol GetMethod
        {
            get { return _getter; }
        }

        public override MethodSymbol SetMethod
        {
            get { return null; }
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get { return _getter.CallingConvention; }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return false; }
        }

        private PropertySymbol ImplementedProperty
        {
            get
            {
                return (PropertySymbol)_getter.ExplicitInterfaceImplementations[0].AssociatedSymbol;
            }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray.Create(ImplementedProperty); }
        }

        public override Symbol ContainingSymbol
        {
            get { return _getter.ContainingSymbol; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return _getter.DeclaredAccessibility; }
        }

        public override bool IsStatic
        {
            get { return _getter.IsStatic; }
        }

        public override bool IsVirtual
        {
            get { return _getter.IsVirtual; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get { return _getter.HasMethodBodyDependency; }
        }

        IMethodSymbolInternal ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method; }
        }
    }
}

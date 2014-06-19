// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// State machine interface property implementation.
    /// </summary>
    internal class SynthesizedStateMachineProperty : PropertySymbol, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly SynthesizedStateMachineMethod getter;
        private readonly string name;

        internal SynthesizedStateMachineProperty(
            MethodSymbol interfacePropertyGetter,
            NamedTypeSymbol implementingType,
            bool debuggerHidden,
            bool hasMethodBodyDependency)
        {
            this.name = ExplicitInterfaceHelpers.GetMemberName(interfacePropertyGetter.AssociatedSymbol.Name, interfacePropertyGetter.ContainingType, aliasQualifierOpt: null);
            var getterName = ExplicitInterfaceHelpers.GetMemberName(interfacePropertyGetter.Name, interfacePropertyGetter.ContainingType, aliasQualifierOpt: null);

            getter = new SynthesizedStateMachineMethod(
                getterName,
                interfacePropertyGetter,
                implementingType,
                asyncKickoffMethod: null,
                associatedProperty: this,
                debuggerHidden: debuggerHidden,
                hasMethodBodyDependency: hasMethodBodyDependency);
        }

        public override string Name
        {
            get { return name; }
        }

        public override TypeSymbol Type
        {
            get { return getter.ReturnType; }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get { return getter.ReturnTypeCustomModifiers; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return getter.Parameters; }
        }

        public override bool IsIndexer
        {
            get { return !getter.Parameters.IsEmpty; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        public override MethodSymbol GetMethod
        {
            get { return getter; }
        }

        public override MethodSymbol SetMethod
        {
            get { return null; }
        }

        internal override Cci.CallingConvention CallingConvention
        {
            get { return getter.CallingConvention; }
        }

        internal override bool MustCallMethodsDirectly
        {
            get { return false; }
        }

        private PropertySymbol ImplementedProperty
        {
            get
            {
                return (PropertySymbol)getter.ExplicitInterfaceImplementations[0].AssociatedSymbol;
            }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray.Create(ImplementedProperty); }
        }

        public override Symbol ContainingSymbol
        {
            get { return getter.ContainingSymbol; }
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
            get { return getter.DeclaredAccessibility; }
        }

        public override bool IsStatic
        {
            get { return getter.IsStatic; }
        }

        public override bool IsVirtual
        {
            get { return getter.IsVirtual; }
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
            get { return getter.HasMethodBodyDependency; }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return ((ISynthesizedMethodBodyImplementationSymbol)ContainingSymbol).Method; }
        }
    }
}
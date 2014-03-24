// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SynthesizedImplementationReadOnlyProperty : PropertySymbol
    {
        private readonly SynthesizedImplementationMethod getter;
        private readonly string propName;

        internal SynthesizedImplementationReadOnlyProperty(
            MethodSymbol interfaceMethod,
            NamedTypeSymbol implementingType,
            bool debuggerHidden = false)
        {
            this.propName = ExplicitInterfaceHelpers.GetMemberName(interfaceMethod.AssociatedSymbol.Name, interfaceMethod.ContainingType, aliasQualifierOpt: null);

            var getterName = ExplicitInterfaceHelpers.GetMemberName(interfaceMethod.Name, interfaceMethod.ContainingType, aliasQualifierOpt: null);
            getter = new SynthesizedImplementationMethod(interfaceMethod,
                                                        implementingType,
                                                        getterName,
                                                        debuggerHidden,
                                                        associatedProperty: this);
        }

        public override string Name
        {
            get { return propName; }
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
    }
}
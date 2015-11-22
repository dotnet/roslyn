// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    internal class MockNamedTypeSymbol : NamedTypeSymbol, IMockSymbol
    {
        private Symbol _container;
        private readonly string _name;
        private readonly TypeKind _typeKind;
        private readonly IEnumerable<Symbol> _children;

        public void SetContainer(Symbol container)
        {
            _container = container;
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return Arity > 0;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray.Create<TypeParameterSymbol>();
            }
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray.Create<TypeSymbolWithAnnotations>();
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override bool HasSpecialName
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            return _children.AsImmutable();
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return (from sym in _children
                    where sym.Name == name
                    select sym).ToArray().AsImmutableOrNull();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembers();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return (from sym in _children
                    where sym is NamedTypeSymbol
                    select (NamedTypeSymbol)sym).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return (from sym in _children
                    where sym is NamedTypeSymbol && sym.Name == name && ((NamedTypeSymbol)sym).Arity == arity
                    select (NamedTypeSymbol)sym).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return (from sym in _children
                    where sym is NamedTypeSymbol && sym.Name == name
                    select (NamedTypeSymbol)sym).ToArray().AsImmutableOrNull();
        }

        public override TypeKind TypeKind
        {
            get { return _typeKind; }
        }

        internal override bool IsInterface
        {
            get { return _typeKind == TypeKind.Interface; }
        }

        public override Symbol ContainingSymbol
        {
            get { return null; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray.Create<Location>(); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>();
            }
        }

        public MockNamedTypeSymbol(string name, IEnumerable<Symbol> children, TypeKind kind = TypeKind.Class)
        {
            _name = name;
            _children = children;
            _typeKind = kind;
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.Public;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { throw new NotImplementedException(); }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal sealed override bool IsManagedType
        {
            get
            {
                return true;
            }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get
            {
                return false;
            }
        }

        internal override bool IsComImport
        {
            get { return false; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal override TypeLayout Layout
        {
            get { return default(TypeLayout); }
        }

        internal override System.Runtime.InteropServices.CharSet MarshallingCharSet
        {
            get { return DefaultMarshallingCharSet; }
        }

        internal override bool IsSerializable
        {
            get { return false; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return null;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return AttributeUsageInfo.Null;
        }
    }
}

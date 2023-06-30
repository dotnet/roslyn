// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
            => throw new NotImplementedException();

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

        internal sealed override bool IsFileLocal => false;
        internal sealed override FileIdentifier? AssociatedFileIdentifier => null;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray.Create<TypeParameterSymbol>();
            }
        }

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray.Create<TypeWithAnnotations>();
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

        internal override bool HasDeclaredRequiredMembers => throw new NotImplementedException();

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

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
        {
            return (from sym in _children
                    where sym is NamedTypeSymbol namedType && sym.Name.AsSpan().SequenceEqual(name.Span) && namedType.Arity == arity
                    select (NamedTypeSymbol)sym).ToArray().AsImmutableOrNull();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return (from sym in _children
                    where sym is NamedTypeSymbol && sym.Name.AsSpan().SequenceEqual(name.Span)
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

        public sealed override bool IsRefLikeType
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsReadOnly
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

        public sealed override bool AreLocalsZeroed
        {
            get
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => throw new NotImplementedException();

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            throw new NotImplementedException();
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal sealed override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => ManagedKind.Managed;

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

        public override bool IsSerializable
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

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;

        internal override bool IsRecord => false;
        internal override bool IsRecordStruct => false;
        internal override bool HasPossibleWellKnownCloneMethod() => false;
        internal override bool IsInterpolatedStringHandlerType => false;

        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal sealed override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }
    }
}

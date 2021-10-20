// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents an anonymous type 'public' symbol which is used in binding and lowering.
        /// In emit phase it is being substituted with implementation symbol.
        /// </summary>
        internal sealed class AnonymousTypePublicSymbol : AnonymousTypeOrDelegatePublicSymbol
        {
            private readonly ImmutableArray<Symbol> _members;

            /// <summary> Properties defined in the type </summary>
            internal readonly ImmutableArray<AnonymousTypePropertySymbol> Properties;

            /// <summary> Maps member names to symbol(s) </summary>
            private readonly MultiDictionary<string, Symbol> _nameToSymbols = new MultiDictionary<string, Symbol>();

            internal AnonymousTypePublicSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr) :
                base(manager, typeDescr)
            {
                typeDescr.AssertIsGood();

                var fields = typeDescr.Fields;
                var properties = fields.SelectAsArray((field, i, type) => new AnonymousTypePropertySymbol(type, field, i), this);

                //  members
                int membersCount = fields.Length * 2 + 1;
                var members = ArrayBuilder<Symbol>.GetInstance(membersCount);

                foreach (var property in properties)
                {
                    // Property related symbols
                    members.Add(property);
                    members.Add(property.GetMethod);
                }

                this.Properties = properties;

                // Add a constructor
                members.Add(new AnonymousTypeConstructorSymbol(this, properties));
                _members = members.ToImmutableAndFree();
                Debug.Assert(membersCount == _members.Length);

                //  fill nameToSymbols map
                foreach (var symbol in _members)
                {
                    _nameToSymbols.Add(symbol.Name, symbol);
                }
            }

            internal override NamedTypeSymbol MapToImplementationSymbol()
            {
                return Manager.ConstructAnonymousTypeImplementationSymbol(this);
            }

            internal override AnonymousTypeOrDelegatePublicSymbol SubstituteTypes(AbstractTypeMap map)
            {
                var oldFieldTypes = TypeDescriptor.Fields.SelectAsArray(f => f.TypeWithAnnotations);
                var newFieldTypes = map.SubstituteTypes(oldFieldTypes);
                return (oldFieldTypes == newFieldTypes) ?
                    this :
                    new AnonymousTypePublicSymbol(Manager, TypeDescriptor.WithNewFieldsTypes(newFieldTypes));
            }

            public override TypeKind TypeKind
            {
                get { return TypeKind.Class; }
            }

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => Manager.System_Object;

            public override ImmutableArray<Symbol> GetMembers()
            {
                return _members;
            }

            public override ImmutableArray<Symbol> GetMembers(string name)
            {
                var symbols = _nameToSymbols[name];
                var builder = ArrayBuilder<Symbol>.GetInstance(symbols.Count);
                foreach (var symbol in symbols)
                {
                    builder.Add(symbol);
                }

                return builder.ToImmutableAndFree();
            }

            public override IEnumerable<string> MemberNames
            {
                get { return _nameToSymbols.Keys; }
            }

            internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
            {
                if (ReferenceEquals(this, t2))
                {
                    return true;
                }

                var other = t2 as AnonymousTypePublicSymbol;
                return other is { } && this.TypeDescriptor.Equals(other.TypeDescriptor, comparison);
            }

            public override int GetHashCode()
            {
                return this.TypeDescriptor.GetHashCode();
            }
        }

        internal abstract class AnonymousTypeOrDelegatePublicSymbol : NamedTypeSymbol
        {
            /// <summary> Anonymous type manager owning this template </summary>
            internal readonly AnonymousTypeManager Manager;

            /// <summary> Anonymous type descriptor </summary>
            internal readonly AnonymousTypeDescriptor TypeDescriptor;

            internal AnonymousTypeOrDelegatePublicSymbol(AnonymousTypeManager manager, AnonymousTypeDescriptor typeDescr)
            {
                typeDescr.AssertIsGood();

                this.Manager = manager;
                this.TypeDescriptor = typeDescr;
            }

            internal abstract NamedTypeSymbol MapToImplementationSymbol();

            internal abstract AnonymousTypeOrDelegatePublicSymbol SubstituteTypes(AbstractTypeMap typeMap);

            protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
                => throw ExceptionUtilities.Unreachable;

            internal sealed override IEnumerable<FieldSymbol> GetFieldsToEmit()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal sealed override bool HasCodeAnalysisEmbeddedAttribute => false;

            internal sealed override bool IsInterpolatedStringHandlerType => false;

            internal sealed override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
            {
                return this.GetMembersUnordered();
            }

            internal sealed override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
            {
                return this.GetMembers(name);
            }

            public sealed override Symbol ContainingSymbol
            {
                get { return this.Manager.Compilation.SourceModule.GlobalNamespace; }
            }

            public sealed override string Name
            {
                get { return string.Empty; }
            }

            public sealed override string MetadataName
            {
                get { return string.Empty; }
            }

            internal sealed override bool MangleName
            {
                get { return false; }
            }

            public sealed override int Arity
            {
                get { return 0; }
            }

            public sealed override bool IsImplicitlyDeclared
            {
                get { return false; }
            }

            public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get { return ImmutableArray<TypeParameterSymbol>.Empty; }
            }

            internal sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
            {
                get { return ImmutableArray<TypeWithAnnotations>.Empty; }
            }

            public sealed override bool IsAbstract
            {
                get { return false; }
            }

            public sealed override bool IsRefLikeType
            {
                get { return false; }
            }

            public sealed override bool IsReadOnly
            {
                get { return false; }
            }

            public sealed override bool IsSealed
            {
                get { return true; }
            }

            public sealed override bool MightContainExtensionMethods
            {
                get { return false; }
            }

            internal sealed override bool HasSpecialName
            {
                get { return false; }
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            public sealed override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Internal; }
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal abstract override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics { get; }

            public abstract override TypeKind TypeKind { get; }

            internal sealed override bool IsInterface
            {
                get { return false; }
            }

            public sealed override ImmutableArray<Location> Locations
            {
                get { return ImmutableArray.Create<Location>(this.TypeDescriptor.Location); }
            }

            public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return GetDeclaringSyntaxReferenceHelper<AnonymousObjectCreationExpressionSyntax>(this.Locations);
                }
            }

            public sealed override bool IsStatic
            {
                get { return false; }
            }

            public sealed override bool IsAnonymousType
            {
                get { return true; }
            }

            public sealed override NamedTypeSymbol ConstructedFrom
            {
                get { return this; }
            }

            internal sealed override bool ShouldAddWinRTMembers
            {
                get { return false; }
            }

            internal sealed override bool IsWindowsRuntimeImport
            {
                get { return false; }
            }

            internal sealed override bool IsComImport
            {
                get { return false; }
            }

            internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
            {
                get { return null; }
            }

            internal sealed override TypeLayout Layout
            {
                get { return default(TypeLayout); }
            }

            internal sealed override CharSet MarshallingCharSet
            {
                get { return DefaultMarshallingCharSet; }
            }

            public sealed override bool IsSerializable
            {
                get { return false; }
            }

            public sealed override bool AreLocalsZeroed
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            internal sealed override bool HasDeclarativeSecurity
            {
                get { return false; }
            }

            internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                return ImmutableArray<string>.Empty;
            }

            internal sealed override AttributeUsageInfo GetAttributeUsageInfo()
            {
                return AttributeUsageInfo.Null;
            }

            internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
            {
                return this.Manager.System_Object;
            }

            internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
            {
                return ImmutableArray<NamedTypeSymbol>.Empty;
            }

            internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable;

            internal sealed override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

            internal sealed override bool IsRecord => false;
            internal sealed override bool IsRecordStruct => false;

            internal abstract override bool Equals(TypeSymbol t2, TypeCompareKind comparison);

            public abstract override int GetHashCode();

            internal sealed override bool HasPossibleWellKnownCloneMethod() => false;

            internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
            {
                return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
            }
        }
    }
}

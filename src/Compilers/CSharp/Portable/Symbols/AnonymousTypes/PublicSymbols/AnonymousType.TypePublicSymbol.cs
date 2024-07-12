// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
                var properties = fields.SelectAsArray(map: (field, i, type) => new AnonymousTypePropertySymbol(type, field, i), arg: this);

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

            public override bool IsImplicitlyDeclared
            {
                get { return false; }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return GetDeclaringSyntaxReferenceHelper<AnonymousObjectCreationExpressionSyntax>(this.Locations);
                }
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
    }
}

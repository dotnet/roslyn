// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents an anonymous type template's property symbol.
        /// </summary>
        private sealed class AnonymousTypePropertySymbol : PropertySymbol
        {
            private readonly NamedTypeSymbol _containingType;
            private readonly TypeWithAnnotations _typeWithAnnotations;
            private readonly string _name;
            private readonly int _index;
            private readonly ImmutableArray<Location> _locations;
            private readonly AnonymousTypePropertyGetAccessorSymbol _getMethod;
            private readonly FieldSymbol _backingField;

            internal AnonymousTypePropertySymbol(AnonymousTypeTemplateSymbol container, AnonymousTypeField field, TypeWithAnnotations fieldTypeWithAnnotations, int index) :
                this(container, field, fieldTypeWithAnnotations, index, ImmutableArray<Location>.Empty, includeBackingField: true)
            {
            }

            internal AnonymousTypePropertySymbol(AnonymousTypePublicSymbol container, AnonymousTypeField field, int index) :
                this(container, field, field.TypeWithAnnotations, index, ImmutableArray.Create<Location>(field.Location), includeBackingField: false)
            {
            }

            private AnonymousTypePropertySymbol(
                NamedTypeSymbol container,
                AnonymousTypeField field,
                TypeWithAnnotations fieldTypeWithAnnotations,
                int index,
                ImmutableArray<Location> locations,
                bool includeBackingField)
            {
                Debug.Assert((object)container != null);
                Debug.Assert((object)field != null);
                Debug.Assert(fieldTypeWithAnnotations.HasType);
                Debug.Assert(index >= 0);
                Debug.Assert(!locations.IsDefault);

                _containingType = container;
                _typeWithAnnotations = fieldTypeWithAnnotations;
                _name = field.Name;
                _index = index;
                _locations = locations;
                _getMethod = new AnonymousTypePropertyGetAccessorSymbol(this);
                _backingField = includeBackingField ? new AnonymousTypeFieldSymbol(this) : null;
            }

            internal override int? MemberIndexOpt => _index;

            public override RefKind RefKind
            {
                get { return RefKind.None; }
            }

            public override TypeWithAnnotations TypeWithAnnotations
            {
                get { return _typeWithAnnotations; }
            }

            public override string Name
            {
                get { return _name; }
            }

            internal override bool HasSpecialName
            {
                get { return false; }
            }

            public override bool IsImplicitlyDeclared
            {
                get { return false; }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return _locations; }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return GetDeclaringSyntaxReferenceHelper<AnonymousObjectMemberDeclaratorSyntax>(this.Locations);
                }
            }

            public override bool IsStatic
            {
                get { return false; }
            }

            public override bool IsOverride
            {
                get { return false; }
            }

            public override bool IsVirtual
            {
                get { return false; }
            }

            public override bool IsIndexer
            {
                get { return false; }
            }

            public override bool IsSealed
            {
                get { return false; }
            }

            public override bool IsAbstract
            {
                get { return false; }
            }

            internal sealed override ObsoleteAttributeData ObsoleteAttributeData
            {
                get { return null; }
            }

            public override ImmutableArray<ParameterSymbol> Parameters
            {
                get { return ImmutableArray<ParameterSymbol>.Empty; }
            }

            public override MethodSymbol SetMethod
            {
                get { return null; }
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers
            {
                get { return ImmutableArray<CustomModifier>.Empty; }
            }

            internal override Microsoft.Cci.CallingConvention CallingConvention
            {
                get { return Microsoft.Cci.CallingConvention.HasThis; }
            }

            public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
            {
                get { return ImmutableArray<PropertySymbol>.Empty; }
            }

            public override Symbol ContainingSymbol
            {
                get { return _containingType; }
            }

            public override NamedTypeSymbol ContainingType
            {
                get
                {
                    return _containingType;
                }
            }

            public override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Public; }
            }

            internal override bool MustCallMethodsDirectly
            {
                get { return false; }
            }

            public override bool IsExtern
            {
                get { return false; }
            }

            public override MethodSymbol GetMethod
            {
                get { return _getMethod; }
            }

            public FieldSymbol BackingField
            {
                get { return _backingField; }
            }

            public override bool Equals(Symbol obj, TypeCompareKind compareKind)
            {
                if (obj == null)
                {
                    return false;
                }
                else if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                var other = obj as AnonymousTypePropertySymbol;
                if ((object)other == null)
                {
                    return false;
                }

                //  consider properties the same is the owning types are the same and 
                //  the names are equal
                return ((object)other != null) && other.Name == this.Name
                    && other.ContainingType.Equals(this.ContainingType, compareKind);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.ContainingType.GetHashCode(), this.Name.GetHashCode());
            }
        }
    }
}

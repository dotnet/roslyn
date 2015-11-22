// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
            private readonly TypeSymbolWithAnnotations _type;
            private readonly string _name;
            private readonly ImmutableArray<Location> _locations;
            private readonly AnonymousTypePropertyGetAccessorSymbol _getMethod;
            private readonly FieldSymbol _backingField;

            internal AnonymousTypePropertySymbol(AnonymousTypeTemplateSymbol container, AnonymousTypeField field, TypeSymbolWithAnnotations fieldTypeSymbol)
            {
                _containingType = container;
                _type = fieldTypeSymbol;
                _name = field.Name;
                _locations = ImmutableArray<Location>.Empty;
                _getMethod = new AnonymousTypePropertyGetAccessorSymbol(this);
                _backingField = new AnonymousTypeFieldSymbol(this);
            }

            internal AnonymousTypePropertySymbol(AnonymousTypePublicSymbol container, AnonymousTypeField field)
            {
                _containingType = container;
                _type = field.Type;
                _name = field.Name;
                _locations = ImmutableArray.Create<Location>(field.Location);
                _getMethod = new AnonymousTypePropertyGetAccessorSymbol(this);
                _backingField = null;
            }

            public override TypeSymbolWithAnnotations Type
            {
                get { return _type; }
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

            public override bool Equals(object obj)
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
                    && other.ContainingType.Equals(this.ContainingType);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.ContainingType.GetHashCode(), this.Name.GetHashCode());
            }
        }
    }
}

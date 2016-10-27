// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationTypeSymbol : CodeGenerationNamespaceOrTypeSymbol, ITypeSymbol
    {
        public SpecialType SpecialType { get; protected set; }

        protected CodeGenerationTypeSymbol(
            INamedTypeSymbol containingType,
            IList<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name,
            SpecialType specialType)
            : base(containingType, attributes, declaredAccessibility, modifiers, name)
        {
            this.SpecialType = specialType;
        }

        public abstract TypeKind TypeKind { get; }

        public virtual INamedTypeSymbol BaseType
        {
            get
            {
                return null;
            }
        }

        public virtual ImmutableArray<INamedTypeSymbol> Interfaces
        {
            get
            {
                return ImmutableArray.Create<INamedTypeSymbol>();
            }
        }

        public ImmutableArray<INamedTypeSymbol> AllInterfaces
        {
            get
            {
                return ImmutableArray.Create<INamedTypeSymbol>();
            }
        }

        public bool IsReferenceType
        {
            get
            {
                return false;
            }
        }

        public bool IsValueType
        {
            get
            {
                return false;
            }
        }

        public bool IsAnonymousType
        {
            get
            {
                return false;
            }
        }

        public bool IsTupleType => false;

        public ImmutableArray<ITypeSymbol> TupleElementTypes => default(ImmutableArray<ITypeSymbol>);

        public ImmutableArray<string> TupleElementNames => default(ImmutableArray<string>);

        public INamedTypeSymbol TupleUnderlyingType => null;

        public new ITypeSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
        {
            return null;
        }

        public override bool IsNamespace
        {
            get
            {
                return false;
            }
        }

        public override bool IsType
        {
            get
            {
                return true;
            }
        }
    }
}

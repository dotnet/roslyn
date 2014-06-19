//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using Roslyn.Compilers.Common;

namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class AnonymousTypeManager
    {
        /// <summary>
        /// Represents a non-generic type symbol which serves as a 'constructed' type symbol 
        /// for empty /i.e. parameterless/ anonymous type template (which is not a generic types). 
        /// </summary>
        private sealed class ConstructedEmptyAnonymousTypeSymbol : NamedTypeSymbol
        {
            private readonly MethodSymbol ctorMethod;
            private readonly AnonymousTypeDescriptor typeDescr;
            private readonly AnonymousTypeTemplateSymbol template;

            internal ConstructedEmptyAnonymousTypeSymbol(AnonymousTypeTemplateSymbol constructedFrom, AnonymousTypeDescriptor typeDescr)
            {
                this.template = constructedFrom;
                this.typeDescr = typeDescr;
                
                this.ctorMethod = new AnonymousTypeConstructorSymbol(this);
            }

            internal AnonymousTypeTemplateSymbol Template
            {
                get { return this.template; }
            }

            public override bool IsImplicitlyDeclared
            {
                get { return true; }
            }

            public override bool IsAnonymousType
            {
                get { return true; }
            }

            public override ReadOnlyArray<Symbol> GetMembers()
            {
                return ReadOnlyArray<Symbol>.CreateFrom(this.ctorMethod);
            }

            public override ReadOnlyArray<Symbol> GetMembers(string name)
            {
                return CommonMemberNames.InstanceConstructorName.Equals(name)
                        ? ReadOnlyArray<Symbol>.CreateFrom(this.ctorMethod)
                        : ReadOnlyArray<Symbol>.Empty;
            }

            public override ICollection<string> MemberNames
            {
                get { return new string[] { this.ctorMethod.Name }; }
            }

            public override ReadOnlyArray<NamedTypeSymbol> GetTypeMembers()
            {
                return ReadOnlyArray<NamedTypeSymbol>.Empty;
            }

            public override ReadOnlyArray<NamedTypeSymbol> GetTypeMembers(string name)
            {
                return ReadOnlyArray<NamedTypeSymbol>.Empty;
            }

            public override IEnumerable<NamedTypeSymbol> GetTypeMembers(string name, int arity)
            {
                return Enumerable.Empty<NamedTypeSymbol>();
            }

            public override ReadOnlyArray<Location> Locations
            {
                get { return ReadOnlyArray<Location>.CreateFrom(this.typeDescr.Location); }
            }

            public override ReadOnlyArray<SyntaxNode> DeclaringSyntaxNodes
            {
                get
                {
                    return GetDeclaringSyntaxNodeHelper<AnonymousObjectCreationExpressionSyntax>(this.Locations);
                }
            }

            public override bool IsSealed
            {
                get { return true; }
            }

            public override bool IsStatic
            {
                get { return false; }
            }

            public override bool IsAbstract
            {
                get { return false; }
            }

            public override ReadOnlyArray<TypeParameterSymbol> TypeParameters
            {
                get { return ReadOnlyArray<TypeParameterSymbol>.Empty; }
            }

            public override ReadOnlyArray<TypeSymbol> TypeArguments
            {
                get { return ReadOnlyArray<TypeSymbol>.Empty; }
            }

            public override NamedTypeSymbol ConstructedFrom
            {
                get { return this; }
            }

            internal override bool MightContainExtensionMethods
            {
                get { return false; }
            }

            public override string Name
            {
                get { return string.Empty; }
            }

            public override string MetadataName
            {
                get { return string.Empty; }
            }

            internal override bool MangleName
            {
                get { return false; }
            }

            public override int Arity
            {
                get { return 0; }
            }

            public override Accessibility DeclaredAccessibility
            {
                get { return Accessibility.Internal; }
            }

            public override ReadOnlyArray<NamedTypeSymbol> Interfaces
            {
                get { return ReadOnlyArray<NamedTypeSymbol>.Empty; }
            }

            public override NamedTypeSymbol BaseType
            {
                get { return this.template.Manager.System_Object; }
            }

            public override TypeKind TypeKind
            {
                get { return TypeKind.Class; }
            }

            public override Symbol ContainingSymbol
            {
                get { return this.template.Manager.Compilation.SourceModule.GlobalNamespace; }
            }

            internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
            {
                return this.template.Manager.System_Object;
            }

            internal override ReadOnlyArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
            {
                return ReadOnlyArray<NamedTypeSymbol>.Empty;
            }

            public override int GetHashCode()
            {
                return this.template.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                ConstructedEmptyAnonymousTypeSymbol other = obj as ConstructedEmptyAnonymousTypeSymbol;

                if (other == null)
                {
                    // TODO: Do we need to ensure ConstructedEmptyAnonymousTypeSymbol equals to 
                    //       it's 'template'? It does not look like, recheck later...
                    return false;
                }

                return ReferenceEquals(this.template, other.template);
            }

            /// <summary> Adjust the smallest location in the 
            /// anonymous type template with this type's location </summary>
            internal void AdjustSmallestLocationInTemplate()
            {
                Debug.Assert(this.typeDescr.Location.IsInSource);
                this.template.AdjustLocation(this.typeDescr.Location);
            }
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SingleTypeDeclaration : SingleNamespaceOrTypeDeclaration
    {
        private readonly DeclarationKind kind;
        private readonly TypeDeclarationFlags flags;
        private readonly ushort arity;
        private readonly DeclarationModifiers modifiers;
        private readonly ImmutableArray<SingleTypeDeclaration> children;
        private readonly ICollection<string> memberNames;

        [Flags()]
        internal enum TypeDeclarationFlags : byte
        {
            None = 0,
            AnyMemberHasExtensionMethodSyntax = 1 << 1,
            HasAnyAttributes = 1 << 2,
            HasBaseDeclarations = 1 << 3,
            AnyMemberHasAttributes = 1 << 4,
            HasAnyNontypeMembers = 1 << 5,
            HasPrimaryCtor = 1 << 6,
        }

        internal SingleTypeDeclaration(
            DeclarationKind kind,
            string name,
            int arity,
            DeclarationModifiers modifiers,
            TypeDeclarationFlags declFlags,
            SyntaxReference syntaxReference,
            SourceLocation nameLocation,
            ICollection<string> memberNames,
            ImmutableArray<SingleTypeDeclaration> children)
            : base(name,
                   syntaxReference,
                   nameLocation)
        {
            Debug.Assert(kind != DeclarationKind.Namespace);

            this.kind = kind;
            this.arity = (ushort)arity;
            this.modifiers = modifiers;
            this.memberNames = memberNames;
            this.children = children;
            this.flags = declFlags;
        }

        public override DeclarationKind Kind
        {
            get
            {
                return this.kind;
            }
        }

        public new ImmutableArray<SingleTypeDeclaration> Children
        {
            get
            {
                return children;
            }
        }

        public int Arity
        {
            get
            {
                return this.arity;
            }
        }

        public DeclarationModifiers Modifiers
        {
            get
            {
                return this.modifiers;
            }
        }

        public ICollection<string> MemberNames
        {
            get
            {
                return memberNames;
            }
        }

        public bool AnyMemberHasExtensionMethodSyntax
        {
            get
            {
                return (this.flags & TypeDeclarationFlags.AnyMemberHasExtensionMethodSyntax) != 0;
            }
        }

        public bool HasAnyAttributes
        {
            get
            {
                return (this.flags & TypeDeclarationFlags.HasAnyAttributes) != 0;
            }
        }

        public bool HasBaseDeclarations
        {
            get
            {
                return (this.flags & TypeDeclarationFlags.HasBaseDeclarations) != 0;
            }
        }

        public bool AnyMemberHasAttributes
        {
            get
            {
                return (this.flags & TypeDeclarationFlags.AnyMemberHasAttributes) != 0;
            }
        }

        public bool HasAnyNontypeMembers
        {
            get
            {
                return (this.flags & TypeDeclarationFlags.HasAnyNontypeMembers) != 0;
            }
        }

        public bool HasPrimaryCtor
        {
            get
            {
                return (this.flags & TypeDeclarationFlags.HasPrimaryCtor) != 0;
            }
        }

        protected override ImmutableArray<SingleNamespaceOrTypeDeclaration> GetNamespaceOrTypeDeclarationChildren()
        {
            return StaticCast<SingleNamespaceOrTypeDeclaration>.From(children);
        }

        internal TypeDeclarationIdentity Identity
        {
            get
            {
                return new TypeDeclarationIdentity(this);
            }
        }

        // identity that is used when collecting all declarations 
        // of same type across multiple containers
        internal struct TypeDeclarationIdentity : IEquatable<TypeDeclarationIdentity>
        {
            private readonly SingleTypeDeclaration decl;

            internal TypeDeclarationIdentity(SingleTypeDeclaration decl)
            {
                this.decl = decl;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeDeclarationIdentity && Equals((TypeDeclarationIdentity)obj);
            }

            public bool Equals(TypeDeclarationIdentity other)
            {
                var thisDecl = this.decl;
                var otherDecl = other.decl;

                // same as itself
                if ((object)thisDecl == otherDecl)
                {
                    return true;
                }

                // arity, kind, name must match
                if ((thisDecl.arity != otherDecl.arity) ||
                    (thisDecl.kind != otherDecl.kind) ||
                    (thisDecl.name != otherDecl.name))
                {
                    return false;
                }

                if (thisDecl.kind == DeclarationKind.Enum || thisDecl.kind == DeclarationKind.Delegate)
                {
                    // oh, so close, but enums and delegates cannot be partial
                    return false;
                }

                return true;
            }

            public override int GetHashCode()
            {
                var thisDecl = this.decl;
                return Hash.Combine(thisDecl.Name.GetHashCode(),
                    Hash.Combine(thisDecl.Arity.GetHashCode(),
                    (int)thisDecl.Kind));
            }
        }
    }
}
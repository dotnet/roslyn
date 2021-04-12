﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SingleTypeDeclaration : SingleNamespaceOrTypeDeclaration
    {
        private readonly DeclarationKind _kind;
        private readonly TypeDeclarationFlags _flags;
        private readonly ushort _arity;
        private readonly DeclarationModifiers _modifiers;
        private readonly ImmutableArray<SingleTypeDeclaration> _children;

        [Flags]
        internal enum TypeDeclarationFlags : ushort
        {
            None = 0,
            AnyMemberHasExtensionMethodSyntax = 1 << 1,
            HasAnyAttributes = 1 << 2,
            HasBaseDeclarations = 1 << 3,
            AnyMemberHasAttributes = 1 << 4,
            HasAnyNontypeMembers = 1 << 5,

            /// <summary>
            /// Simple program uses await expressions. Set only for <see cref="DeclarationKind.SimpleProgram"/>
            /// </summary>
            HasAwaitExpressions = 1 << 6,

            /// <summary>
            /// Set only for <see cref="DeclarationKind.SimpleProgram"/>
            /// </summary>
            IsIterator = 1 << 7,

            /// <summary>
            /// Set only for <see cref="DeclarationKind.SimpleProgram"/>
            /// </summary>
            HasReturnWithExpression = 1 << 8,
        }

        internal SingleTypeDeclaration(
            DeclarationKind kind,
            string name,
            int arity,
            DeclarationModifiers modifiers,
            TypeDeclarationFlags declFlags,
            SyntaxReference syntaxReference,
            SourceLocation nameLocation,
            ImmutableHashSet<string> memberNames,
            ImmutableArray<SingleTypeDeclaration> children,
            ImmutableArray<Diagnostic> diagnostics)
            : base(name, syntaxReference, nameLocation, diagnostics)
        {
            Debug.Assert(kind != DeclarationKind.Namespace);

            _kind = kind;
            _arity = (ushort)arity;
            _modifiers = modifiers;
            MemberNames = memberNames;
            _children = children;
            _flags = declFlags;
        }

        public override DeclarationKind Kind
        {
            get
            {
                return _kind;
            }
        }

        public new ImmutableArray<SingleTypeDeclaration> Children
        {
            get
            {
                return _children;
            }
        }

        public int Arity
        {
            get
            {
                return _arity;
            }
        }

        public DeclarationModifiers Modifiers
        {
            get
            {
                return _modifiers;
            }
        }

        public ImmutableHashSet<string> MemberNames { get; }

        public bool AnyMemberHasExtensionMethodSyntax
        {
            get
            {
                return (_flags & TypeDeclarationFlags.AnyMemberHasExtensionMethodSyntax) != 0;
            }
        }

        public bool HasAnyAttributes
        {
            get
            {
                return (_flags & TypeDeclarationFlags.HasAnyAttributes) != 0;
            }
        }

        public bool HasBaseDeclarations
        {
            get
            {
                return (_flags & TypeDeclarationFlags.HasBaseDeclarations) != 0;
            }
        }

        public bool AnyMemberHasAttributes
        {
            get
            {
                return (_flags & TypeDeclarationFlags.AnyMemberHasAttributes) != 0;
            }
        }

        public bool HasAnyNontypeMembers
        {
            get
            {
                return (_flags & TypeDeclarationFlags.HasAnyNontypeMembers) != 0;
            }
        }

        public bool HasAwaitExpressions
        {
            get
            {
                return (_flags & TypeDeclarationFlags.HasAwaitExpressions) != 0;
            }
        }

        public bool HasReturnWithExpression
        {
            get
            {
                return (_flags & TypeDeclarationFlags.HasReturnWithExpression) != 0;
            }
        }

        public bool IsIterator
        {
            get
            {
                return (_flags & TypeDeclarationFlags.IsIterator) != 0;
            }
        }

        protected override ImmutableArray<SingleNamespaceOrTypeDeclaration> GetNamespaceOrTypeDeclarationChildren()
        {
            return StaticCast<SingleNamespaceOrTypeDeclaration>.From(_children);
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
            private readonly SingleTypeDeclaration _decl;

            internal TypeDeclarationIdentity(SingleTypeDeclaration decl)
            {
                _decl = decl;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeDeclarationIdentity && Equals((TypeDeclarationIdentity)obj);
            }

            public bool Equals(TypeDeclarationIdentity other)
            {
                var thisDecl = _decl;
                var otherDecl = other._decl;

                // same as itself
                if ((object)thisDecl == otherDecl)
                {
                    return true;
                }

                // arity, kind, name must match
                if ((thisDecl._arity != otherDecl._arity) ||
                    (thisDecl._kind != otherDecl._kind) ||
                    (thisDecl.name != otherDecl.name))
                {
                    return false;
                }

                if (thisDecl._kind == DeclarationKind.Enum || thisDecl._kind == DeclarationKind.Delegate)
                {
                    // oh, so close, but enums and delegates cannot be partial
                    return false;
                }

                return true;
            }

            public override int GetHashCode()
            {
                var thisDecl = _decl;
                return Hash.Combine(thisDecl.Name.GetHashCode(),
                    Hash.Combine(thisDecl.Arity.GetHashCode(),
                    (int)thisDecl.Kind));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // An invariant of a merged type declaration is that all of its children are also merged
    // declarations.
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class MergedTypeDeclaration : MergedNamespaceOrTypeDeclaration
    {
        private readonly ImmutableArray<SingleTypeDeclaration> _declarations;
        private ImmutableArray<MergedTypeDeclaration> _lazyChildren;
        private ICollection<string> _lazyMemberNames;

        internal MergedTypeDeclaration(ImmutableArray<SingleTypeDeclaration> declarations)
            : base(declarations[0].Name)
        {
            _declarations = declarations;
        }

        public ImmutableArray<SingleTypeDeclaration> Declarations
        {
            get
            {
                return _declarations;
            }
        }

        public ImmutableArray<SyntaxReference> SyntaxReferences
        {
            get
            {
                return _declarations.SelectAsArray(r => r.SyntaxReference);
            }
        }

        /// <summary>
        /// Returns the original syntax nodes for this type declaration across all its parts.  If
        /// <paramref name="quickAttributes"/> is provided, attributes will not be returned if it
        /// is certain there are none that could match the request.  This prevents going back to 
        /// source unnecessarily.
        /// </summary>
        public ImmutableArray<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations(QuickAttributes? quickAttributes)
        {
            var attributeSyntaxListBuilder = ArrayBuilder<SyntaxList<AttributeListSyntax>>.GetInstance();

            foreach (var decl in _declarations)
            {
                if (!decl.HasAnyAttributes)
                {
                    continue;
                }

                if (quickAttributes != null && (decl.QuickAttributes & quickAttributes.Value) == 0)
                {
                    continue;
                }

                var syntaxRef = decl.SyntaxReference;
                var typeDecl = syntaxRef.GetSyntax();
                SyntaxList<AttributeListSyntax> attributesSyntaxList;
                switch (typeDecl.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        attributesSyntaxList = ((TypeDeclarationSyntax)typeDecl).AttributeLists;
                        break;

                    case SyntaxKind.DelegateDeclaration:
                        attributesSyntaxList = ((DelegateDeclarationSyntax)typeDecl).AttributeLists;
                        break;

                    case SyntaxKind.EnumDeclaration:
                        attributesSyntaxList = ((EnumDeclarationSyntax)typeDecl).AttributeLists;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeDecl.Kind());
                }

                attributeSyntaxListBuilder.Add(attributesSyntaxList);
            }

            return attributeSyntaxListBuilder.ToImmutableAndFree();
        }

        public override DeclarationKind Kind
        {
            get
            {
                return this.Declarations[0].Kind;
            }
        }

        public int Arity
        {
            get
            {
                return this.Declarations[0].Arity;
            }
        }

        public bool ContainsExtensionMethods
        {
            get
            {
                foreach (var decl in this.Declarations)
                {
                    if (decl.AnyMemberHasExtensionMethodSyntax)
                        return true;
                }

                return false;
            }
        }

        public bool HasPrimaryConstructor
        {
            get
            {
                foreach (var decl in this.Declarations)
                {
                    if (decl.HasPrimaryConstructor)
                        return true;
                }

                return false;
            }
        }

        public bool AnyMemberHasAttributes
        {
            get
            {
                foreach (var decl in this.Declarations)
                {
                    if (decl.AnyMemberHasAttributes)
                        return true;
                }

                return false;
            }
        }

        public LexicalSortKey GetLexicalSortKey(CSharpCompilation compilation)
        {
            LexicalSortKey sortKey = new LexicalSortKey(Declarations[0].NameLocation, compilation);
            for (var i = 1; i < Declarations.Length; i++)
            {
                sortKey = LexicalSortKey.First(sortKey, new LexicalSortKey(Declarations[i].NameLocation, compilation));
            }

            return sortKey;
        }

        public OneOrMany<SourceLocation> NameLocations
        {
            get
            {
                if (Declarations.Length == 1)
                    return OneOrMany.Create(Declarations[0].NameLocation);

                var builder = ArrayBuilder<SourceLocation>.GetInstance(Declarations.Length);
                foreach (var decl in Declarations)
                    builder.AddIfNotNull(decl.NameLocation);

                return builder.ToOneOrManyAndFree();
            }
        }

        private ImmutableArray<MergedTypeDeclaration> MakeChildren()
        {
            ArrayBuilder<SingleTypeDeclaration> nestedTypes = null;

            foreach (var decl in this.Declarations)
            {
                foreach (var child in decl.Children)
                {
                    var asType = child as SingleTypeDeclaration;
                    if (asType != null)
                    {
                        if (nestedTypes == null)
                        {
                            nestedTypes = ArrayBuilder<SingleTypeDeclaration>.GetInstance();
                        }
                        nestedTypes.Add(asType);
                    }
                }
            }

            var children = ArrayBuilder<MergedTypeDeclaration>.GetInstance();

            if (nestedTypes != null)
            {
                var typesGrouped = nestedTypes.ToDictionary(t => t.Identity);
                nestedTypes.Free();

                foreach (var typeGroup in typesGrouped.Values)
                {
                    children.Add(new MergedTypeDeclaration(typeGroup));
                }
            }

            return children.ToImmutableAndFree();
        }

        public new ImmutableArray<MergedTypeDeclaration> Children
        {
            get
            {
                if (_lazyChildren.IsDefault)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChildren, MakeChildren());
                }

                return _lazyChildren;
            }
        }

        protected override ImmutableArray<Declaration> GetDeclarationChildren()
        {
            return StaticCast<Declaration>.From(this.Children);
        }

        public ICollection<string> MemberNames
        {
            get
            {
                if (_lazyMemberNames == null)
                {
                    var names = UnionCollection<string>.Create(this.Declarations, d => d.MemberNames.Value);
                    Interlocked.CompareExchange(ref _lazyMemberNames, names, null);
                }

                return _lazyMemberNames;
            }
        }

        internal string GetDebuggerDisplay()
        {
            return $"{nameof(MergedTypeDeclaration)} {Name}";
        }
    }
}

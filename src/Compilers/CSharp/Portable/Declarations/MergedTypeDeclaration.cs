// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // An invariant of a merged type declaration is that all of its children are also merged
    // declarations.
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

        public ImmutableArray<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            var attributeSyntaxListBuilder = ArrayBuilder<SyntaxList<AttributeListSyntax>>.GetInstance();

            foreach (var decl in _declarations)
            {
                if (!decl.HasAnyAttributes)
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

        public ImmutableArray<SourceLocation> NameLocations
        {
            get
            {
                if (Declarations.Length == 1)
                {
                    return ImmutableArray.Create(Declarations[0].NameLocation);
                }
                else
                {
                    var builder = ArrayBuilder<SourceLocation>.GetInstance();
                    foreach (var decl in Declarations)
                    {
                        SourceLocation loc = decl.NameLocation;
                        if (loc != null)
                            builder.Add(loc);
                    }
                    return builder.ToImmutableAndFree();
                }
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
                    var names = UnionCollection<string>.Create(this.Declarations, d => d.MemberNames);
                    Interlocked.CompareExchange(ref _lazyMemberNames, names, null);
                }

                return _lazyMemberNames;
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // An invariant of a merged declaration is that all of its children 
    // are also merged declarations.
    internal sealed class MergedNamespaceDeclaration : MergedNamespaceOrTypeDeclaration
    {
        private readonly ImmutableArray<SingleNamespaceDeclaration> _declarations;
        private ImmutableArray<MergedNamespaceOrTypeDeclaration> _lazyChildren;

        private MergedNamespaceDeclaration(ImmutableArray<SingleNamespaceDeclaration> declarations)
            : base(declarations.IsEmpty ? string.Empty : declarations[0].Name)
        {
            _declarations = declarations;
        }

        public static MergedNamespaceDeclaration Create(ImmutableArray<SingleNamespaceDeclaration> declarations)
        {
            return new MergedNamespaceDeclaration(declarations);
        }

        public static MergedNamespaceDeclaration Create(SingleNamespaceDeclaration declaration)
        {
            return new MergedNamespaceDeclaration(ImmutableArray.Create(declaration));
        }

        public static MergedNamespaceDeclaration Create(
            MergedNamespaceDeclaration mergedDeclaration,
            SingleNamespaceDeclaration declaration)
        {
            return new MergedNamespaceDeclaration(mergedDeclaration._declarations.Add(declaration));
        }

        public override DeclarationKind Kind
        {
            get
            {
                return DeclarationKind.Namespace;
            }
        }

        public LexicalSortKey GetLexicalSortKey(CSharpCompilation compilation)
        {
            LexicalSortKey sortKey = new LexicalSortKey(_declarations[0].NameLocation, compilation);
            for (var i = 1; i < _declarations.Length; i++)
            {
                sortKey = LexicalSortKey.First(sortKey, new LexicalSortKey(_declarations[i].NameLocation, compilation));
            }

            return sortKey;
        }

        public ImmutableArray<Location> NameLocations
        {
            get
            {
                if (_declarations.Length == 1)
                {
                    return ImmutableArray.Create<Location>(_declarations[0].NameLocation);
                }
                else
                {
                    var builder = ArrayBuilder<Location>.GetInstance();
                    foreach (var decl in _declarations)
                    {
                        SourceLocation loc = decl.NameLocation;
                        if (loc != null)
                            builder.Add(loc);
                    }
                    return builder.ToImmutableAndFree();
                }
            }
        }

        public ImmutableArray<SingleNamespaceDeclaration> Declarations
        {
            get { return _declarations; }
        }

        protected override ImmutableArray<Declaration> GetDeclarationChildren()
        {
            return StaticCast<Declaration>.From(this.Children);
        }

        private ImmutableArray<MergedNamespaceOrTypeDeclaration> MakeChildren()
        {
            ArrayBuilder<SingleNamespaceDeclaration> namespaces = null;
            ArrayBuilder<SingleTypeDeclaration> types = null;
            bool allNamespacesHaveSameName = true;
            bool allTypesHaveSameIdentity = true;

            foreach (var decl in _declarations)
            {
                foreach (var child in decl.Children)
                {
                    // it is either a type (more likely)
                    var asType = child as SingleTypeDeclaration;
                    if (asType != null)
                    {
                        // handle types
                        if (types == null)
                        {
                            types = ArrayBuilder<SingleTypeDeclaration>.GetInstance();
                        }
                        else if (allTypesHaveSameIdentity && !asType.Identity.Equals(types[0].Identity))
                        {
                            allTypesHaveSameIdentity = false;
                        }

                        types.Add(asType);
                        continue;
                    }

                    // or it is a namespace
                    var asNamespace = child as SingleNamespaceDeclaration;
                    if (asNamespace != null)
                    {
                        // handle namespace
                        if (namespaces == null)
                        {
                            namespaces = ArrayBuilder<SingleNamespaceDeclaration>.GetInstance();
                        }
                        else if (allNamespacesHaveSameName && !asNamespace.Name.Equals(namespaces[0].Name))
                        {
                            allNamespacesHaveSameName = false;
                        }

                        namespaces.Add(asNamespace);
                        continue;
                    }

                    // Not sure if we can get here, perhaps, if we have errors, 
                    // but we care only about types and namespaces anyways.
                }
            }

            var children = ArrayBuilder<MergedNamespaceOrTypeDeclaration>.GetInstance();

            if (namespaces != null)
            {
                if (allNamespacesHaveSameName)
                {
                    children.Add(MergedNamespaceDeclaration.Create(namespaces.ToImmutableAndFree()));
                }
                else
                {
                    var namespaceGroups = namespaces.ToDictionary(n => n.Name, StringOrdinalComparer.Instance);
                    namespaces.Free();

                    foreach (var namespaceGroup in namespaceGroups.Values)
                    {
                        children.Add(MergedNamespaceDeclaration.Create(namespaceGroup));
                    }
                }
            }

            if (types != null)
            {
                if (allTypesHaveSameIdentity)
                {
                    children.Add(new MergedTypeDeclaration(types.ToImmutableAndFree()));
                }
                else
                {
                    var typeGroups = types.ToDictionary(t => t.Identity);
                    types.Free();

                    foreach (var typeGroup in typeGroups.Values)
                    {
                        children.Add(new MergedTypeDeclaration(typeGroup));
                    }
                }
            }

            return children.ToImmutableAndFree();
        }

        public new ImmutableArray<MergedNamespaceOrTypeDeclaration> Children
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
    }
}

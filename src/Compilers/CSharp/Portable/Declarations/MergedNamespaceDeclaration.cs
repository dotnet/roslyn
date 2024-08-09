// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
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

            addNamespacesToChildren(namespaces, allNamespacesHaveSameName, children);
            addTypesToChildren(types, allTypesHaveSameIdentity, children);

            return children.ToImmutableAndFree();

            static void addNamespacesToChildren(ArrayBuilder<SingleNamespaceDeclaration> namespaces, bool allNamespacesHaveSameName, ArrayBuilder<MergedNamespaceOrTypeDeclaration> children)
            {
                if (namespaces != null)
                {
                    if (allNamespacesHaveSameName)
                    {
                        children.Add(MergedNamespaceDeclaration.Create(namespaces.ToImmutableAndFree()));
                    }
                    else
                    {
                        // PERF: Don't use ArrayBuilder.ToDictionary directly as it requires an extra dictionary allocation. Other options such
                        // as MultiDictionary<string, SingleNamespaceDeclaration> and Dictionary<string, OneOrMany<SingleNamespaceDeclaration>>
                        // are even less appealing as they don't perform well when their value sets grow to contain a large number of items,
                        // as typically happens when processing the namespaces.
                        var namespaceGroups = new Dictionary<string, ArrayBuilder<SingleNamespaceDeclaration>>(StringOrdinalComparer.Instance);

                        foreach (var n in namespaces)
                        {
                            var builder = namespaceGroups.GetOrAdd(n.Name, static () => ArrayBuilder<SingleNamespaceDeclaration>.GetInstance());

                            builder.Add(n);
                        }

                        namespaces.Free();

                        foreach (var namespaceGroup in namespaceGroups.Values)
                        {
                            children.Add(MergedNamespaceDeclaration.Create(namespaceGroup.ToImmutableAndFree()));
                        }
                    }
                }
            }

            static void addTypesToChildren(ArrayBuilder<SingleTypeDeclaration> types, bool allTypesHaveSameIdentity, ArrayBuilder<MergedNamespaceOrTypeDeclaration> children)
            {
                if (types != null)
                {
                    if (allTypesHaveSameIdentity)
                    {
                        children.Add(new MergedTypeDeclaration(types.ToImmutableAndFree()));
                    }
                    else
                    {
                        // PERF: Use object as the value in this dictionary to efficiently represent single item collections.
                        // If only a single object has been seen with a given identity, the value will be a SingleTypeDeclaration,
                        // otherwise, the value will be an ArrayBuilder<SingleTypeDeclaration>. This code differs from
                        // addNamespacesToChildren intentionally as the vast majority of identities are represented by only a
                        // single item in the types collection.
                        var typeGroups = PooledDictionary<SingleTypeDeclaration.TypeDeclarationIdentity, object>.GetInstance();

                        foreach (var t in types)
                        {
                            var id = t.Identity;

                            if (typeGroups.TryGetValue(id, out var existingValue))
                            {
                                if (existingValue is not ArrayBuilder<SingleTypeDeclaration> builder)
                                {
                                    builder = ArrayBuilder<SingleTypeDeclaration>.GetInstance();
                                    builder.Add((SingleTypeDeclaration)existingValue);
                                    typeGroups[id] = builder;
                                }

                                builder.Add(t);
                            }
                            else
                            {
                                typeGroups.Add(id, t);
                            }
                        }

                        foreach (var typeGroup in typeGroups.Values)
                        {
                            if (typeGroup is SingleTypeDeclaration t)
                            {
                                children.Add(new MergedTypeDeclaration([t]));
                            }
                            else
                            {
                                var builder = (ArrayBuilder<SingleTypeDeclaration>)typeGroup;
                                children.Add(new MergedTypeDeclaration(builder.ToImmutableAndFree()));
                            }
                        }

                        types.Free();
                        typeGroups.Free();
                    }
                }
            }
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

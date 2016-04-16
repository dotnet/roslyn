// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class RootSingleNamespaceDeclaration : SingleNamespaceDeclaration
    {
        private readonly ImmutableArray<ReferenceDirective> _referenceDirectives;
        private readonly bool _hasAssemblyAttributes;
        private readonly bool _hasUsings;
        private readonly bool _hasExternAliases;

        public RootSingleNamespaceDeclaration(
            bool hasUsings,
            bool hasExternAliases,
            SyntaxReference treeNode,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<ReferenceDirective> referenceDirectives,
            bool hasAssemblyAttributes)
            : base(string.Empty,
                   treeNode,
                   nameLocation: new SourceLocation(treeNode),
                   children: children)
        {
            Debug.Assert(!referenceDirectives.IsDefault);

            _referenceDirectives = referenceDirectives;
            _hasAssemblyAttributes = hasAssemblyAttributes;
            _hasUsings = hasUsings;
            _hasExternAliases = hasExternAliases;
        }

        public ImmutableArray<ReferenceDirective> ReferenceDirectives
        {
            get
            {
                return _referenceDirectives;
            }
        }

        public bool HasAssemblyAttributes
        {
            get
            {
                return _hasAssemblyAttributes;
            }
        }

        public override bool HasUsings
        {
            get
            {
                return _hasUsings;
            }
        }

        public override bool HasExternAliases
        {
            get
            {
                return _hasExternAliases;
            }
        }
    }
}

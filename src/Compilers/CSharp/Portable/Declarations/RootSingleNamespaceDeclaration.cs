// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class RootSingleNamespaceDeclaration : SingleNamespaceDeclaration
    {
        private readonly ImmutableArray<ReferenceDirective> _referenceDirectives;
        private readonly bool _hasAssemblyAttributes;
        private readonly bool _hasGlobalUsings;
        private readonly bool _hasUsings;
        private readonly bool _hasExternAliases;

        /// <summary>
        /// Any special attributes we may be referencing directly through a global using alias in the file.
        /// <c>global using X = System.Runtime.CompilerServices.TypeForwardedToAttribute</c>.
        /// </summary>
        public QuickAttributes GlobalAliasedQuickAttributes { get; }

        public RootSingleNamespaceDeclaration(
            bool hasGlobalUsings,
            bool hasUsings,
            bool hasExternAliases,
            SyntaxReference treeNode,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<ReferenceDirective> referenceDirectives,
            bool hasAssemblyAttributes,
            ImmutableArray<Diagnostic> diagnostics,
            QuickAttributes globalAliasedQuickAttributes)
            : base(string.Empty,
                   treeNode,
                   nameLocation: new SourceLocation(treeNode),
                   children: children,
                   diagnostics: diagnostics)
        {
            Debug.Assert(!referenceDirectives.IsDefault);

            _referenceDirectives = referenceDirectives;
            _hasAssemblyAttributes = hasAssemblyAttributes;
            _hasGlobalUsings = hasGlobalUsings;
            _hasUsings = hasUsings;
            _hasExternAliases = hasExternAliases;
            GlobalAliasedQuickAttributes = globalAliasedQuickAttributes;
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

        public override bool HasGlobalUsings
        {
            get
            {
                return _hasGlobalUsings;
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

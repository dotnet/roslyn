// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class RootSingleNamespaceDeclaration : SingleNamespaceDeclaration
    {
        private readonly ImmutableArray<Diagnostic> referenceDirectiveDiagnostics;
        private readonly ImmutableArray<ReferenceDirective> referenceDirectives;
        private readonly bool hasAssemblyAttributes;
        private readonly bool hasUsings;
        private readonly bool hasExternAliases;

        public RootSingleNamespaceDeclaration(
            bool hasUsings,
            bool hasExternAliases,
            SyntaxReference treeNode,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<ReferenceDirective> referenceDirectives,
            ImmutableArray<Diagnostic> referenceDirectiveDiagnostics,
            bool hasAssemblyAttributes)
            : base(string.Empty,
                   treeNode,
                   nameLocation: new SourceLocation(treeNode),
                   children: children)
        {
            Debug.Assert(!referenceDirectives.IsDefault);
            Debug.Assert(!referenceDirectiveDiagnostics.IsDefault);

            this.referenceDirectives = referenceDirectives;
            this.referenceDirectiveDiagnostics = referenceDirectiveDiagnostics;
            this.hasAssemblyAttributes = hasAssemblyAttributes;
            this.hasUsings = hasUsings;
            this.hasExternAliases = hasExternAliases;
        }

        public ImmutableArray<Diagnostic> ReferenceDirectiveDiagnostics
        {
            get
            {
                return referenceDirectiveDiagnostics;
            }
        }

        public ImmutableArray<ReferenceDirective> ReferenceDirectives
        {
            get
            {
                return referenceDirectives;
            }
        }

        public bool HasAssemblyAttributes
        {
            get
            {
                return hasAssemblyAttributes;
            }
        }

        public override bool HasUsings
        {
            get
            {
                return hasUsings;
            }
        }

        public override bool HasExternAliases
        {
            get
            {
                return hasExternAliases;
            }
        }

    }
}
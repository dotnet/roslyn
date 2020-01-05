// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SingleNamespaceDeclarationEx : SingleNamespaceDeclaration
    {
        private readonly bool _hasUsings;
        private readonly bool _hasExternAliases;

        public SingleNamespaceDeclarationEx(
            string name, bool hasUsings, bool hasExternAliases,
            SyntaxReference syntaxReference, SourceLocation nameLocation,
            ImmutableArray<SingleNamespaceOrTypeDeclaration> children,
            ImmutableArray<Diagnostic> diagnostics)
            : base(name, syntaxReference, nameLocation, children, diagnostics)
        {
            _hasUsings = hasUsings;
            _hasExternAliases = hasExternAliases;
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

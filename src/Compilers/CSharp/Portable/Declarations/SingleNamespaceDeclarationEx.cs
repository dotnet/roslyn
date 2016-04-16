// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SingleNamespaceDeclarationEx : SingleNamespaceDeclaration
    {
        private readonly bool _hasUsings;
        private readonly bool _hasExternAliases;

        public SingleNamespaceDeclarationEx(string name, bool hasUsings, bool hasExternAliases, SyntaxReference syntaxReference, SourceLocation nameLocation, ImmutableArray<SingleNamespaceOrTypeDeclaration> children) : base(name, syntaxReference, nameLocation, children)
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

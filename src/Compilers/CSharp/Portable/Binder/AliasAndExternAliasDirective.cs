﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct AliasAndExternAliasDirective
    {
        public readonly AliasSymbol Alias;
        public readonly ExternAliasDirectiveSyntax ExternAliasDirective;

        public AliasAndExternAliasDirective(AliasSymbol alias, ExternAliasDirectiveSyntax externAliasDirective)
        {
            this.Alias = alias;
            this.ExternAliasDirective = externAliasDirective;
        }
    }
}

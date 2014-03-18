// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WhileBinder : LoopBinder
    {
        public WhileBinder(MethodSymbol owner, Binder enclosing, StatementSyntax syntax)
            : base(owner, enclosing)
        {
            Debug.Assert(syntax != null);
        }
    }
}
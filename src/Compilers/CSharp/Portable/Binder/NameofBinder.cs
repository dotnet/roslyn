// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class NameofBinder : TypeofOrNameofBinder
    {
        private readonly SyntaxNode _nameofArgument;

        public NameofBinder(ExpressionSyntax nameofArgument, Binder next) : base(nameofArgument, next, next.Flags)
        {
            _nameofArgument = nameofArgument;
        }

        protected override SyntaxNode EnclosingNameofArgument => _nameofArgument;
    }
}

// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class NameofBinder : Binder
    {
        private readonly SyntaxNode nameofArgument;

        public NameofBinder(SyntaxNode nameofArgument, Binder next) : base(next)
        {
            this.nameofArgument = nameofArgument;
        }

        protected override bool IsNameofArgument(SyntaxNode possibleNameofArgument)
        {
            return possibleNameofArgument == this.nameofArgument;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundLocalFunctionStatement
    {
        public BoundBlock? Body { get => BlockBody ?? ExpressionBody; }

        public ImmutableArray<BoundAttribute> BoundAttributes { get; }
        public ImmutableArray<BoundAttribute> ReturnBoundAttributes { get; }

        public BoundLocalFunctionStatement(SyntaxNode syntax, LocalFunctionSymbol symbol, BoundBlock? blockBody, BoundBlock? expressionBody, ImmutableArray<BoundAttribute> boundAttributes, ImmutableArray<BoundAttribute> returnBoundAttributes, bool hasErrors = false)
            : this(syntax, symbol, blockBody, expressionBody, hasErrors: hasErrors)
        {
            BoundAttributes = boundAttributes;
            ReturnBoundAttributes = returnBoundAttributes;
        }
    }
}

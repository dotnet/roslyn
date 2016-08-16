// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Syntax;
using CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxListBuilder : AbstractSyntaxListBuilder
    {
        public SyntaxListBuilder(int size) : base(size)
        {
        }

        public bool Any(SyntaxKind kind) => Any((int)kind);

        public static implicit operator SyntaxList<SyntaxNode>(SyntaxListBuilder builder)
        {
            if (builder == null)
            {
                return default(SyntaxList<SyntaxNode>);
            }

            return builder.ToList();
        }
    }
}
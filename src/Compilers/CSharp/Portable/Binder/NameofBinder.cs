// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class NameofBinder : TypeofBinder
    {
        private readonly SyntaxNode _nameofArgument;

        public NameofBinder(ExpressionSyntax nameofArgument, Binder next)
            : base(nameofArgument, next, next.Flags)
        {
            _nameofArgument = nameofArgument;
        }

        protected override SyntaxNode EnclosingNameofArgument => _nameofArgument;
    }
}

﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class BaseExpressionSyntax
    {
        public BaseExpressionSyntax Update(SyntaxToken token)
        {
            return Update(token, typeClause: null);
        }
    }
}

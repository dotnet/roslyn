// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ObjectCreationExpressionSyntax ObjectCreationExpression(TypeSyntax type)
        {
            return SyntaxFactory.ObjectCreationExpression(type, default(ArgumentListSyntax), default(InitializerExpressionSyntax));
        }
    }
}

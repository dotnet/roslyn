// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class StackAllocArrayCreationExpressionSyntax
    {
        public StackAllocArrayCreationExpressionSyntax Update(SyntaxToken stackAllocKeyword, TypeSyntax type)
            => Update(StackAllocKeyword, type, default(InitializerExpressionSyntax));
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static StackAllocArrayCreationExpressionSyntax StackAllocArrayCreationExpression(SyntaxToken stackAllocKeyword, TypeSyntax type)
            => StackAllocArrayCreationExpression(stackAllocKeyword, type, default(InitializerExpressionSyntax));
    }
}

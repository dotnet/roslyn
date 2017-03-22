// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public sealed class CSharpSyntaxNodeGetOperationWalker
        : SyntaxNodeGetOperationWalker
    {
        protected override bool IsSyntaxNodeKindExcluded(SyntaxNode node)
        {
            return node is CompilationUnitSyntax
                || node is UsingDirectiveSyntax 
                || node is NamespaceDeclarationSyntax
                || node is ClassDeclarationSyntax;
        }
    }
}

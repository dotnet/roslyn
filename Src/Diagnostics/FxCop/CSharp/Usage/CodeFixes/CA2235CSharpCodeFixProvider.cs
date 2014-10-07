// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage
{
    [ExportCodeFixProvider("CA2237 CodeFix provider", LanguageNames.CSharp), Shared]
    public class CA2235CSharpCodeFixProvider : CA2235CodeFixProviderBase
    {
        protected override SyntaxNode GetFieldDeclarationNode(SyntaxNode node)
        {
            var fieldNode = node;
            while (fieldNode != null && fieldNode.CSharpKind() != SyntaxKind.FieldDeclaration)
            {
                fieldNode = fieldNode.Parent;
            }

            return fieldNode.CSharpKind() == SyntaxKind.FieldDeclaration ? fieldNode : null;
        }
    }
}

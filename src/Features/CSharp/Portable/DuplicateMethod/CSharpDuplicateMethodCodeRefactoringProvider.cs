// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DuplicateMethod;

namespace Microsoft.CodeAnalysis.CSharp.DuplicateMethod
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpDuplicateMethodCodeRefactoringProvider)), Shared]
    internal sealed class CSharpDuplicateMethodCodeRefactoringProvider
       : AbstractDuplicateMethodCodeRefactoringProvider<MethodDeclarationSyntax>
    {
        protected override SyntaxToken GetIdentifier(MethodDeclarationSyntax method)
            => method.Identifier;

        protected override MethodDeclarationSyntax WithName(MethodDeclarationSyntax method, string name) 
            => method.WithIdentifier(SyntaxFactory.Identifier(name));
    }
}

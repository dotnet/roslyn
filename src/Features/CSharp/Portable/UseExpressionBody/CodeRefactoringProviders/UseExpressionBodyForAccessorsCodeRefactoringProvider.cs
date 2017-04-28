// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForAccessorsCodeRefactoringProvider : AbstractUseExpressionBodyCodeRefactoringProvider<AccessorDeclarationSyntax>
    {
        public UseExpressionBodyForAccessorsCodeRefactoringProvider()
            : base(new UseExpressionBodyForAccessorsHelper())
        {
        }
    }
}
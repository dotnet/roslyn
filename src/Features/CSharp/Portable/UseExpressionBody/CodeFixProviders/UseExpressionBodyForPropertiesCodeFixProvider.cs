// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class UseExpressionBodyForPropertiesCodeFixProvider : AbstractUseExpressionBodyCodeFixProvider<PropertyDeclarationSyntax>
    {
        public UseExpressionBodyForPropertiesCodeFixProvider()
            : base(IDEDiagnosticIds.UseExpressionBodyForPropertiesDiagnosticId,
                   UseExpressionBodyForPropertiesHelper.Instance)
        {
        }
    }
}
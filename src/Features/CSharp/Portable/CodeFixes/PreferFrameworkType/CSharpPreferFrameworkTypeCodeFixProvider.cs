// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.PreferFrameworkType;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.PreferFrameworkType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.PreferFrameworkType), Shared]
    internal class CSharpPreferFrameworkTypeCodeFixProvider : AbstractPreferFrameworkTypeCodeFixProvider
    {
        protected override SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol) => symbol.GenerateTypeSyntax();
    }
}

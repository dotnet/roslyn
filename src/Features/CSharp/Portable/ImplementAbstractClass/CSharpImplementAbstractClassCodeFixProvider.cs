// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.ImplementAbstractClass;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementAbstractClass), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateType)]
    internal class CSharpImplementAbstractClassCodeFixProvider :
        AbstractImplementAbstractClassCodeFixProvider<ClassDeclarationSyntax>
    {
        private const string CS0534 = nameof(CS0534); // 'Program' does not implement inherited abstract member 'Goo.bar()'

        [ImportingConstructor]
        public CSharpImplementAbstractClassCodeFixProvider()
            : base(CS0534)
        {
        }
    }
}

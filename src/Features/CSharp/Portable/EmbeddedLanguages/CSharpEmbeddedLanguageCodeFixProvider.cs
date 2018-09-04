// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpEmbeddedLanguageCodeFixProvider)), Shared]
    internal class CSharpEmbeddedLanguageCodeFixProvider : AbstractEmbeddedLanguageCodeFixProvider
    {
        public CSharpEmbeddedLanguageCodeFixProvider()
            : base(CSharpEmbeddedLanguagesProvider.Instance)
        {
        }
    }
}

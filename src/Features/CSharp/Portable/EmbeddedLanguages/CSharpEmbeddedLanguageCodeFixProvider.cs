// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpEmbeddedLanguageCodeFixProvider)), Shared]
    internal class CSharpEmbeddedLanguageCodeFixProvider : AbstractEmbeddedLanguageCodeFixProvider
    {
        public CSharpEmbeddedLanguageCodeFixProvider()
            : base(CSharpEmbeddedLanguageFeaturesProvider.Instance)
        {
        }
    }
}

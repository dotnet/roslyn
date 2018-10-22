// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices
{
    [ExportLanguageService(typeof(IEmbeddedLanguagesProvider), LanguageNames.CSharp), Shared]
    internal class CSharpEmbeddedLanguagesProvider : AbstractEmbeddedLanguagesProvider
    {
        public static EmbeddedLanguageInfo Info = new EmbeddedLanguageInfo(
            (int)SyntaxKind.StringLiteralToken,
            (int)SyntaxKind.InterpolatedStringTextToken,
            CSharpSyntaxFactsService.Instance,
            CSharpSemanticFactsService.Instance,
            CSharpVirtualCharService.Instance);
        public static IEmbeddedLanguagesProvider Instance = new CSharpEmbeddedLanguagesProvider();

        public CSharpEmbeddedLanguagesProvider() : base(Info)
        {
        }
    }
}

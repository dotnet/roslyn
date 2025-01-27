// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.JsonDetection), Shared]
internal class CSharpJsonDetectionCodeFixProvider : AbstractJsonDetectionCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpJsonDetectionCodeFixProvider()
        : base(CSharpEmbeddedLanguagesProvider.Info)
    {
    }

    protected override void AddComment(SyntaxEditor editor, SyntaxToken stringLiteral, string commentContents)
        => EmbeddedLanguageUtilities.AddComment(editor, stringLiteral, commentContents);
}

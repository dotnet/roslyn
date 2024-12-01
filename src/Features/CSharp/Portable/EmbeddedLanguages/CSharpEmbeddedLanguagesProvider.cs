// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.Features.EmbeddedLanguages;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;

[ExportLanguageService(typeof(IEmbeddedLanguagesProvider), LanguageNames.CSharp, ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEmbeddedLanguagesProvider() : AbstractEmbeddedLanguagesProvider(Info)
{
    public static readonly EmbeddedLanguageInfo Info = new(
        CSharpBlockFacts.Instance,
        CSharpSyntaxFacts.Instance,
        CSharpSemanticFactsService.Instance,
        CSharpVirtualCharService.Instance);

    public override string EscapeText(string text, SyntaxToken token)
        => EmbeddedLanguageUtilities.EscapeText(text, token);
}

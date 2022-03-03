// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices
{
    [ExportLanguageService(typeof(IEmbeddedLanguagesProvider), LanguageNames.CSharp, ServiceLayer.Default), Shared]
    internal class CSharpEmbeddedLanguagesProvider : AbstractEmbeddedLanguagesProvider
    {
        public static EmbeddedLanguageInfo Info = new(
            CSharpSyntaxFacts.Instance,
            CSharpSemanticFactsService.Instance,
            CSharpVirtualCharService.Instance);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEmbeddedLanguagesProvider()
            : base(Info)
        {
        }
    }
}

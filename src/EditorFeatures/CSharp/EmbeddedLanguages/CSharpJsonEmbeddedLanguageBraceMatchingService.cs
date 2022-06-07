// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EmbeddedLanguages
{
    [ExportEmbeddedLanguageBraceMatcherInternal(
        PredefinedEmbeddedLanguageBraceMatcherNames.Json, LanguageNames.CSharp, supportsUnannotatedAPIs: true, "Json"), Shared]
    internal sealed class CSharpJsonEmbeddedLanguageBraceMatcher :
        AbstractJsonEmbeddedLanguageBraceMatcher
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpJsonEmbeddedLanguageBraceMatcher()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}

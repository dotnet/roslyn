// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EmbeddedLanguages
{
    [ExportEmbeddedLanguageBraceMatchingServiceInternal(
        PredefinedEmbeddedLanguageClassifierNames.Json, LanguageNames.CSharp, supportsUnannotatedAPIs: true, "Json"), Shared]
    internal sealed class CSharpJsonEmbeddedLanguageBraceMatchingService :
        AbstractJsonEmbeddedLanguageBraceMatchingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpJsonEmbeddedLanguageBraceMatchingService()
            : base(CSharpEmbeddedLanguagesProvider.Info)
        {
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

[ExportQuickInfoProvider(QuickInfoProviderNames.EmbeddedLanguages, LanguageNames.CSharp)]
[ExtensionOrder(Before = QuickInfoProviderNames.Semantic)]
[Shared]
internal sealed class CSharpEmbeddedLanguageQuickInfoProvider : AbstractEmbeddedLanguageQuickInfoProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpEmbeddedLanguageQuickInfoProvider([ImportMany] IEnumerable<Lazy<IEmbeddedLanguageQuickInfoProvider, EmbeddedLanguageMetadata>> services)
        : base(LanguageNames.CSharp, CSharpEmbeddedLanguagesProvider.Info, CSharpSyntaxKinds.Instance, services)
    {
    }
}

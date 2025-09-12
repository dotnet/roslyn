// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

[ExportLanguageServiceFactory(typeof(QuickInfoService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpQuickInfoServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        => new CSharpQuickInfoService(languageServices.LanguageServices);

    private sealed class CSharpQuickInfoService(LanguageServices services) : QuickInfoServiceWithProviders(services);
}

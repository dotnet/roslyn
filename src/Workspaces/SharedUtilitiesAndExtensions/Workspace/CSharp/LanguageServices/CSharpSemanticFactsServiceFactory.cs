// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageServiceFactory(typeof(ISemanticFactsService), LanguageNames.CSharp), Shared]
internal sealed class CSharpSemanticFactsServiceFactory : ILanguageServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSemanticFactsServiceFactory()
    {
    }

    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        => CSharpSemanticFactsService.Instance;
}

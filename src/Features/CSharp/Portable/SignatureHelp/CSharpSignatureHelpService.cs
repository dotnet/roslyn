// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportLanguageServiceFactory(typeof(SignatureHelpService), LanguageNames.CSharp), Shared]
internal class CSharpSignatureHelpServiceFactory : ILanguageServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSignatureHelpServiceFactory()
    {
    }

    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        => new CSharpSignatureHelpService(languageServices.LanguageServices);
}

internal class CSharpSignatureHelpService : SignatureHelpServiceWithProviders
{
    internal CSharpSignatureHelpService(LanguageServices services)
        : base(services)
    {
    }
}

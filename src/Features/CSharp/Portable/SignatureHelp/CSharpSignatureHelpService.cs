// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    [ExportLanguageServiceFactory(typeof(SignatureHelpService), LanguageNames.CSharp), Shared]
    internal class CSharpCompletionServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpSignatureHelpService();
        }
    }

    internal class CSharpSignatureHelpService : CommonSignatureHelpService
    {
        public override string Language => LanguageNames.CSharp;
    }
}

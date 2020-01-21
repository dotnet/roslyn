// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageServiceFactory(typeof(ISyntaxFactsService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxFactsServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpSyntaxFactsServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => CSharpSyntaxFactsService.Instance;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [ExportLanguageServiceFactory(typeof(INoCompilationLanguageService), NoCompilationConstants.LanguageName), Shared]
    internal class NoCompilationLanguageServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public NoCompilationLanguageServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new NoCompilationLanguageService();
        }

        private class NoCompilationLanguageService : INoCompilationLanguageService
        {
        }
    }

    internal interface INoCompilationLanguageService : ILanguageService
    {
    }
}

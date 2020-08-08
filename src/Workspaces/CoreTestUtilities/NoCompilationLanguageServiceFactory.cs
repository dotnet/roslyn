// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [ExportLanguageServiceFactory(typeof(INoCompilationLanguageService), NoCompilationConstants.LanguageName, ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal class NoCompilationLanguageServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NoCompilationLanguageServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new NoCompilationLanguageService();

        private class NoCompilationLanguageService : INoCompilationLanguageService
        {
        }
    }

    internal interface INoCompilationLanguageService : ILanguageService
    {
    }
}

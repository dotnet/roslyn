// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    [ExportLanguageServiceFactory(typeof(ILibraryService), LanguageNames.CSharp), Shared]
    internal class CSharpLibraryServiceFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpLibraryService();
        }
    }
}

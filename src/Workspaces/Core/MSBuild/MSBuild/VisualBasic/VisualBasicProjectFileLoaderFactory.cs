// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(IProjectFileLoader), LanguageNames.VisualBasic)]
    [ProjectFileExtension("vbproj")]
    internal class VisualBasicProjectFileLoaderFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public VisualBasicProjectFileLoaderFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new VisualBasicProjectFileLoader();
        }
    }
}

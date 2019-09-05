// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Shared]
    [ExportLanguageServiceFactory(typeof(IProjectFileLoader), LanguageNames.CSharp)]
    [ProjectFileExtension("csproj")]
    internal class CSharpProjectFileLoaderFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpProjectFileLoaderFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpProjectFileLoader();
        }
    }
}

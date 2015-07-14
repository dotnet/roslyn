// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageServiceFactory(typeof(IProjectFileLoader), LanguageNames.CSharp)]
    [Shared]
    [ProjectFileExtension("csproj")]
    [ProjectTypeGuid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC")]
    internal class CSharpProjectFileLoaderFactory : ILanguageServiceFactory
    {
        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpProjectFileLoader();
        }
    }
}
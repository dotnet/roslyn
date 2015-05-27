// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
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
        private IProjectFileLoader s_Loader;

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (s_Loader == null)
            {
                Interlocked.CompareExchange(ref s_Loader, RemoteProjectFileLoader.CreateProjectLoader(typeof(CSharpProjectFileLoader)), null);
            }

            return s_Loader;
        }
    }
}
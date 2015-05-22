// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    [ExportLanguageServiceFactory(typeof(IProjectFileLoader), LanguageNames.VisualBasic), Shared()]
    [ProjectFileExtension("vbproj")]
    [ProjectTypeGuid("F184B08F-C81C-45F6-A57F-5ABD9991F28F")]
    internal class VisualBasicProjectFileLoaderFactory : ILanguageServiceFactory
    {
        private IProjectFileLoader s_Loader;

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            if (s_Loader == null)
            {
                Interlocked.CompareExchange(ref s_Loader, RemoteProjectFileLoader.CreateProjectLoader(typeof(VisualBasicProjectFileLoader)), null);
            }

            return s_Loader;
        }
    }
}
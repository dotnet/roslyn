// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualBasicProjectFileLoaderFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new VisualBasicProjectFileLoader();
        }
    }
}

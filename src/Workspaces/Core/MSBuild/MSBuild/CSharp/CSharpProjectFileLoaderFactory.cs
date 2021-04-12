// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpProjectFileLoaderFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new CSharpProjectFileLoader();
        }
    }
}

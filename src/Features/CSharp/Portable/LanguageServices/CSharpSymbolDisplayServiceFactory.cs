// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    [ExportLanguageServiceFactory(typeof(ISymbolDisplayService), LanguageNames.CSharp), Shared]
    internal partial class CSharpSymbolDisplayServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSymbolDisplayServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
            => new CSharpSymbolDisplayService(provider.LanguageServices);
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;

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
        {
            return new CSharpSymbolDisplayService(provider);
        }
    }
}

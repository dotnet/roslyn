// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    [ExportLanguageServiceFactory(typeof(ICodeCleanerService), LanguageNames.CSharp), Shared]
    internal class CSharpCodeCleanerServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpCodeCleanerServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpCodeCleanerService();
        }
    }
}

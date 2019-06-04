// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    [ExportLanguageServiceFactory(typeof(ICodeGenerationService), LanguageNames.CSharp), Shared]
    internal partial class CSharpCodeGenerationServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpCodeGenerationServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpCodeGenerationService(provider);
        }
    }
}

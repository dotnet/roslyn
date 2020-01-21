// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    [ExportLanguageServiceFactory(typeof(ISyntaxTriviaService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxTriviaServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpSyntaxTriviaServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpSyntaxTriviaService(provider);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource
{
    [ExportLanguageServiceFactory(typeof(IDecompiledSourceService), LanguageNames.CSharp), Shared]
    internal partial class CSharpDecompiledSourceServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpDecompiledSourceServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpDecompiledSourceService(provider);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource
{
    [ExportLanguageServiceFactory(typeof(IMetadataAsSourceService), LanguageNames.CSharp), Shared]
    internal class CSharpMetadataAsSourceServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpMetadataAsSourceServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpMetadataAsSourceService(provider);
        }
    }
}

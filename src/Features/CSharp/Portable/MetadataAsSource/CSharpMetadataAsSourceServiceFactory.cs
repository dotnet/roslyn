﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

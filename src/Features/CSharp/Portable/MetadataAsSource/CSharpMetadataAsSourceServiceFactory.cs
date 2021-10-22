// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMetadataAsSourceServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
            => CSharpMetadataAsSourceService.Instance;
    }
}

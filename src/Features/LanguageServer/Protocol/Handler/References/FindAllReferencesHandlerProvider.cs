// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider, Shared]
    internal class FindAllReferencesHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindAllReferencesHandlerProvider(IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            return ImmutableArray.Create(new FindAllReferencesHandler(_metadataAsSourceFileService));
        }
    }
}

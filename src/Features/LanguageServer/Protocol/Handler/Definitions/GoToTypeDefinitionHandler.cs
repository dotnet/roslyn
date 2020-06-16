﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.TextDocumentTypeDefinitionName)]
    internal class GoToTypeDefinitionHandler : AbstractGoToDefinitionHandler<LSP.TextDocumentPositionParams, LSP.Location[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GoToTypeDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, ILspSolutionProvider solutionProvider)
            : base(metadataAsSourceFileService, solutionProvider)
        {
        }

        public override Task<LSP.Location[]> HandleRequestAsync(LSP.TextDocumentPositionParams request, LSP.ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken)
            => GetDefinitionAsync(request, typeOnly: true, clientName, cancellationToken: cancellationToken);
    }
}

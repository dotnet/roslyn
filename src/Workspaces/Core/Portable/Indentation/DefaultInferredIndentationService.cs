// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Indentation
{
    [ExportWorkspaceService(typeof(IInferredIndentationService), ServiceLayer.Default), Shared]
    internal sealed class DefaultInferredIndentationService
        : IInferredIndentationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultInferredIndentationService()
        {
        }

        public Task<DocumentOptionSet> GetDocumentOptionsWithInferredIndentationAsync(Document document, bool explicitFormat, CancellationToken cancellationToken)
        {
            // The workspaces layer doesn't have any smarts to infer spaces/tabs settings without an editorconfig, so just return
            // the document's options.
            return document.GetOptionsAsync(cancellationToken);
        }
    }
}

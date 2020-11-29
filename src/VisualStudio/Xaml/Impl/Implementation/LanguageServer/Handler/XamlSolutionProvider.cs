// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [Export(typeof(XamlSolutionProvider)), Shared]
    internal class XamlSolutionProvider : ILspSolutionProvider
    {
        private readonly XamlProjectService _projectService;
        private readonly ILspSolutionProvider _solutionProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlSolutionProvider(ILspSolutionProvider lspSolutionProvider, XamlProjectService projectService)
        {
            _solutionProvider = lspSolutionProvider;
            _projectService = projectService;
        }

        public (DocumentId?, Solution) FindDocumentAndSolution(TextDocumentIdentifier? textDocument, string? clientName)
        {
            if (textDocument is { Uri: { IsAbsoluteUri: true } documentUri })
            {
                _projectService.TrackOpenDocument(documentUri.LocalPath);
            }

            return _solutionProvider.FindDocumentAndSolution(textDocument, clientName);
        }
    }
}

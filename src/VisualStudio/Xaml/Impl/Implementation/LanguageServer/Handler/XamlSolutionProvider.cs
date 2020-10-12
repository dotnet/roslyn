// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

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

        public Solution GetCurrentSolutionForMainWorkspace()
        {
            return _solutionProvider.GetCurrentSolutionForMainWorkspace();
        }

        public ImmutableArray<Document> GetDocuments(Uri documentUri)
        {
            if (documentUri.IsAbsoluteUri)
            {
                _projectService.TrackOpenDocument(documentUri.LocalPath);
            }

            return _solutionProvider.GetDocuments(documentUri);
        }
    }
}

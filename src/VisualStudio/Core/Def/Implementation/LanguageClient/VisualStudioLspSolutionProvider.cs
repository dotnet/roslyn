// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.LanguageServerProtocol;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [Export(typeof(ILspSolutionProvider)), Shared]
    internal class VisualStudioLspSolutionProvider : ILspSolutionProvider
    {
        private readonly ILspWorkspaceRegistrationService _registrationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioLspSolutionProvider(ILspWorkspaceRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        public (DocumentId?, Solution) FindDocumentAndSolution(TextDocumentIdentifier? textDocument, string? clientName)
        {
            // Assume the first workspace registered is the main one
            var mainWorkspace = _registrationService.GetAllRegistrations().First();

            // if we weren't asked for a document, then we just return the main solution
            if (textDocument is null)
            {
                return (null, mainWorkspace.CurrentSolution);
            }

            foreach (var workspace in _registrationService.GetAllRegistrations())
            {
                var documents = workspace.CurrentSolution.GetDocuments(textDocument.Uri, clientName);

                if (!documents.IsEmpty)
                {
                    var document = documents.FindDocumentInProjectContext(textDocument);

                    return (document.Id, document.Project.Solution);
                }
            }

            // If we couldn't find the document then we don't know what solution, so just return the main one
            return (null, mainWorkspace.CurrentSolution);
        }
    }
}

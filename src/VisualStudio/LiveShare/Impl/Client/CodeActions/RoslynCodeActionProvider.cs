﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.CodeActions
{
    internal class RoslynCodeActionProvider : CodeRefactoringProvider
    {
        private readonly AbstractLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        public RoslynCodeActionProvider(AbstractLspClientServiceFactory roslynLspClientServiceFactory, IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _diagnosticAnalyzerService = diagnosticAnalyzerService ?? throw new ArgumentNullException(nameof(diagnosticAnalyzerService));
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // This provider is exported for all workspaces - so limit it to just our workspace.
            var (document, span, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind != WorkspaceKind.AnyCodeRoslynWorkspace)
            {
                return;
            }

            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var codeActionParams = new LSP.CodeActionParams
            {
                TextDocument = ProtocolConversions.DocumentToTextDocumentIdentifier(document),
                Range = ProtocolConversions.TextSpanToRange(span, text)
            };

            var results = await lspClient.RequestAsync(LSP.Methods.TextDocumentCodeAction.ToLSRequest(), codeActionParams, cancellationToken).ConfigureAwait(false);
            if (results == null)
            {
                return;
            }

            foreach (var result in results)
            {
                if (result.Value is LSP.Command lspCommand)
                {
                    // The command can either wrap a Command or a CodeAction.
                    // If a Command, leave it unchanged; we want to dispatch it to the host to execute.
                    // If a CodeAction, unwrap the CodeAction so the guest can run it locally.
                    var commandArguments = lspCommand.Arguments.Single();

                    if (LanguageServicesUtils.TryParseJson(commandArguments, out LSP.CodeAction lspCodeAction))
                    {
                        context.RegisterRefactoring(new RoslynRemoteCodeAction(document, lspCodeAction.Command, lspCodeAction.Edit, lspCodeAction.Title, lspClient));
                    }
                    else
                    {
                        context.RegisterRefactoring(new RoslynRemoteCodeAction(document, lspCommand, lspCommand?.Title, lspClient));
                    }
                }
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynCodeActionProvider : CodeRefactoringProvider
    {
        private readonly RoslynLSPClientServiceFactory _roslynLSPClientServiceFactory;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        public RoslynCodeActionProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
            _diagnosticAnalyzerService = diagnosticAnalyzerService ?? throw new ArgumentNullException(nameof(diagnosticAnalyzerService));
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // This provider is exported for all workspaces - so limit it to just our workspace.
            if (context.Document.Project.Solution.Workspace.Kind != WorkspaceKind.AnyCodeRoslynWorkspace)
            {
                return;
            }

            var lspClient = _roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

            var span = context.Span;

            var diagnostics = await _diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(context.Document, span, cancellationToken: context.CancellationToken).ConfigureAwait(false);

            var diagnostic = diagnostics?.FirstOrDefault();
            if (diagnostic != null)
            {
                span = diagnostic.TextSpan;
            }

            var codeActionParams = new LSP.CodeActionParams
            {
                TextDocument = new LSP.TextDocumentIdentifier { Uri = context.Document.GetURI() },
                Range = ProtocolConversions.TextSpanToRange(span, text)
            };

            var commands = await lspClient.RequestAsync(LSP.Methods.TextDocumentCodeAction, codeActionParams, context.CancellationToken).ConfigureAwait(false);
            if (commands == null)
            {
                return;
            }

            foreach (var command in commands)
            {
                // The command can either wrap a Command or a CodeAction.
                // If a Command, leave it unchanged; we want to dispatch it to the host to execute.
                // If a CodeAction, unwrap the CodeAction so the guest can run it locally.
                var commandArguments = command.Arguments.Single();
                var codeAction = (commandArguments is LSP.CodeAction) ? (LSP.CodeAction)commandArguments : null;
                context.RegisterRefactoring(new RoslynRemoteCodeAction(context.Document, (codeAction == null) ? command : null, codeAction, lspClient));
            }
        }
    }
}

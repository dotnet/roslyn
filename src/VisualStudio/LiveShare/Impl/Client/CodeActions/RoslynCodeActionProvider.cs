//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Cascade.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LspCodeAction = Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol.CodeAction;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynCodeActionProvider : CodeRefactoringProvider
    {
        private readonly RoslynLSPClientServiceFactory roslynLSPClientServiceFactory;
        private readonly IVsConfigurationSettings configurationSettings;
        private readonly IDiagnosticAnalyzerService diagnosticAnalyzerService;

        public RoslynCodeActionProvider(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, IVsConfigurationSettings configurationSettings, IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            this.roslynLSPClientServiceFactory = roslynLSPClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLSPClientServiceFactory));
            this.configurationSettings = configurationSettings ?? throw new ArgumentNullException(nameof(configurationSettings));
            this.diagnosticAnalyzerService = diagnosticAnalyzerService ?? throw new ArgumentNullException(nameof(diagnosticAnalyzerService));
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // This provider is exported for all workspaces - so limit it to just our workspace.
            if (context.Document.Project.Solution.Workspace.Kind != WorkspaceKind.AnyCodeRoslynWorkspace)
            {
                return;
            }

            var lspClient = this.roslynLSPClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return;
            }

            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

            var span = context.Span;

            var diagnostics = await this.diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(context.Document, span, cancellationToken: context.CancellationToken).ConfigureAwait(false);

            var diagnostic = diagnostics?.FirstOrDefault();
            if (diagnostic != null)
            {
                span = diagnostic.TextSpan;
            }

            var codeActionParams = new CodeActionParams
            {
                TextDocument = context.Document.ToTextDocumentIdentifier(),
                Range = span.ToRange(text)
            };

            Command[] commands = await lspClient.RequestAsync(Methods.TextDocumentCodeAction, codeActionParams, context.CancellationToken).ConfigureAwait(false);
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
                LspCodeAction codeAction = (commandArguments is LspCodeAction) ? (LspCodeAction) commandArguments : null;
                context.RegisterRefactoring(new RoslynRemoteCodeAction(context.Document, (codeAction == null) ? command : null, codeAction, lspClient));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handles the initial request for code actions. Leaves the Edit and Command properties of the returned
    /// VSCodeActions blank, as these properties should be populated by the CodeActionsResolveHandler only when the user
    /// requests them.
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(CodeActionsHandler)), Shared]
    [Method(LSP.Methods.TextDocumentCodeActionName)]
    internal class CodeActionsHandler : ILspServiceDocumentRequestHandler<LSP.CodeActionParams, LSP.CodeAction[]>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IGlobalOptionService _globalOptions;

        internal const string RunCodeActionCommandName = "Roslyn.RunCodeAction";
        internal const string RunFixAllCodeActionCommandName = "roslyn.client.fixAllCodeAction";
        internal const string RunNestedCodeActionCommandName = "roslyn.client.nestedCodeAction";

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IGlobalOptionService globalOptions)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _globalOptions = globalOptions;
        }

        public TextDocumentIdentifier GetTextDocumentIdentifier(CodeActionParams request) => request.TextDocument;

        public async Task<LSP.CodeAction[]> HandleRequestAsync(LSP.CodeActionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var options = _globalOptions.GetCodeActionOptionsProvider();
            var clientCapability = context.GetRequiredClientCapabilities();

            var codeActions = await CodeActionHelpers.GetVSCodeActionsAsync(
                request, document, options, _codeFixService, _codeRefactoringService, hasVsLspCapability: clientCapability.HasVisualStudioLspCapability(), cancellationToken).ConfigureAwait(false);
            return codeActions;
        }
    }
}

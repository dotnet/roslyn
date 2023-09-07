// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(CodeActionFixAllResolveHandler)), Shared]
    [Method("codeAction/resolveFixAll")]
    internal class CodeActionFixAllResolveHandler : ILspServiceDocumentRequestHandler<RoslynFixAllCodeAction, RoslynFixAllCodeAction>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionFixAllResolveHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IGlobalOptionService globalOptions)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _globalOptions = globalOptions;
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;


        public TextDocumentIdentifier GetTextDocumentIdentifier(RoslynFixAllCodeAction request)
            => ((JToken)request.Data!).ToObject<CodeActionResolveData>()!.TextDocument;

        public async Task<RoslynFixAllCodeAction> HandleRequestAsync(RoslynFixAllCodeAction request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var data = ((JToken)request.Data!).ToObject<CodeActionResolveData>();
            Assumes.Present(data);

            var options = _globalOptions.GetCodeActionOptionsProvider();

            var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
                document,
                data.Range,
                options,
                _codeFixService,
                _codeRefactoringService,
                cancellationToken,
                request.Scope).ConfigureAwait(false);

            var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(data.UniqueIdentifier, codeActions);
            Contract.ThrowIfNull(codeActionToResolve);

            var fixAllCodeAction = (FixAllCodeAction)codeActionToResolve;
            Contract.ThrowIfNull(fixAllCodeAction);

            var operations = await fixAllCodeAction.ComputeOperationsAsync(new ProgressTracker(), cancellationToken).ConfigureAwait(false);
            var edit = await CodeActionResolveHelper.GetCodeActionResolveEditsAsync(context, data, operations, cancellationToken).ConfigureAwait(false);

            request.Edit = edit;
            return request;
        }
    }
}

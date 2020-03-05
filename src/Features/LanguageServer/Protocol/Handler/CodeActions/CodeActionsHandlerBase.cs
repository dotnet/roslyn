﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class CodeActionsHandlerBase
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;

        protected const string RunCodeActionCommandName = "Roslyn.RunCodeAction";

        public CodeActionsHandlerBase(ICodeFixService codeFixService, ICodeRefactoringService codeRefactoringService)
        {
            _codeFixService = codeFixService ?? throw new ArgumentNullException(nameof(codeFixService));
            _codeRefactoringService = codeRefactoringService ?? throw new ArgumentNullException(nameof(codeRefactoringService));
        }

        public async Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Solution solution, Uri documentUri, LSP.Range selection, CancellationToken cancellationToken)
        {
            var document = solution.GetDocumentFromURI(documentUri);
            if (document == null)
            {
                return ImmutableArray<CodeAction>.Empty;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var textSpan = ProtocolConversions.RangeToTextSpan(selection, text);
            var codeFixCollections = await _codeFixService.GetFixesAsync(document, textSpan, true, cancellationToken).ConfigureAwait(false);
            var codeRefactorings = await _codeRefactoringService.GetRefactoringsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            var codeActions = codeFixCollections.SelectMany(c => c.Fixes.Select(f => f.Action)).Concat(
                                codeRefactorings.SelectMany(r => r.CodeActions.Select(ca => ca.action)));

            // Flatten out the nested codeactions.
            var nestedCodeActions = codeActions.Where(c => c is CodeAction.CodeActionWithNestedActions nc && nc.IsInlinable).SelectMany(nc => nc.NestedCodeActions);
            codeActions = codeActions.Where(c => !(c is CodeAction.CodeActionWithNestedActions)).Concat(nestedCodeActions);

            return codeActions;
        }
    }
}

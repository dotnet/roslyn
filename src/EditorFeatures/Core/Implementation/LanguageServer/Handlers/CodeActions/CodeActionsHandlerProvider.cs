// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.CodeAnalysis.Options;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Exports all the code action handlers together to ensure they
    /// share the same code actions cache state.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
    internal class CodeActionsHandlerProvider :
        IRequestHandlerProvider<CodeActionsHandler>,
        IRequestHandlerProvider<CodeActionResolveHandler>,
        IRequestHandlerProvider<RunCodeActionHandler>
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IThreadingContext _threadingContext;
        private readonly IGlobalOptionService _globalOptions;

        private readonly Lazy<CodeActionsCache> _codeActionsCache = new(() => new());

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandlerProvider(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _threadingContext = threadingContext;
            _globalOptions = globalOptions;
        }

        CodeActionsHandler IRequestHandlerProvider<CodeActionsHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
            => new(_codeActionsCache.Value, _codeFixService, _codeRefactoringService, _globalOptions);

        CodeActionResolveHandler IRequestHandlerProvider<CodeActionResolveHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
            => new(_codeActionsCache.Value, _codeFixService, _codeRefactoringService, _globalOptions);

        RunCodeActionHandler IRequestHandlerProvider<RunCodeActionHandler>.CreateRequestHandler(WellKnownLspServerKinds serverKind)
            => new(_codeActionsCache.Value, _codeFixService, _codeRefactoringService, _globalOptions, _threadingContext);
    }
}

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

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Exports all the code action handlers together to ensure they
    /// share the same code actions cache state.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(CodeActionsHandler), typeof(CodeActionResolveHandler), typeof(RunCodeActionHandler)), Shared]
    internal class CodeActionsHandlerProvider : IRequestHandlerProvider
    {
        private readonly ICodeFixService _codeFixService;
        private readonly ICodeRefactoringService _codeRefactoringService;
        private readonly IThreadingContext _threadingContext;
        private readonly IGlobalOptionService _globalOptions;

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

        public ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            var codeActionsCache = new CodeActionsCache();
            return ImmutableArray.Create<IRequestHandler>(
                new CodeActionsHandler(codeActionsCache, _codeFixService, _codeRefactoringService, _globalOptions),
                new CodeActionResolveHandler(codeActionsCache, _codeFixService, _codeRefactoringService, _globalOptions),
                new RunCodeActionHandler(codeActionsCache, _codeFixService, _codeRefactoringService, _globalOptions, _threadingContext));
        }
    }
}

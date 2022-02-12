// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    /// <summary>
    /// Exports all the code action handlers together to ensure they
    /// share the same code actions cache state.
    /// </summary>
    /// <remarks>
    /// Same as C# and VB but for XAML. See also <seealso cref="Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActionsHandlerProvider"/>.
    /// </remarks>
    [ExportLspRequestHandlerProvider(StringConstants.XamlLanguageName), Shared]
    internal class CodeActionsHandlerProvider : AbstractRequestHandlerProvider
    {
        private readonly Lazy<ICodeFixService> _codeFixService;
        private readonly Lazy<ICodeRefactoringService> _codeRefactoringService;
        private readonly Lazy<IThreadingContext> _threadingContext;
        private readonly Lazy<IGlobalOptionService> _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeActionsHandlerProvider(
            Lazy<ICodeFixService> codeFixService,
            Lazy<ICodeRefactoringService> codeRefactoringService,
            Lazy<IThreadingContext> threadingContext,
            Lazy<IGlobalOptionService> globalOptions)
        {
            _codeFixService = codeFixService;
            _codeRefactoringService = codeRefactoringService;
            _threadingContext = threadingContext;
            _globalOptions = globalOptions;
        }

        public override ImmutableArray<LazyRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            var codeActionsCache = new Lazy<CodeActionsCache>(() => new());
            return ImmutableArray.Create(
                CreateLazyRequestHandlerMetadata(() => new CodeActionsHandler(codeActionsCache.Value, _codeFixService.Value, _codeRefactoringService.Value, _globalOptions.Value)),
                CreateLazyRequestHandlerMetadata(() => new CodeActionResolveHandler(codeActionsCache.Value, _codeFixService.Value, _codeRefactoringService.Value, _globalOptions.Value)),
                CreateLazyRequestHandlerMetadata(() => new RunCodeActionHandler(codeActionsCache.Value, _codeFixService.Value, _codeRefactoringService.Value, _globalOptions.Value, _threadingContext.Value)));
        }
    }
}

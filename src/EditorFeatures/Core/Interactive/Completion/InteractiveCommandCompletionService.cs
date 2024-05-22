// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Interactive;

internal sealed class InteractiveCommandCompletionService : CompletionService
{
    [ExportLanguageServiceFactory(typeof(CompletionService), InteractiveLanguageNames.InteractiveCommand), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class Factory(IAsynchronousOperationListenerProvider listenerProvider) : ILanguageServiceFactory
    {
        private readonly IAsynchronousOperationListenerProvider _listenerProvider = listenerProvider;

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new InteractiveCommandCompletionService(languageServices.LanguageServices.SolutionServices, _listenerProvider);
    }

    private InteractiveCommandCompletionService(SolutionServices services, IAsynchronousOperationListenerProvider listenerProvider)
        : base(services, listenerProvider)
    {
    }

    public override string Language
        => InteractiveLanguageNames.InteractiveCommand;

    internal override CompletionRules GetRules(CompletionOptions options)
        => CompletionRules.Default;
}

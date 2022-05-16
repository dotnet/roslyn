// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class InteractiveCommandCompletionService : CompletionServiceWithProviders
    {
        [ExportLanguageServiceFactory(typeof(CompletionService), InteractiveLanguageNames.InteractiveCommand), Shared]
        internal sealed class Factory : ILanguageServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
                => new InteractiveCommandCompletionService(languageServices.WorkspaceServices.Workspace);
        }

        private InteractiveCommandCompletionService(Workspace workspace)
            : base(workspace)
        {
        }

        public override string Language
            => InteractiveLanguageNames.InteractiveCommand;

        internal override CompletionRules GetRules(CompletionOptions options)
            => CompletionRules.Default;
    }
}

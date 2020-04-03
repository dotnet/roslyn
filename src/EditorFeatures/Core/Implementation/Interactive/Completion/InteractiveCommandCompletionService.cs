// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Completion
{
    [ExportLanguageServiceFactory(typeof(CompletionService), InteractiveLanguageNames.InteractiveCommand), Shared]
    internal class InteractiveCommandCompletionServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveCommandCompletionServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => new InteractiveCommandCompletionService(languageServices.WorkspaceServices.Workspace);
    }

    internal class InteractiveCommandCompletionService : CompletionServiceWithProviders
    {
        public InteractiveCommandCompletionService(Workspace workspace)
            : base(workspace)
        {
        }

        public override string Language
        {
            get { return InteractiveLanguageNames.InteractiveCommand; }
        }
    }
}

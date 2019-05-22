// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public InteractiveCommandCompletionServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        {
            return new InteractiveCommandCompletionService(languageServices.WorkspaceServices.Workspace);
        }
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

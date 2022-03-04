// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp
{
    [Export(typeof(FSharpGlobalOptions)), Shared]
    internal sealed class FSharpGlobalOptions
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpGlobalOptions(IGlobalOptionService globalOptions, VisualStudioWorkspace workspace)
        {
            _globalOptions = globalOptions;
            _workspace = workspace;
        }

        public bool BlockForCompletionItems
        {
            get => _globalOptions.GetOption(CompletionViewOptions.BlockForCompletionItems, LanguageNames.FSharp);
            set => _globalOptions.SetGlobalOption(new OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.FSharp), value);
        }

        public void SetBackgroundAnalysisScope(bool openFilesOnly)
        {
            var solution = _workspace.CurrentSolution;

#pragma warning disable CS0618 // Type or member is obsolete
            _workspace.TryApplyChanges(solution.WithOptions(solution.Options
                .WithChangedOption(
                    SolutionCrawlerOptions.ClosedFileDiagnostic,
                    LanguageNames.FSharp,
                    !openFilesOnly)));
#pragma warning restore
        }
    }
}

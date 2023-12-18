﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    [Export(typeof(VSTypeScriptGlobalOptions)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class VSTypeScriptGlobalOptions(IGlobalOptionService globalOptions)
    {
        private readonly IGlobalOptionService _globalOptions = globalOptions;

        public bool BlockForCompletionItems
        {
            get => _globalOptions.GetOption(CompletionViewOptionsStorage.BlockForCompletionItems, InternalLanguageNames.TypeScript);
            set => _globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, InternalLanguageNames.TypeScript, value);
        }

        public void SetBackgroundAnalysisScope(bool openFilesOnly)
        {
            _globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript,
                openFilesOnly ? BackgroundAnalysisScope.OpenFiles : BackgroundAnalysisScope.FullSolution);

            _globalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.RemoveDocumentDiagnosticsOnDocumentClose, InternalLanguageNames.TypeScript,
                openFilesOnly);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        [Obsolete("Do not pass workspace")]
        public void SetBackgroundAnalysisScope(Workspace workspace, bool openFilesOnly)
            => SetBackgroundAnalysisScope(openFilesOnly);
#pragma warning restore

        internal IGlobalOptionService Service => _globalOptions;
    }
}

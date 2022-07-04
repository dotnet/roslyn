// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int BackgroundAnalysisScopeOption
        {
            get { return (int)GetOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption); }
            set { SetOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, (BackgroundAnalysisScope)value); }
        }

        public int CompilerDiagnosticsScopeOption
        {
            get { return (int)GetOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption); }
            set { SetOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, (CompilerDiagnosticsScope)value); }
        }
    }
}

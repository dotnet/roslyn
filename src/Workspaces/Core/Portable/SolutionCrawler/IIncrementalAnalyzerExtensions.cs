// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static partial class IIncrementalAnalyzerExtensions
    {
        public static BackgroundAnalysisScope GetOverriddenBackgroundAnalysisScope(this IIncrementalAnalyzer incrementalAnalyzer, OptionSet options, BackgroundAnalysisScope defaultBackgroundAnalysisScope)
        {
            // Unit testing analyzer has special semantics for analysis scope.
            if (incrementalAnalyzer is UnitTestingIncrementalAnalyzer)
            {
                return UnitTestingIncrementalAnalyzer.GetBackgroundAnalysisScope(options);
            }

            return defaultBackgroundAnalysisScope;
        }
    }
}

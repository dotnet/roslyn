// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static partial class IIncrementalAnalyzerExtensions
    {
        public static BackgroundAnalysisScope GetOverriddenBackgroundAnalysisScope(this IIncrementalAnalyzer incrementalAnalyzer, OptionSet options, BackgroundAnalysisScope defaultBackgroundAnalysisScope)
        {
            // Unit testing analyzer has special semantics for analysis scope.
            if (incrementalAnalyzer is UnitTestingIncrementalAnalyzer unitTestingAnalyzer)
            {
                return unitTestingAnalyzer.GetBackgroundAnalysisScope(options);
            }

            return defaultBackgroundAnalysisScope;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingPredefinedInvocationReasonsWrapper
    {
        public const string Reanalyze = PredefinedInvocationReasons.Reanalyze;
        public const string SemanticChanged = PredefinedInvocationReasons.SemanticChanged;
        public const string ProjectConfigurationChanged = PredefinedInvocationReasons.ProjectConfigurationChanged;
    }
}

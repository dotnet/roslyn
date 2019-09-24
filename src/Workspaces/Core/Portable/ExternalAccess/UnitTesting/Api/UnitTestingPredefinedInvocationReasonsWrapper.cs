// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingPredefinedInvocationReasonsWrapper
    {
        public const string Reanalyze = PredefinedInvocationReasons.Reanalyze;
        public const string SemanticChanged = PredefinedInvocationReasons.SemanticChanged;
        public const string SyntaxChanged = PredefinedInvocationReasons.SyntaxChanged;
        public const string ProjectConfigurationChanged = PredefinedInvocationReasons.ProjectConfigurationChanged;
        public const string DocumentAdded = PredefinedInvocationReasons.DocumentAdded;
        public const string DocumentOpened = PredefinedInvocationReasons.DocumentOpened;
        public const string DocumentRemoved = PredefinedInvocationReasons.DocumentRemoved;
        public const string DocumentClosed = PredefinedInvocationReasons.DocumentClosed;
        public const string HighPriority = PredefinedInvocationReasons.HighPriority;
        public const string ProjectParseOptionsChanged = PredefinedInvocationReasons.ProjectParseOptionsChanged;
        public const string SolutionRemoved = PredefinedInvocationReasons.SolutionRemoved;
    }
}

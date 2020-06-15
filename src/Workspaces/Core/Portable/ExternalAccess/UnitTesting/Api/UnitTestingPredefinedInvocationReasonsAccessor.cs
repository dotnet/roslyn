// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal static class UnitTestingPredefinedInvocationReasonsAccessor
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

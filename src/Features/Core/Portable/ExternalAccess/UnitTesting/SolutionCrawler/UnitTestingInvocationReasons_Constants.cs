// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal readonly partial struct UnitTestingInvocationReasons
    {
        public static readonly UnitTestingInvocationReasons DocumentAdded =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.DocumentAdded,
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons DocumentRemoved =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.DocumentRemoved,
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged,
                                    UnitTestingPredefinedInvocationReasons.HighPriority));

#if false // Not used in unit testing crawling
        public static readonly UnitTestingInvocationReasons ProjectParseOptionChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.ProjectParseOptionsChanged,
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));
#endif

        public static readonly UnitTestingInvocationReasons ProjectConfigurationChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.ProjectConfigurationChanged,
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons SolutionRemoved =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SolutionRemoved,
                                    UnitTestingPredefinedInvocationReasons.DocumentRemoved));

#if false // Not used in unit testing crawling
        public static readonly UnitTestingInvocationReasons DocumentOpened =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.DocumentOpened,
                                    UnitTestingPredefinedInvocationReasons.HighPriority));

        public static readonly UnitTestingInvocationReasons DocumentClosed =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.DocumentClosed,
                                    UnitTestingPredefinedInvocationReasons.HighPriority));
#endif

        public static readonly UnitTestingInvocationReasons DocumentChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons AdditionalDocumentChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons SyntaxChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged));

        public static readonly UnitTestingInvocationReasons SemanticChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons Reanalyze =
            new(UnitTestingPredefinedInvocationReasons.Reanalyze);

        public static readonly UnitTestingInvocationReasons ReanalyzeHighPriority =
            Reanalyze.With(UnitTestingPredefinedInvocationReasons.HighPriority);

#if false // Not used in unit testing crawling
        public static readonly UnitTestingInvocationReasons ActiveDocumentSwitched =
            new(UnitTestingPredefinedInvocationReasons.ActiveDocumentSwitched);
#endif
    }
}

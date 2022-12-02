// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly partial struct UnitTestingInvocationReasons
    {
        public static readonly UnitTestingInvocationReasons DocumentAdded =
            new(
                ImmutableHashSet.Create<string>(
#if false // Not used in unit testing crawling
                                    UnitTestingPredefinedInvocationReasons.DocumentAdded,
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
#endif
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons DocumentRemoved =
            new(
                ImmutableHashSet.Create<string>(
#if false // Not used in unit testing crawling
                                    UnitTestingPredefinedInvocationReasons.DocumentRemoved,
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
#endif
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
#if false // Not used in unit testing crawling
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
#endif
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

#if false
        public static readonly UnitTestingInvocationReasons SolutionRemoved =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SolutionRemoved,
                                    UnitTestingPredefinedInvocationReasons.DocumentRemoved));
#endif

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
#if false // Not used in unit testing crawling
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
#endif
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons AdditionalDocumentChanged =
            new(
                ImmutableHashSet.Create<string>(
#if false // Not used in unit testing crawling
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged,
#endif
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

#if false // Not used in unit testing crawling
        public static readonly UnitTestingInvocationReasons SyntaxChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SyntaxChanged));
#endif

        public static readonly UnitTestingInvocationReasons SemanticChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    UnitTestingPredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons Reanalyze =
            new(UnitTestingPredefinedInvocationReasons.Reanalyze);

#if false // Not used in unit testing crawling
        public static readonly UnitTestingInvocationReasons ReanalyzeHighPriority =
            Reanalyze.With(UnitTestingPredefinedInvocationReasons.HighPriority);

        public static readonly UnitTestingInvocationReasons ActiveDocumentSwitched =
            new(UnitTestingPredefinedInvocationReasons.ActiveDocumentSwitched);
#endif
    }
}

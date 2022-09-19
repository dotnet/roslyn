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
                                    PredefinedInvocationReasons.DocumentAdded,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons DocumentRemoved =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentRemoved,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged,
                                    PredefinedInvocationReasons.HighPriority));

        public static readonly UnitTestingInvocationReasons ProjectParseOptionChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.ProjectParseOptionsChanged,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons ProjectConfigurationChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.ProjectConfigurationChanged,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons SolutionRemoved =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SolutionRemoved,
                                    PredefinedInvocationReasons.DocumentRemoved));

        public static readonly UnitTestingInvocationReasons DocumentOpened =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentOpened,
                                    PredefinedInvocationReasons.HighPriority));

        public static readonly UnitTestingInvocationReasons DocumentClosed =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentClosed,
                                    PredefinedInvocationReasons.HighPriority));

        public static readonly UnitTestingInvocationReasons DocumentChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons AdditionalDocumentChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons SyntaxChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SyntaxChanged));

        public static readonly UnitTestingInvocationReasons SemanticChanged =
            new(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly UnitTestingInvocationReasons Reanalyze =
            new(PredefinedInvocationReasons.Reanalyze);

        public static readonly UnitTestingInvocationReasons ReanalyzeHighPriority =
            Reanalyze.With(PredefinedInvocationReasons.HighPriority);

        public static readonly UnitTestingInvocationReasons ActiveDocumentSwitched =
            new(PredefinedInvocationReasons.ActiveDocumentSwitched);
    }
}

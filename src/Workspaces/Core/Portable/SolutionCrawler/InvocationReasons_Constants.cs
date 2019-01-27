// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial struct InvocationReasons
    {
        public static readonly InvocationReasons DocumentAdded =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentAdded,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly InvocationReasons DocumentRemoved =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentRemoved,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged,
                                    PredefinedInvocationReasons.HighPriority));

        public static readonly InvocationReasons ProjectParseOptionChanged =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.ProjectParseOptionsChanged,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly InvocationReasons ProjectConfigurationChanged =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.ProjectConfigurationChanged,
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly InvocationReasons SolutionRemoved =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SolutionRemoved,
                                    PredefinedInvocationReasons.DocumentRemoved));

        public static readonly InvocationReasons DocumentOpened =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentOpened,
                                    PredefinedInvocationReasons.HighPriority));

        public static readonly InvocationReasons DocumentClosed =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.DocumentClosed,
                                    PredefinedInvocationReasons.HighPriority));

        public static readonly InvocationReasons DocumentChanged =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly InvocationReasons AdditionalDocumentChanged =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SyntaxChanged,
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly InvocationReasons SyntaxChanged =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SyntaxChanged));

        public static readonly InvocationReasons SemanticChanged =
            new InvocationReasons(
                ImmutableHashSet.Create<string>(
                                    PredefinedInvocationReasons.SemanticChanged));

        public static readonly InvocationReasons Reanalyze =
            new InvocationReasons(PredefinedInvocationReasons.Reanalyze);

        public static readonly InvocationReasons ReanalyzeHighPriority =
            Reanalyze.With(PredefinedInvocationReasons.HighPriority);
    }
}

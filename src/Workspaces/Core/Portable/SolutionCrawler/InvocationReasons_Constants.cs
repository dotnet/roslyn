// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.SolutionCrawler;

internal readonly partial struct InvocationReasons
{
    public static readonly InvocationReasons DocumentAdded =
        new(
            [
                PredefinedInvocationReasons.DocumentAdded,
                PredefinedInvocationReasons.SyntaxChanged,
                PredefinedInvocationReasons.SemanticChanged,
            ]);

    public static readonly InvocationReasons DocumentRemoved =
        new(
            [
                PredefinedInvocationReasons.DocumentRemoved,
                PredefinedInvocationReasons.SyntaxChanged,
                PredefinedInvocationReasons.SemanticChanged,
                PredefinedInvocationReasons.HighPriority,
            ]);

    public static readonly InvocationReasons ProjectParseOptionChanged =
        new(
            [
                PredefinedInvocationReasons.ProjectParseOptionsChanged,
                PredefinedInvocationReasons.SyntaxChanged,
                PredefinedInvocationReasons.SemanticChanged,
            ]);

    public static readonly InvocationReasons ProjectConfigurationChanged =
        new(
            [
                PredefinedInvocationReasons.ProjectConfigurationChanged,
                PredefinedInvocationReasons.SyntaxChanged,
                PredefinedInvocationReasons.SemanticChanged,
            ]);

    public static readonly InvocationReasons SolutionRemoved =
        new(
            [PredefinedInvocationReasons.SolutionRemoved, PredefinedInvocationReasons.DocumentRemoved]);

    public static readonly InvocationReasons DocumentOpened =
        new(
            [PredefinedInvocationReasons.DocumentOpened, PredefinedInvocationReasons.HighPriority]);

    public static readonly InvocationReasons DocumentClosed =
        new(
            [PredefinedInvocationReasons.DocumentClosed, PredefinedInvocationReasons.HighPriority]);

    public static readonly InvocationReasons DocumentChanged =
        new(
            [PredefinedInvocationReasons.SyntaxChanged, PredefinedInvocationReasons.SemanticChanged]);

    public static readonly InvocationReasons AdditionalDocumentChanged =
        new(
            [PredefinedInvocationReasons.SyntaxChanged, PredefinedInvocationReasons.SemanticChanged]);

    public static readonly InvocationReasons SyntaxChanged =
        new(
            [PredefinedInvocationReasons.SyntaxChanged]);

    public static readonly InvocationReasons SemanticChanged =
        new(
            [PredefinedInvocationReasons.SemanticChanged]);

    public static readonly InvocationReasons Reanalyze =
        new(PredefinedInvocationReasons.Reanalyze);

    public static readonly InvocationReasons ReanalyzeHighPriority =
        Reanalyze.With(PredefinedInvocationReasons.HighPriority);

    public static readonly InvocationReasons ActiveDocumentSwitched =
        new(PredefinedInvocationReasons.ActiveDocumentSwitched);
}

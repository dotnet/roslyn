// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Apply this annotation to a SyntaxNode to indicate that a warning message should be presented to the user.
/// </summary>
public static class WarningAnnotation
{
    public const string Kind = "CodeAction_Warning";

    public static SyntaxAnnotation Create(string description)
        => new(Kind, description);

    public static string? GetDescription(SyntaxAnnotation annotation)
        => annotation.Data;
}

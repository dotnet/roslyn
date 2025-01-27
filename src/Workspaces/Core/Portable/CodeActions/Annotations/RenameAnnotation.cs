// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Apply this annotation to an appropriate SyntaxNode to request that it should be renamed by the user after the action.
/// </summary>
public static class RenameAnnotation
{
    public const string Kind = "CodeAction_Rename";

    public static SyntaxAnnotation Create()
        => new(Kind);
}

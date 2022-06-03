// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Apply this annotation to a SyntaxNode to indicate a conflict may exist that requires user understanding and acknowledgment before taking action.
    /// </summary>
    public static class ConflictAnnotation
    {
        public const string Kind = "CodeAction_Conflict";

        public static SyntaxAnnotation Create(string description)
            => new(Kind, description);

        public static string? GetDescription(SyntaxAnnotation annotation)
            => annotation.Data;
    }
}

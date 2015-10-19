// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Apply this annotation to a SyntaxNode to indicate a conflict may exist that requires user understanding and acknowledgment before taking action.
    /// </summary>
    public static class ConflictAnnotation
    {
        public const string Kind = "CodeAction_Conflict";

        public static SyntaxAnnotation Create(string description)
        {
            return new SyntaxAnnotation(Kind, description);
        }

        public static string GetDescription(SyntaxAnnotation annotation)
        {
            return annotation.Data;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Apply this annotation to a SyntaxNode to indicate that a warning message should be presented to the user.
    /// </summary>
    public static class WarningAnnotation
    {
        public const string Kind = "CodeAction_Warning";

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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Apply this annotation to an appropriate SyntaxNode to request that it should be renamed by the user after the action.
    /// </summary>
    public static class RenameAnnotation
    {
        public const string Kind = "CodeAction_Rename";

        public static SyntaxAnnotation Create()
        {
            return new SyntaxAnnotation(Kind);
        }
    }
}

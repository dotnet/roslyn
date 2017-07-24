// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Apply this annotation to an appropriate Syntax element to request that it should be 
    /// navigated to by the user after the action.
    /// </summary>
    internal static class NavigationAnnotation
    {
        public const string Kind = "CodeAction_Navigation";

        public static SyntaxAnnotation Create()
            => new SyntaxAnnotation(Kind);
    }
}

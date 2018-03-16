// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal class SuppressDiagnosticsAnnotation
    {
        public const string Kind = "CodeAction_SuppressDiagnostics";

        public static SyntaxAnnotation Create()
        {
            return new SyntaxAnnotation(Kind);
        }
    }
}

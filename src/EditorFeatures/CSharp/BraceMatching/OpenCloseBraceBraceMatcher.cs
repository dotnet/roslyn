// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.BraceMatching
{
    [ExportBraceMatcher(LanguageNames.CSharp)]
    internal class OpenCloseBraceBraceMatcher : AbstractCSharpBraceMatcher
    {
        public OpenCloseBraceBraceMatcher()
            : base(SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken)
        {
        }
    }
}

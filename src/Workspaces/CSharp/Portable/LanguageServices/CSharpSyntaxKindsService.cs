// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpSyntaxKindsService : AbstractSyntaxKindsService
    {
        public static readonly CSharpSyntaxKindsService Instance = new CSharpSyntaxKindsService();

        private CSharpSyntaxKindsService()
        {
        }

        public override int DotToken => (int)SyntaxKind.DotToken;
        public override int QuestionToken => (int)SyntaxKind.QuestionToken;
    }
}

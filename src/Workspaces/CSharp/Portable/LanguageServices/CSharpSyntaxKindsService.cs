// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal sealed class CSharpSyntaxKindsService : AbstractSyntaxKindsService
    {
        public static readonly CSharpSyntaxKindsService Instance = new CSharpSyntaxKindsService();

        [ImportingConstructor]
        public CSharpSyntaxKindsService()
        {
        }

        public override int DotToken => (int)SyntaxKind.DotToken;
        public override int QuestionToken => (int)SyntaxKind.QuestionToken;

        public override int IfKeyword => (int)SyntaxKind.IfKeyword;

        public override int LogicalAndExpression => (int)SyntaxKind.LogicalAndExpression;
        public override int LogicalOrExpression => (int)SyntaxKind.LogicalOrExpression;
    }
}

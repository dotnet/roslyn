// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServices
{
    internal sealed class CSharpSyntaxKindsService : AbstractSyntaxKindsService
    {
        public static readonly CSharpSyntaxKindsService Instance = new CSharpSyntaxKindsService();

        private CSharpSyntaxKindsService()
        {
        }

        public override int DotToken => (int)SyntaxKind.DotToken;
        public override int QuestionToken => (int)SyntaxKind.QuestionToken;

        public override int IfKeyword => (int)SyntaxKind.IfKeyword;

        public override int LogicalAndExpression => (int)SyntaxKind.LogicalAndExpression;
        public override int LogicalOrExpression => (int)SyntaxKind.LogicalOrExpression;
        public override int EndOfFileToken => (int)SyntaxKind.EndOfFileToken;
        public override int AwaitKeyword => (int)SyntaxKind.AwaitKeyword;
        public override int IdentifierToken => (int)SyntaxKind.IdentifierToken;
        public override int GlobalKeyword => (int)SyntaxKind.GlobalKeyword;
        public override int IncompleteMember => (int)SyntaxKind.IncompleteMember;
        public override int UsingStatement => (int)SyntaxKind.UsingStatement;
        public override int ReturnStatement => (int)SyntaxKind.ReturnStatement;
        public override int HashToken => (int)SyntaxKind.HashToken;

        public override int ExpressionStatement => (int)SyntaxKind.ExpressionStatement;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer.Utilities
{
    internal sealed class CSharpSyntaxKinds : ISyntaxKinds
    {
        public static CSharpSyntaxKinds Instance { get; } = new CSharpSyntaxKinds();

        private CSharpSyntaxKinds()
        {
        }

        public int EndOfFileToken => (int)SyntaxKind.EndOfFileToken;

        public int ExpressionStatement => (int)SyntaxKind.ExpressionStatement;
        public int LocalDeclarationStatement => (int)SyntaxKind.LocalDeclarationStatement;

        public int VariableDeclarator => (int)SyntaxKind.VariableDeclarator;
    }
}

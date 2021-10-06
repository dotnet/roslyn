// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal class StackFrameCompilationUnit
    {
        public readonly StackFrameMethodDeclarationNode MethodDeclaration;
        public readonly StackFrameFileInformationNode? FileInformationExpression;
        public readonly StackFrameToken EndOfLineToken;

        public StackFrameCompilationUnit(StackFrameMethodDeclarationNode methodDeclaration, StackFrameFileInformationNode? fileInformationExpression, StackFrameToken endOfLineToken)
        {
            MethodDeclaration = methodDeclaration;
            FileInformationExpression = fileInformationExpression;
            EndOfLineToken = endOfLineToken;
        }
    }
}

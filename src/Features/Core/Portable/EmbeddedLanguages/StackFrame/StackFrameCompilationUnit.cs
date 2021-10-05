// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    internal class StackFrameCompilationUnit
    {
        public readonly EmbeddedSyntaxTrivia<StackFrameKind>? AtTrivia;
        public readonly StackFrameMethodDeclarationNode MethodDeclaration;
        public readonly EmbeddedSyntaxTrivia<StackFrameKind>? InTrivia;
        public readonly StackFrameFileInformationNode? FileInformationExpression;
        public readonly EmbeddedSyntaxTrivia<StackFrameKind>? TrailingTrivia;

        public StackFrameCompilationUnit(EmbeddedSyntaxTrivia<StackFrameKind>? atTrivia, StackFrameMethodDeclarationNode methodDeclaration, EmbeddedSyntaxTrivia<StackFrameKind>? inTrivia, StackFrameFileInformationNode? fileInformationExpression, EmbeddedSyntaxTrivia<StackFrameKind>? trailingTrivia)
        {
            AtTrivia = atTrivia;
            MethodDeclaration = methodDeclaration;
            InTrivia = inTrivia;
            FileInformationExpression = fileInformationExpression;
            TrailingTrivia = trailingTrivia;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    internal interface IStackFrameNodeVisitor
    {
        void Visit(StackFrameCompilationUnit node);
        void Visit(StackFrameMethodDeclarationNode node);
        void Visit(StackFrameMemberAccessExpressionNode node);
        void Visit(StackFrameTypeArgumentList node);
        void Visit(StackFrameParameterList node);
        void Visit(StackFrameGenericTypeIdentifier node);
        void Visit(StackFrameTypeArgumentNode node);
        void Visit(StackFrameArrayRankSpecifier node);
        void Visit(StackFrameFileInformationNode node);
        void Visit(StackFrameArrayTypeExpression node);
        void Visit(StackFrameParameterNode node);
    }
}

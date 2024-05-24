// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

internal interface IStackFrameNodeVisitor
{
    void Visit(StackFrameCompilationUnit node);
    void Visit(StackFrameMethodDeclarationNode node);
    void Visit(StackFrameQualifiedNameNode node);
    void Visit(StackFrameTypeArgumentList node);
    void Visit(StackFrameParameterList node);
    void Visit(StackFrameGenericNameNode node);
    void Visit(StackFrameIdentifierNameNode node);
    void Visit(StackFrameArrayRankSpecifier node);
    void Visit(StackFrameFileInformationNode node);
    void Visit(StackFrameArrayTypeNode node);
    void Visit(StackFrameParameterDeclarationNode node);
    void Visit(StackFrameGeneratedMethodNameNode stackFrameGeneratedNameNode);
    void Visit(StackFrameLocalMethodNameNode stackFrameLocalMethodNameNode);
    void Visit(StackFrameConstructorNode constructorNode);
}

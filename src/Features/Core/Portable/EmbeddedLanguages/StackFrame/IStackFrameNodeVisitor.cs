// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    internal interface IStackFrameNodeVisitor
    {
        void Visit(StackFrameMethodDeclarationNode stackFrameMethodDeclarationNode);
        void Visit(StackFrameMemberAccessExpressionNode stackFrameMemberAccessExpressionNode);
        void Visit(StackFrameTypeArgumentList stackFrameTypeArguments);
        void Visit(StackFrameParameterList stackFrameArgumentList);
        void Visit(StackFrameIdentifierNode stackFrameIdentifierNode);
        void Visit(StackFrameGenericTypeIdentifier stackFrameGenericTypeIdentifier);
        void Visit(StackFrameTypeArgument stackFrameTypeArgument);
        void Visit(StackFrameArrayExpressionNode stackFrameArrayExpressionNode);
    }
}

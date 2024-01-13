// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    /// <summary>
    /// The root unit for a stackframe. Includes the method declaration for the stack frame and optional file information. 
    /// Any leading "at " is considered trivia of <see cref="MethodDeclaration"/>, and " in " is put as trivia for the <see cref="FileInformationExpression"/>.
    /// Remaining unparsable text is put as leading trivia on the <see cref="EndOfLineToken"/>
    /// </summary>
    internal class StackFrameCompilationUnit(StackFrameMethodDeclarationNode methodDeclaration, StackFrameFileInformationNode? fileInformationExpression, StackFrameToken endOfLineToken) : StackFrameNode(StackFrameKind.CompilationUnit)
    {
        /// <summary>
        /// Represents the method declaration for a stack frame. Requires at least a member 
        /// access and argument list with no parameters to be considered valid
        /// </summary>
        public readonly StackFrameMethodDeclarationNode MethodDeclaration = methodDeclaration;

        /// <summary>
        /// File information for a stack frame. May be optionally contained. If available, represents
        /// the file path of a stackframe and optionally the line number. This is available as hint information
        /// and may be useful for a user, but is not always accurate when mapping back to source.
        /// </summary>
        public readonly StackFrameFileInformationNode? FileInformationExpression = fileInformationExpression;

        /// <summary>
        /// The end token of a frame. Any trailing text is added as leading trivia of this token.
        /// </summary>
        public readonly StackFrameToken EndOfLineToken = endOfLineToken;

        internal override int ChildCount => 3;

        public override void Accept(IStackFrameNodeVisitor visitor)
            => visitor.Visit(this);

        internal override StackFrameNodeOrToken ChildAt(int index)
            => index switch
            {
                0 => MethodDeclaration,
                1 => FileInformationExpression,
                2 => EndOfLineToken,
                _ => throw new InvalidOperationException()
            };
    }
}

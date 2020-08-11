// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal partial class AbstractInlineMethodRefactoringProvider
    {
        private class MethodInvocationInfo
        {
            public SyntaxNode? StatementContainsCalleeInvocationExpression { get; }
            public bool IsCalleeSingleInvokedAsStatementOrDeclaration { get; }
            public bool AssignedToVariable { get; }

            private MethodInvocationInfo(
                SyntaxNode? statementContainsCalleeInvocationExpression,
                bool isCalleeSingleInvokedAsStatementOrDeclaration,
                bool assignedToVariable)
            {
                StatementContainsCalleeInvocationExpression = statementContainsCalleeInvocationExpression;
                IsCalleeSingleInvokedAsStatementOrDeclaration = isCalleeSingleInvokedAsStatementOrDeclaration;
                AssignedToVariable = assignedToVariable;
            }

            public static MethodInvocationInfo GetMethodInvocationInfo(
                ISyntaxFacts syntaxFacts,
                AbstractInlineMethodRefactoringProvider inlineMethodRefactoringProvider,
                SyntaxNode calleeInvocationSyntaxNode)
            {
                var statementInvokesCallee = inlineMethodRefactoringProvider.GetStatementInvokesCallee(calleeInvocationSyntaxNode);
                var parent = calleeInvocationSyntaxNode.Parent;
                var isCalleeSingleInvoked = syntaxFacts.IsLocalDeclarationStatement(parent) || syntaxFacts.IsExpressionStatement(parent);
                var assignedToVariable = syntaxFacts.IsLocalDeclarationStatement(parent);
                return new MethodInvocationInfo(statementInvokesCallee, isCalleeSingleInvoked, assignedToVariable);
            }
        }
    }
}

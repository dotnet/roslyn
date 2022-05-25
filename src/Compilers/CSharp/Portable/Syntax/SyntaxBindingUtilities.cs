// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class SyntaxBindingUtilities
    {
        public static bool BindsToResumableStateMachineState(SyntaxNode node)
            => node.IsKind(SyntaxKind.YieldReturnStatement) ||
               node.IsKind(SyntaxKind.AwaitExpression) ||
               node is CommonForEachStatementSyntax { AwaitKeyword.IsMissing: false }
                    or VariableDeclaratorSyntax { Parent.Parent: UsingStatementSyntax { AwaitKeyword.IsMissing: false } or LocalDeclarationStatementSyntax { AwaitKeyword.IsMissing: false } }
                    or UsingStatementSyntax { Expression: not null, AwaitKeyword.IsMissing: false };

        public static bool BindsToTryStatement(SyntaxNode node)
            => node is VariableDeclaratorSyntax { Parent.Parent: UsingStatementSyntax { } or LocalDeclarationStatementSyntax { UsingKeyword.IsMissing: false } }
                    or UsingStatementSyntax { Expression: not null }
                    or CommonForEachStatementSyntax
                    or TryStatementSyntax
                    or LockStatementSyntax;
    }
}

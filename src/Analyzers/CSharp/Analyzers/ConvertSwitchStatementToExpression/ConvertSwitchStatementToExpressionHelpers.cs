// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;

internal static class ConvertSwitchStatementToExpressionHelpers
{
    public static bool IsDefaultSwitchLabel(SwitchLabelSyntax node)
    {
        // default:
        if (node.IsKind(SyntaxKind.DefaultSwitchLabel))
        {
            return true;
        }

        if (node is CasePatternSwitchLabelSyntax @case)
        {
            // case _:
            if (@case.Pattern.IsKind(SyntaxKind.DiscardPattern))
            {
                return @case.WhenClause == null;
            }

            // case var _:
            // case var x:
            if (@case.Pattern is VarPatternSyntax varPattern &&
                varPattern.Designation.Kind() is SyntaxKind.DiscardDesignation or SyntaxKind.SingleVariableDesignation)
            {
                return @case.WhenClause == null;
            }
        }

        return false;
    }
}

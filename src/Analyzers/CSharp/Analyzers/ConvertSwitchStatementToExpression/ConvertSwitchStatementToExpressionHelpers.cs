﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    internal static class ConvertSwitchStatementToExpressionHelpers
    {
        public static bool IsDefaultSwitchLabel(SwitchLabelSyntax node)
        {
            // default:
            if (node.IsKind(SyntaxKind.DefaultSwitchLabel))
            {
                return true;
            }

            if (node.IsKind(SyntaxKind.CasePatternSwitchLabel, out CasePatternSwitchLabelSyntax @case))
            {
                // case _:
                if (@case.Pattern.IsKind(SyntaxKind.DiscardPattern))
                {
                    return @case.WhenClause == null;
                }

                // case var _:
                // case var x:
                if (@case.Pattern.IsKind(SyntaxKind.VarPattern, out VarPatternSyntax varPattern) &&
                    varPattern.Designation.IsKind(SyntaxKind.DiscardDesignation, SyntaxKind.SingleVariableDesignation))
                {
                    return @case.WhenClause == null;
                }
            }

            return false;
        }
    }
}

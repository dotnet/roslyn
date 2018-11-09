' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment
    Friend Module Utilities
        Public ReadOnly Kinds As ImmutableArray(Of (SyntaxKind, SyntaxKind, SyntaxKind)) =
            ImmutableArray.Create(
                (SyntaxKind.AddExpression, SyntaxKind.AddAssignmentStatement, SyntaxKind.PlusEqualsToken),
                (SyntaxKind.SubtractExpression, SyntaxKind.SubtractAssignmentStatement, SyntaxKind.MinusEqualsToken),
                (SyntaxKind.MultiplyExpression, SyntaxKind.MultiplyAssignmentStatement, SyntaxKind.AsteriskEqualsToken),
                (SyntaxKind.DivideExpression, SyntaxKind.DivideAssignmentStatement, SyntaxKind.SlashEqualsToken),
                (SyntaxKind.IntegerDivideExpression, SyntaxKind.IntegerDivideAssignmentStatement, SyntaxKind.BackslashEqualsToken),
                (SyntaxKind.ExponentiateExpression, SyntaxKind.ExponentiateAssignmentStatement, SyntaxKind.CaretEqualsToken),
                (SyntaxKind.ConcatenateExpression, SyntaxKind.ConcatenateAssignmentStatement, SyntaxKind.AmpersandEqualsToken),
                (SyntaxKind.RightShiftExpression, SyntaxKind.RightShiftAssignmentStatement, SyntaxKind.GreaterThanGreaterThanEqualsToken),
                (SyntaxKind.LeftShiftExpression, SyntaxKind.LeftShiftAssignmentStatement, SyntaxKind.LessThanLessThanEqualsToken))
    End Module
End Namespace

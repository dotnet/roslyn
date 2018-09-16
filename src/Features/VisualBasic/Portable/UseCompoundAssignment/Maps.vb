' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment
    Friend Module Maps
        Public ReadOnly BinaryToAssignmentMap As ImmutableDictionary(Of SyntaxKind, SyntaxKind) = New Dictionary(Of SyntaxKind, SyntaxKind) From {
                {SyntaxKind.AddExpression, SyntaxKind.AddAssignmentStatement},
                {SyntaxKind.SubtractExpression, SyntaxKind.SubtractAssignmentStatement},
                {SyntaxKind.MultiplyExpression, SyntaxKind.MultiplyAssignmentStatement},
                {SyntaxKind.DivideExpression, SyntaxKind.DivideAssignmentStatement},
                {SyntaxKind.IntegerDivideExpression, SyntaxKind.IntegerDivideAssignmentStatement},
                {SyntaxKind.ExponentiateExpression, SyntaxKind.ExponentiateAssignmentStatement},
                {SyntaxKind.ConcatenateExpression, SyntaxKind.ConcatenateAssignmentStatement},
                {SyntaxKind.RightShiftExpression, SyntaxKind.RightShiftAssignmentStatement},
                {SyntaxKind.LeftShiftExpression, SyntaxKind.LeftShiftAssignmentStatement}
            }.ToImmutableDictionary()

        Public ReadOnly AssignmentToTokenMap As ImmutableDictionary(Of SyntaxKind, SyntaxKind) = New Dictionary(Of SyntaxKind, SyntaxKind) From {
                {SyntaxKind.AddAssignmentStatement, SyntaxKind.PlusEqualsToken},
                {SyntaxKind.SubtractAssignmentStatement, SyntaxKind.MinusEqualsToken},
                {SyntaxKind.MultiplyAssignmentStatement, SyntaxKind.AsteriskEqualsToken},
                {SyntaxKind.DivideAssignmentStatement, SyntaxKind.SlashEqualsToken},
                {SyntaxKind.IntegerDivideAssignmentStatement, SyntaxKind.BackslashEqualsToken},
                {SyntaxKind.ExponentiateAssignmentStatement, SyntaxKind.CaretEqualsToken},
                {SyntaxKind.ConcatenateAssignmentStatement, SyntaxKind.AmpersandEqualsToken},
                {SyntaxKind.RightShiftAssignmentStatement, SyntaxKind.GreaterThanGreaterThanEqualsToken},
                {SyntaxKind.LeftShiftAssignmentStatement, SyntaxKind.LessThanLessThanEqualsToken}
            }.ToImmutableDictionary()
    End Module
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitLabelStatement(node As BoundLabelStatement) As BoundNode
            Dim statement = DirectCast(MyBase.VisitLabelStatement(node), BoundStatement)

            ' Keep track of line number if need to.
            If _currentLineTemporary IsNot Nothing AndAlso _currentMethodOrLambda Is _topMethod AndAlso
               Not node.WasCompilerGenerated AndAlso node.Syntax.Kind = SyntaxKind.LabelStatement Then
                Dim labelSyntax = DirectCast(node.Syntax, LabelStatementSyntax)

                If labelSyntax.LabelToken.Kind = SyntaxKind.IntegerLiteralToken Then

                    Dim lineNumber As Integer = 0

                    Integer.TryParse(labelSyntax.LabelToken.ValueText, NumberStyles.None, CultureInfo.InvariantCulture, lineNumber)
                    Dim trackLineNumber As BoundStatement = New BoundAssignmentOperator(node.Syntax,
                                                                                        New BoundLocal(node.Syntax, _currentLineTemporary, _currentLineTemporary.Type),
                                                                                        New BoundLiteral(node.Syntax, ConstantValue.Create(lineNumber), _currentLineTemporary.Type),
                                                                                        suppressObjectClone:=True).ToStatement()

                    ' Need to update resume state when we track line numbers for labels.
                    If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                        trackLineNumber = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, trackLineNumber, canThrow:=False)
                    End If

                    statement = New BoundStatementList(node.Syntax, ImmutableArray.Create(statement, trackLineNumber))
                End If
            End If

            ' only labels from the source get their sequence points here
            ' synthetic labels are the responsibility of whoever created them
            If node.Label.IsFromCompilation(_compilationState.Compilation) AndAlso Instrument(node, statement) Then
                statement = _instrumenterOpt.InstrumentLabelStatement(node, statement)
            End If

            Return statement
        End Function
    End Class
End Namespace

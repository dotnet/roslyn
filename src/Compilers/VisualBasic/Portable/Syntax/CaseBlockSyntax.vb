' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class CaseBlockSyntax

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use CaseStatement instead.", True)>
        Public ReadOnly Property Begin As CaseStatementSyntax
            Get
                Return CaseStatement
            End Get
        End Property

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use WithCaseStatement instead.", True)>
        Public Function WithBegin(begin As CaseStatementSyntax) As CaseBlockSyntax
            Return WithCaseStatement(begin)
        End Function

        <EditorBrowsable(EditorBrowsableState.Never)>
        <Obsolete("This member is obsolete. Use AddCaseStatementCases instead.", True)>
        Public Function AddBeginCases(ParamArray items As CaseClauseSyntax()) As CaseBlockSyntax
            Return AddCaseStatementCases(items)
        End Function

    End Class

End Namespace

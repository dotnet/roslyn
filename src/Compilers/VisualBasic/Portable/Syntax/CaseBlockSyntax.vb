' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
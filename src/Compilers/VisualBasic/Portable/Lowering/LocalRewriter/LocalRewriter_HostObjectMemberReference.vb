' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class LocalRewriter
        Public Overrides Function VisitHostObjectMemberReference(node As BoundHostObjectMemberReference) As BoundNode
            Debug.Assert(_previousSubmissionFields IsNot Nothing)
            Debug.Assert(Not _topMethod.IsShared)

            Dim syntax = node.Syntax
            Dim hostObjectReference = _previousSubmissionFields.GetHostObjectField()
            Dim meReference = New BoundMeReference(syntax, _topMethod.ContainingType)
            Return New BoundFieldAccess(syntax, receiverOpt:=meReference, FieldSymbol:=hostObjectReference, isLValue:=False, Type:=hostObjectReference.Type)
        End Function
    End Class
End Namespace

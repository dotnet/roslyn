' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class LocalRewriter
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

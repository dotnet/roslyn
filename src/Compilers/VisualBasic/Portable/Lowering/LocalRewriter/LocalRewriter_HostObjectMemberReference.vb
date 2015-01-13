' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class LocalRewriter
        Public Overrides Function VisitHostObjectMemberReference(node As BoundHostObjectMemberReference) As BoundNode
            Debug.Assert(previousSubmissionFields IsNot Nothing)
            Debug.Assert(Not topMethod.IsShared)

            Dim syntax = node.Syntax
            Dim hostObjectReference = previousSubmissionFields.GetHostObjectField()
            Dim meReference = New BoundMeReference(syntax, topMethod.ContainingType)
            Return New BoundFieldAccess(syntax, receiverOpt:=meReference, FieldSymbol:=hostObjectReference, isLValue:=False, Type:=hostObjectReference.Type)
        End Function
    End Class
End Namespace
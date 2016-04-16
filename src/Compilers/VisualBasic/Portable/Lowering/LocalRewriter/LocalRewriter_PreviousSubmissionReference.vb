' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class LocalRewriter
        Public Overrides Function VisitPreviousSubmissionReference(node As BoundPreviousSubmissionReference) As BoundNode
            Dim targetType = DirectCast(node.Type, ImplicitNamedTypeSymbol)
            Debug.Assert(targetType.TypeKind = TypeKind.Submission)
            Debug.Assert(Not _topMethod.IsShared)
            Debug.Assert(_previousSubmissionFields IsNot Nothing)

            Dim syntax = node.Syntax
            Dim targetScriptReference = _previousSubmissionFields.GetOrMakeField(targetType)
            Dim meReference = New BoundMeReference(syntax, _topMethod.ContainingType)
            Return New BoundFieldAccess(syntax, receiverOpt:=meReference, FieldSymbol:=targetScriptReference, isLValue:=False, Type:=targetScriptReference.Type)
        End Function
    End Class
End Namespace

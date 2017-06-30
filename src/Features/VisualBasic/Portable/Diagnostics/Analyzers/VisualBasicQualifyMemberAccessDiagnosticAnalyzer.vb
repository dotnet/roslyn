﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.QualifyMemberAccess

Namespace Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicQualifyMemberAccessDiagnosticAnalyzer
        Inherits AbstractQualifyMemberAccessDiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function IsAlreadyQualifiedMemberAccess(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.MeExpression)
        End Function

        Protected Overrides Function CanMemberAccessBeQualified(node As SyntaxNode) As Boolean
            ' If the member is already qualified with `MyBase.`, or `MyClass.`, it cannot be further qualified.
            Return Not (node.IsKind(SyntaxKind.MyBaseExpression) OrElse node.IsKind(SyntaxKind.MyClassExpression))
        End Function
    End Class
End Namespace

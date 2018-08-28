' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnusedMembers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnusedMembersDiagnosticAnalyzer
        Inherits AbstractRemoveUnusedMembersDiagnosticAnalyzer(Of DocumentationCommentTriviaSyntax, IdentifierNameSyntax)

        Public Sub New()
            MyBase.New(forceEnableRules:=False)
        End Sub

        ' For testing purposes only.
        Friend Sub New(forceEnableRules As Boolean)
            MyBase.New(forceEnableRules)
        End Sub
    End Class
End Namespace

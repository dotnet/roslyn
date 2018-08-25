' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnusedMembers

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedMembers

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnusedMembersDiagnosticAnalyzer
        Inherits AbstractRemoveUnusedMembersDiagnosticAnalyzer

        Public Sub New()
            ' Compound assigment is a statement in VB that does not return a value.
            ' So, we treat it as a write-only usage.
            MyBase.New(treatCompoundAssignmentAsWriteOnlyOperation:=True)
        End Sub
    End Class
End Namespace

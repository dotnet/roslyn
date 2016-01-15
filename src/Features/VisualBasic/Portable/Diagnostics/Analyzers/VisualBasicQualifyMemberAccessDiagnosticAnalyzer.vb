' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.QualifyMemberAccess

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicQualifyMemberAccessDiagnosticAnalyzer
        Inherits QualifyMemberAccessDiagnosticAnalyzerBase(Of SyntaxKind)

        Protected Overrides Function IsAlreadyQualifiedMemberAccess(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.MeExpression)
        End Function
    End Class

End Namespace

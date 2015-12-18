' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.QualifyMemberAccess

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.QualifyMemberAccess

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicQualifyMemberAccessDiagnosticAnalyzer
        Inherits QualifyMemberAccessDiagnosticAnalyzerBase(Of SyntaxKind)

        Private Shared ReadOnly s_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(SyntaxKind.IdentifierName)

        Protected Overrides Function GetSupportedSyntaxKinds() As ImmutableArray(Of SyntaxKind)
            Return s_kindsOfInterest
        End Function

        Protected Overrides Function IsAlreadyQualifiedMemberAccess(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.MeExpression)
        End Function
    End Class

End Namespace

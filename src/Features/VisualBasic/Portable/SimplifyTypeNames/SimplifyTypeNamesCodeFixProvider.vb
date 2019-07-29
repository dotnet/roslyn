' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyTypeNames

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyNames), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class SimplifyTypeNamesCodeFixProvider
        Inherits AbstractSimplifyTypeNamesCodeFixProvider(Of SyntaxKind)

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer())
        End Sub

        Protected Overrides Function GetTitle(simplifyDiagnosticId As String, nodeText As String) As String
            Select Case simplifyDiagnosticId
                Case IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                     IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId
                    Return String.Format(VBFeaturesResources.Simplify_name_0, nodeText)

                Case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId
                    Return String.Format(VBFeaturesResources.Simplify_member_access_0, nodeText)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(simplifyDiagnosticId)
            End Select
        End Function

        Protected Overrides Function AddSimplificationAnnotationTo(expression As SyntaxNode) As SyntaxNode
            Return expression.WithAdditionalAnnotations(Simplifier.Annotation)
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyTypeNames

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyNames), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class SimplifyTypeNamesCodeFixProvider
        Inherits AbstractSimplifyTypeNamesCodeFixProvider(Of SyntaxKind, VisualBasicSimplifierOptions)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
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

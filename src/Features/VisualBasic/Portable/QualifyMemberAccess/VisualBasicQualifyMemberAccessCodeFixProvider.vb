'Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.QualifyMemberAccess
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.QualifyMemberAccess), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Friend Class VisualBasicQualifyMemberAccessCodeFixProvider
        Inherits AbstractQualifyMemberAccessCodeFixprovider(Of SimpleNameSyntax, InvocationExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Add_Me
        End Function

        Protected Overrides Function GetNode(diagnostic As Diagnostic, cancellationToken As CancellationToken) As SimpleNameSyntax
            Dim node = diagnostic.Location.FindNode(True, cancellationToken)
            If TypeOf node Is SimpleNameSyntax Then
                Return CType(node, SimpleNameSyntax)
            End If

            If TypeOf node Is InvocationExpressionSyntax Then
                Dim invocationExpressionSyntax = CType(node, InvocationExpressionSyntax)
                Return CType(invocationExpressionSyntax.Expression, SimpleNameSyntax)
            End If

            Return Nothing
        End Function
    End Class
End Namespace

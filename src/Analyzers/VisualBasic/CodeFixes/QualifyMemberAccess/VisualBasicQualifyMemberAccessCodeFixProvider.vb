﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.QualifyMemberAccess
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.QualifyMemberAccess
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.QualifyMemberAccess), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Friend Class VisualBasicQualifyMemberAccessCodeFixProvider
        Inherits AbstractQualifyMemberAccessCodeFixprovider(Of SimpleNameSyntax, InvocationExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VisualBasicCodeFixesResources.Add_Me
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

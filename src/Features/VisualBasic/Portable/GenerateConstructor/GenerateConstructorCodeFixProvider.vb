﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor
    Friend Class GenerateConstructorDiagnosticIds
        Friend Const BC30057 As String = NameOf(BC30057) ' error BC30057: Too many arguments to 'Public Sub New()'.
        Friend Const BC30272 As String = NameOf(BC30272) ' error BC30272: 'p' is not a parameter of 'Public Sub New()'.
        Friend Const BC30274 As String = NameOf(BC30274) ' error BC30274: Parameter 'prop' of 'Public Sub New(prop As String)' already has a matching argument.
        Friend Const BC30311 As String = NameOf(BC30311) ' error BC30311: Value of type 'String' cannot be converted to 'Exception'.
        Friend Const BC30389 As String = NameOf(BC30389) ' error BC30389: 'x' is not accessible in this context
        Friend Const BC30455 As String = NameOf(BC30455) ' error BC30455: Argument not specified for parameter 'x' of 'Public Sub New(x As Integer)'.
        Friend Const BC30512 As String = NameOf(BC30512) ' error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Friend Const BC32006 As String = NameOf(BC32006) ' error BC32006: 'Char' values cannot be converted to 'Integer'. 
        Friend Const BC30387 As String = NameOf(BC30387) ' error BC32006: Class 'Derived' must declare a 'Sub New' because its base class 'Base' does not have an accessible 'Sub New' that can be called with no arguments. 
        Friend Const BC30516 As String = NameOf(BC30516) ' error BC30516: Overload resolution failed because no accessible 'Blah' accepts this number of arguments.
        Friend Const BC36625 As String = NameOf(BC36625) ' error BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.

        Friend Shared ReadOnly AllDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30057, IDEDiagnosticIds.UnboundConstructorId, BC30272, BC30274, BC30389, BC30455, BC32006, BC30512, BC30387, BC30516)
        Friend Shared ReadOnly TooManyArgumentsDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30057)
        Friend Shared ReadOnly CannotConvertDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC30512, BC32006, BC30311, BC36625)
    End Class

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateConstructor), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.FullyQualify)>
    Friend Class GenerateConstructorCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return GenerateConstructorDiagnosticIds.AllDiagnosticIds
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateConstructorService)()
            Return service.GenerateConstructorAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            Dim invocation = TryCast(node, InvocationExpressionSyntax)
            If invocation IsNot Nothing AndAlso invocation.Expression IsNot Nothing Then
                Return GetRightmostName(invocation.Expression)
            End If

            Dim objectCreation = TryCast(node, ObjectCreationExpressionSyntax)
            If objectCreation IsNot Nothing AndAlso objectCreation.Type IsNot Nothing Then
                Return objectCreation.Type.GetRightmostName()
            End If

            Dim attribute = TryCast(node, AttributeSyntax)
            If attribute IsNot Nothing Then
                Return attribute.Name
            End If

            Return node
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
            Return TypeOf node Is InvocationExpressionSyntax OrElse
                   TypeOf node Is ObjectCreationExpressionSyntax OrElse
                   TypeOf node Is AttributeSyntax
        End Function
    End Class
End Namespace

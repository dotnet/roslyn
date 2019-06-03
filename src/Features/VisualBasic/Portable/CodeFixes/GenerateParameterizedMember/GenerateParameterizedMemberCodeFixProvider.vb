' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.GenerateMember
Imports Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateMethod
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateMethod), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.PopulateSwitch, After:=PredefinedCodeFixProviderNames.GenerateEvent)>
    Friend Class GenerateParameterizedMemberCodeFixProvider
        Inherits AbstractGenerateMemberCodeFixProvider

        Friend Const BC30057 As String = "BC30057" ' error BC30057: Too many arguments to 'Public Sub Baz()'
        Friend Const BC30518 As String = "BC30518" ' error BC30518: Overload resolution failed because no accessible 'sub1' can be called with these arguments.
        Friend Const BC30519 As String = "BC30519" ' error BC30519: Overload resolution failed because no accessible 'sub1' can be called without a narrowing conversion.
        Friend Const BC30520 As String = "BC30520" ' error BC30520: Argument matching parameter 'blah' narrows from 'A' to 'B'.
        Friend Const BC30521 As String = "BC30521" ' error BC30521: Overload resolution failed because no accessible 'Baz' is most specific for these arguments.
        Friend Const BC30112 As String = "BC30112" ' error BC30112: 'blah' is a namespace and cannot be used as a type/
        Friend Const BC30451 As String = "BC30451" ' error BC30451: 'Goo' is not declared. It may be inaccessible due to its protection level.
        Friend Const BC30455 As String = "BC30455" ' error BC30455: Argument not specified for parameter 'two' of 'Public Sub Baz(one As System.Func(Of String), two As Integer)'.
        Friend Const BC30456 As String = "BC30456" ' error BC30456: 'Goo' is not a member of 'Sibling'.
        Friend Const BC30401 As String = "BC30401" ' error BC30401: 'Blah' cannot implement 'Snarf' because there is no matching sub on interface 'IGoo'.
        Friend Const BC30516 As String = "BC30516" ' error BC30516: Overload resolution failed because no accessible 'Blah' accepts this number of arguments.
        Friend Const BC32016 As String = "BC32016" ' error BC32016: 'blah' has no parameters and its return type cannot be indexed.
        Friend Const BC32045 As String = "BC32045" ' error BC32045: 'Private Sub Blah()' has no type parameters and so cannot have type arguments.
        Friend Const BC32087 As String = "BC32087" ' error BC32087: Overload resolution failed because no accessible 'Blah' accepts this number of type arguments.
        Friend Const BC36625 As String = "BC36625" ' error BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.
        Friend Const BC30107 As String = "BC30107" ' error BC30107: 'Foo' is an Enum type and cannot be used as an expression.
        Friend Const BC30108 As String = "BC30108" ' error BC30108: 'Foo' is a type and cannot be used as an expression.
        Friend Const BC30109 As String = "BC30109" ' error BC30109: 'Foo' is a class type and cannot be used as an expression.
        Friend Const BC30110 As String = "BC30110" ' error BC30110: 'Foo' is a structure type and cannot be used as an expression.
        Friend Const BC30111 As String = "BC30111" ' error BC30111: 'Foo' is an interface type and cannot be used as an expression.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30518, BC30519, BC30520, BC30521, BC30057, BC30112, BC30451, BC30455, BC30456, BC30401, BC30516, BC32016, BC32045, BC32087, BC36625, BC30107, BC30108, BC30109, BC30110, BC30111)
            End Get
        End Property

        Protected Overrides Function GetCodeActionsAsync(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of CodeAction))
            Dim service = document.GetLanguageService(Of IGenerateParameterizedMemberService)()
            Return service.GenerateMethodAsync(document, node, cancellationToken)
        End Function

        Protected Overrides Function IsCandidate(node As SyntaxNode, token As SyntaxToken, diagnostic As Diagnostic) As Boolean
            ' If we have a diagnostic on "a.b.c" in something like a.b.c(...), then don't try to 
            ' perform fixes on 'a' or 'a.b'.
            Dim diagnosticSpan = diagnostic.Location.SourceSpan
            If node.Span.Start = diagnosticSpan.Start AndAlso node.Span.End < diagnosticSpan.End Then
                Return False
            End If

            Return TypeOf node Is QualifiedNameSyntax OrElse
                TypeOf node Is SimpleNameSyntax OrElse
                TypeOf node Is MemberAccessExpressionSyntax OrElse
                TypeOf node Is InvocationExpressionSyntax OrElse
                TypeOf node Is ExpressionSyntax OrElse
                TypeOf node Is IdentifierNameSyntax
        End Function

        Protected Overrides Function GetTargetNode(node As SyntaxNode) As SyntaxNode
            Dim memberAccess = TryCast(node, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return memberAccess.Name
            End If

            Dim invocationExpression = TryCast(node, InvocationExpressionSyntax)
            If invocationExpression IsNot Nothing Then
                Return invocationExpression.Expression
            End If

            Return node
        End Function
    End Class
End Namespace

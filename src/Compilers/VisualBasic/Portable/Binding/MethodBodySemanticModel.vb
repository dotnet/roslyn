' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class MethodBodySemanticModel
        Inherits MemberSemanticModel

        Private Sub New(root As SyntaxNode,
                        binder As Binder,
                        Optional containingSemanticModelOpt As SyntaxTreeSemanticModel = Nothing,
                        Optional parentSemanticModelOpt As SyntaxTreeSemanticModel = Nothing,
                        Optional speculatedPosition As Integer = 0,
                        Optional ignoreAccessibility As Boolean = False)
            MyBase.New(root, binder, containingSemanticModelOpt, parentSemanticModelOpt, speculatedPosition, ignoreAccessibility)
        End Sub

        ''' <summary>
        ''' Creates an MethodBodySemanticModel that allows asking semantic questions about an attribute node.
        ''' </summary>
        Friend Shared Function Create(containingSemanticModel As SyntaxTreeSemanticModel, binder As SubOrFunctionBodyBinder, Optional ignoreAccessibility As Boolean = False) As MethodBodySemanticModel
            Debug.Assert(containingSemanticModel IsNot Nothing)
            Return New MethodBodySemanticModel(binder.Root, binder, containingSemanticModel, ignoreAccessibility:=ignoreAccessibility)
        End Function

        ''' <summary>
        ''' Creates a speculative MethodBodySemanticModel that allows asking semantic questions about an attribute node that did not appear in the original source code.
        ''' </summary>
        Friend Shared Function CreateSpeculative(parentSemanticModel As SyntaxTreeSemanticModel, root As VisualBasicSyntaxNode, binder As Binder, position As Integer) As MethodBodySemanticModel
            Debug.Assert(parentSemanticModel IsNot Nothing)
            Debug.Assert(root IsNot Nothing)
            Debug.Assert(binder IsNot Nothing)
            Debug.Assert(binder.IsSemanticModelBinder)

            Return New MethodBodySemanticModel(root, binder, parentSemanticModelOpt:=parentSemanticModel, speculatedPosition:=position)
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            ' CONSIDER: Do we want to ensure that speculated method and the original method have identical signatures?

            ' Create a speculative binder for the method body.
            Dim methodSymbol = DirectCast(Me.MemberSymbol, methodSymbol)

            Dim containingBinder As binder = Me.RootBinder

            ' Get up to the NamedTypeBinder
            Dim namedTypeBinder As namedTypeBinder

            Do
                namedTypeBinder = TryCast(containingBinder, namedTypeBinder)

                If namedTypeBinder IsNot Nothing Then
                    Exit Do
                End If

                containingBinder = containingBinder.ContainingBinder
            Loop

            Dim methodBodyBinder = BinderBuilder.CreateBinderForMethodBody(methodSymbol, method, SemanticModelBinder.Mark(namedTypeBinder, IgnoresAccessibility))

            ' Wrap this binder with a BlockBaseBinder to hold onto the locals declared within the statement.
            Dim binder = New StatementListBinder(methodBodyBinder, method.Statements)
            Debug.Assert(binder.IsSemanticModelBinder)

            speculativeModel = CreateSpeculative(parentModel, method, binder, position)
            Return True
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            Dim binder = Me.GetEnclosingBinder(position)
            If binder Is Nothing Then
                speculativeModel = Nothing
                Return False
            End If

            ' wrap the binder with a Speculative ExecutableCodeBinder
            binder = New SpeculativeStatementBinder(statement, binder)

            ' wrap the binder with a BlockBaseBinder to hold onto the locals declared within the statement
            Dim statementList = New SyntaxList(Of StatementSyntax)(statement)
            binder = New StatementListBinder(binder, statementList)

            speculativeModel = CreateSpeculative(parentModel, statement, binder, position)
            Return True
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As SemanticModel) As Boolean
            speculativeModel = Nothing
            Return False
        End Function
    End Class
End Namespace



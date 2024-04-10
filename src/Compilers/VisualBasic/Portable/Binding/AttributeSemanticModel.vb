' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binding info for attribute syntax and expressions that are part of a attribute.
    ''' </summary>
    Friend NotInheritable Class AttributeSemanticModel
        Inherits MemberSemanticModel

        Friend Sub New(root As VisualBasicSyntaxNode,
                       binder As Binder,
                       containingPublicSemanticModel As PublicSemanticModel)
            MyBase.New(root, binder, containingPublicSemanticModel)
        End Sub

        ''' <summary>
        ''' Creates an AttributeSemanticModel that allows asking semantic questions about an attribute node.
        ''' </summary>
        Friend Shared Function Create(containingSemanticModel As SyntaxTreeSemanticModel, binder As AttributeBinder) As AttributeSemanticModel
            Debug.Assert(containingSemanticModel IsNot Nothing)
            Dim owner As Symbol = GetAttributeTarget(containingSemanticModel, binder)
            Dim wrappedBinder As Binder = binder
            If owner IsNot Nothing Then
                wrappedBinder = New LocationSpecificBinder(BindingLocation.Attribute, owner, binder)
            End If

            Return New AttributeSemanticModel(binder.Root, wrappedBinder, containingSemanticModel)
        End Function

        Private Shared Function GetAttributeTarget(model As SyntaxTreeSemanticModel, binder As AttributeBinder) As Symbol
            Debug.Assert(TypeOf binder.Root Is AttributeSyntax)
            If TypeOf binder.Root.Parent Is AttributeListSyntax Then
                Return DirectCast(model.GetDeclaredSymbolForNode(binder.Root.Parent.Parent), Symbol)
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Creates a speculative semantic model that allows asking semantic questions about an attribute node that did not appear in the original source code.
        ''' </summary>
        Friend Shared Function CreateSpeculative(parentSemanticModel As SyntaxTreeSemanticModel, root As AttributeSyntax, binder As Binder, position As Integer) As SpeculativeSemanticModelWithMemberModel
            Return New SpeculativeSemanticModelWithMemberModel(parentSemanticModel, position, root, binder)
        End Function

        Friend Overrides Function Bind(binder As Binder, node As SyntaxNode, diagnostics As BindingDiagnosticBag) As BoundNode
            Debug.Assert(binder.IsSemanticModelBinder)

            Dim boundNode As BoundNode

            Select Case node.Kind
                Case SyntaxKind.Attribute
                    boundNode = binder.BindAttribute(DirectCast(node, AttributeSyntax), diagnostics)
                    Return boundNode

                Case SyntaxKind.IdentifierName, SyntaxKind.QualifiedName
                    ' Special binding for attribute type to account for the implicit Attribute suffix.
                    If SyntaxFacts.IsAttributeName(node) Then
                        Dim name = DirectCast(node, NameSyntax)
                        boundNode = binder.BindNamespaceOrTypeExpression(name, diagnostics)
                        Return boundNode
                    End If
            End Select

            boundNode = MyBase.Bind(binder, node, diagnostics)
            Return boundNode
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, initializer As EqualsValueSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelCore(parentModel As SyntaxTreeSemanticModel, position As Integer, statement As ExecutableStatementSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            speculativeModel = Nothing
            Return False
        End Function

        Friend Overrides Function TryGetSpeculativeSemanticModelForMethodBodyCore(parentModel As SyntaxTreeSemanticModel, position As Integer, method As MethodBlockBaseSyntax, <Out> ByRef speculativeModel As PublicSemanticModel) As Boolean
            speculativeModel = Nothing
            Return False
        End Function
    End Class
End Namespace


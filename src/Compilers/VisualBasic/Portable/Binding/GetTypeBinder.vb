' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' This binder is for binding the argument to GetType.  It traverses
    ''' the syntax marking each open type ("unbound generic type" in the
    ''' VB spec) as either allowed or not allowed, so that BindType can 
    ''' appropriately return either the corresponding type symbol or an 
    ''' error type.  
    ''' </summary>
    Friend Class GetTypeBinder
        Inherits Binder

        Private ReadOnly _allowedMap As Dictionary(Of GenericNameSyntax, Boolean)

        Friend Sub New(typeExpression As ExpressionSyntax, containingBinder As Binder)
            MyBase.New(containingBinder)

            OpenTypeVisitor.Visit(typeExpression, _allowedMap)
        End Sub

        Public Overrides Function IsUnboundTypeAllowed(Syntax As GenericNameSyntax) As Boolean
            Dim allowed As Boolean
            Return _allowedMap IsNot Nothing AndAlso _allowedMap.TryGetValue(Syntax, allowed) AndAlso allowed
        End Function

        ''' <summary>
        ''' This visitor walks over a type expression looking for open types.
        ''' Open types are allowed if an only if:
        '''   1) There is no constructed generic type elsewhere in the visited syntax; and
        '''   2) The open type is not used as a type argument or array/nullable
        '''        element type.
        ''' </summary>
        Private Class OpenTypeVisitor
            Inherits VisualBasicSyntaxVisitor

            Private _allowedMap As Dictionary(Of GenericNameSyntax, Boolean) = Nothing
            Private _seenConstructed As Boolean = False

            ''' <param name="typeSyntax">The argument to typeof.</param>
            ''' <param name="allowedMap">
            ''' Keys are GenericNameSyntax nodes representing unbound generic types.
            ''' Values are false if the node should result in an error and true otherwise.
            ''' </param>
            Public Overloads Shared Sub Visit(typeSyntax As ExpressionSyntax, <Out()> ByRef allowedMap As Dictionary(Of GenericNameSyntax, Boolean))
                Dim visitor = New OpenTypeVisitor()
                visitor.Visit(typeSyntax)
                allowedMap = visitor._allowedMap
            End Sub

            Public Overrides Sub VisitGenericName(node As GenericNameSyntax)
                Dim typeArguments As SeparatedSyntaxList(Of TypeSyntax) = node.TypeArgumentList.Arguments
                ' Missing type arguments are represented as missing name syntax
                Dim isOpenType = typeArguments.AllAreMissingIdentifierName

                If isOpenType Then
                    If _allowedMap Is Nothing Then
                        _allowedMap = New Dictionary(Of GenericNameSyntax, Boolean)()
                    End If
                    _allowedMap(node) = Not _seenConstructed
                Else
                    _seenConstructed = True
                    For Each arg As TypeSyntax In typeArguments
                        Visit(arg)
                    Next
                End If
            End Sub

            Public Overrides Sub VisitQualifiedName(node As QualifiedNameSyntax)
                Dim seenConstructedBeforeRight As Boolean = _seenConstructed

                ' Visit Right first because it's smaller (to make backtracking cheaper).
                Visit(node.Right)

                Dim seenConstructedBeforeLeft As Boolean = _seenConstructed

                Visit(node.Left)

                ' If the first time we saw a constructed type was in Left, then we need to re-visit Right
                If Not seenConstructedBeforeRight AndAlso Not seenConstructedBeforeLeft AndAlso _seenConstructed Then
                    Visit(node.Right)
                End If

            End Sub

            Public Overrides Sub VisitArrayType(node As ArrayTypeSyntax)
                _seenConstructed = True
                Visit(node.ElementType)
            End Sub

            Public Overrides Sub VisitNullableType(node As NullableTypeSyntax)
                _seenConstructed = True
                Visit(node.ElementType)
            End Sub

        End Class

    End Class
End Namespace

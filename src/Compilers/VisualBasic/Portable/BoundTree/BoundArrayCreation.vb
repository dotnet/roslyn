' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundArrayCreation
        Inherits BoundExpression

        Public Sub New(syntax As VisualBasicSyntaxNode, bounds As ImmutableArray(Of BoundExpression), initializerOpt As BoundArrayInitialization, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, False, bounds, initializerOpt, Nothing, Nothing, type, hasErrors)
        End Sub

        Public Sub New(syntax As VisualBasicSyntaxNode, bounds As ImmutableArray(Of BoundExpression), initializerOpt As BoundArrayInitialization, arrayLiteralOpt As BoundArrayLiteral, arrayLiteralConversion As ConversionKind, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, False, bounds, initializerOpt, arrayLiteralOpt, arrayLiteralConversion, type, hasErrors)
        End Sub

#If DEBUG Then
        Private Sub Validate()
            Dim elementType As TypeSymbol = ErrorTypeSymbol.UnknownResultType

            If Type.Kind = SymbolKind.ArrayType Then
                elementType = DirectCast(Type, ArrayTypeSymbol).ElementType
            End If

            If InitializerOpt IsNot Nothing Then
                ValidateInitializer(InitializerOpt, elementType)
            End If
        End Sub

        Private Sub ValidateInitializer(initializer As BoundArrayInitialization, elementType As TypeSymbol)
            For Each item In initializer.Initializers
                If item.Kind = BoundKind.ArrayInitialization Then
                    ValidateInitializer(DirectCast(item, BoundArrayInitialization), elementType)
                Else
                    item.AssertRValue()

                    If Not elementType.IsErrorType() AndAlso Not item.Type.IsErrorType() Then
                        Debug.Assert(elementType.IsSameTypeIgnoringCustomModifiers(item.Type))
                    End If
                End If
            Next
        End Sub
#End If
    End Class

End Namespace

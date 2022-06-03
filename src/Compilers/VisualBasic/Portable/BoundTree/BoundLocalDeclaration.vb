' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundLocalDeclaration
        Implements IBoundLocalDeclarations

        Public Sub New(syntax As SyntaxNode, localSymbol As LocalSymbol, initializerOpt As BoundExpression)
            MyClass.New(syntax, localSymbol, initializerOpt, Nothing, False, False)
        End Sub

        Public ReadOnly Property InitializerOpt As BoundExpression
            Get
                Return If(DeclarationInitializerOpt, IdentifierInitializerOpt)
            End Get
        End Property

        Private ReadOnly Property IBoundLocalDeclarations_Declarations As ImmutableArray(Of BoundLocalDeclarationBase) Implements IBoundLocalDeclarations.Declarations
            Get
                Return ImmutableArray.Create(Of BoundLocalDeclarationBase)(Me)
            End Get
        End Property

#If DEBUG Then
        Private Sub Validate()
            If InitializerOpt IsNot Nothing Then

                InitializerOpt.AssertRValue()

                Debug.Assert(DeclarationInitializerOpt IsNot IdentifierInitializerOpt)

                If Not HasErrors Then
                    If InitializerOpt.Type Is Nothing Then
                        Debug.Assert(LocalSymbol.IsConst AndAlso InitializerOpt.IsStrictNothingLiteral())
                    Else
                        Debug.Assert(LocalSymbol.Type.IsSameTypeIgnoringAll(InitializerOpt.Type) OrElse InitializerOpt.Type.IsErrorType() OrElse
                                     (LocalSymbol.IsConst AndAlso LocalSymbol.Type.SpecialType = SpecialType.System_Object AndAlso
                                      InitializerOpt.IsConstant AndAlso InitializerOpt.ConstantValueOpt.IsNothing))
                    End If
                End If
            End If
        End Sub
#End If

    End Class

End Namespace

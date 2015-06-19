' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundObjectCreationExpression

        Public Sub New(syntax As VisualBasicSyntaxNode, constructorOpt As MethodSymbol, arguments As ImmutableArray(Of BoundExpression), initializerOpt As BoundObjectInitializerExpressionBase, type As TypeSymbol, Optional hasErrors As Boolean = False)
            Me.New(syntax, constructorOpt, Nothing, arguments, initializerOpt, type, hasErrors)
        End Sub

        Public Function Update(constructorOpt As MethodSymbol, arguments As ImmutableArray(Of BoundExpression), initializerOpt As BoundObjectInitializerExpressionBase, type As TypeSymbol) As BoundObjectCreationExpression
            Return Update(constructorOpt, Nothing, arguments, initializerOpt, type)
        End Function

        Public Overrides ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Me.ConstructorOpt
            End Get
        End Property

    End Class

End Namespace

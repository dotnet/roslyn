' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ReturnValueLocalSymbol
        Inherits PlaceholderLocalSymbol

        Private ReadOnly _index As Integer

        Friend Sub New(method As MethodSymbol, name As String, displayName As String, type As TypeSymbol, index As Integer)
            MyBase.New(method, name, displayName, type)
            _index = index
        End Sub

        Friend Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As SyntaxNode,
            isLValue As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

            Dim method = GetIntrinsicMethod(compilation, ExpressionCompilerConstants.GetReturnValueMethodName)
            Dim argument As New BoundLiteral(
                syntax,
                Microsoft.CodeAnalysis.ConstantValue.Create(_index),
                method.Parameters(0).Type)
            Dim [call] As New BoundCall(
                syntax,
                method,
                methodGroupOpt:=Nothing,
                receiverOpt:=Nothing,
                arguments:=ImmutableArray.Create(Of BoundExpression)(argument),
                constantValueOpt:=Nothing,
                suppressObjectClone:=False,
                type:=method.ReturnType)
            Return ConvertToLocalType(compilation, [call], Type, diagnostics)
        End Function

    End Class

End Namespace

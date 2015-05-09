' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ExceptionLocalSymbol
        Inherits PlaceholderLocalSymbol

        Private ReadOnly _getExceptionMethodName As String

        Friend Sub New(method As MethodSymbol, name As String, displayName As String, type As TypeSymbol, getExceptionMethodName As String)
            MyBase.New(method, name, displayName, type)
            _getExceptionMethodName = getExceptionMethodName
        End Sub

        Friend Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            isLValue As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

            Dim method = GetIntrinsicMethod(compilation, _getExceptionMethodName)
            Dim [call] As New BoundCall(
                syntax,
                method,
                methodGroupOpt:=Nothing,
                receiverOpt:=Nothing,
                arguments:=ImmutableArray(Of BoundExpression).Empty,
                constantValueOpt:=Nothing,
                suppressObjectClone:=False, ' Doesn't matter, since no arguments.
                type:=method.ReturnType)
            Return ConvertToLocalType(compilation, [call], Type, diagnostics)
        End Function

    End Class

End Namespace
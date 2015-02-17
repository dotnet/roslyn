' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ExceptionLocalSymbol
        Inherits PlaceholderLocalSymbol

        Friend Sub New(method As MethodSymbol, name As String, type As TypeSymbol)
            MyBase.New(method, name, type)
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
            isLValue As Boolean) As BoundExpression

            ' The intrinsic accessor method has the same name as the pseudo-variable (normalized to lowercase).
            Dim method = container.GetOrAddSynthesizedMethod(
                Name.ToLowerInvariant(),
                Function(c, n, s)
                    Dim returnType = compilation.GetWellKnownType(WellKnownType.System_Exception)
                    Return New PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        returnType,
                        Function(m) ImmutableArray(Of ParameterSymbol).Empty)
                End Function)
            Dim [call] As New BoundCall(
                syntax,
                method,
                methodGroupOpt:=Nothing,
                receiverOpt:=Nothing,
                arguments:=ImmutableArray(Of BoundExpression).Empty,
                constantValueOpt:=Nothing,
                suppressObjectClone:=False, ' Doesn't matter, since no arguments.
                type:=method.ReturnType)
            Return ConvertToLocalType(compilation, [call], Type)
        End Function

    End Class

End Namespace
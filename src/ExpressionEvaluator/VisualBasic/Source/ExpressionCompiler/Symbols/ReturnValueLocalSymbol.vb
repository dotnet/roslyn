' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ReturnValueLocalSymbol
        Inherits PlaceholderLocalSymbol

        Private ReadOnly _index As Integer

        Friend Sub New(method As MethodSymbol, name As String, type As TypeSymbol, index As Integer)
            MyBase.New(method, name, type)
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
            syntax As VisualBasicSyntaxNode,
            isLValue As Boolean) As BoundExpression

            Dim method = container.GetOrAddSynthesizedMethod(
                ExpressionCompilerConstants.GetReturnValueMethodName,
                Function(c, n, s)
                    Dim parameterType = compilation.GetSpecialType(SpecialType.System_Int32)
                    Dim returnType = compilation.GetSpecialType(SpecialType.System_Object)
                    Return New PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        returnType,
                        Function(m) ImmutableArray.Create(Of ParameterSymbol)(New SynthesizedParameterSymbol(m, parameterType, ordinal:=0, isByRef:=False)))
                End Function)
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
            Return ConvertToLocalType(compilation, [call], Type)
        End Function

    End Class

End Namespace
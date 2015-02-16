' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ObjectIdLocalSymbol
        Inherits PlaceholderLocalSymbol

        Private ReadOnly _id As String
        Private ReadOnly _isReadOnly As Boolean

        Friend Sub New(method As MethodSymbol, type As TypeSymbol, id As String, isReadOnly As Boolean)
            MyBase.New(method, id, type)
            _id = id
            _isReadOnly = isReadOnly
        End Sub

        Friend Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _isReadOnly
            End Get
        End Property

        Friend Overrides Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            isLValue As Boolean) As BoundExpression

            Return RewriteLocalInternal(compilation, container, syntax, Me, isLValue:=isLValue)
        End Function

        Friend Overloads Shared Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            local As LocalSymbol,
            isLValue As Boolean) As BoundExpression

            Return RewriteLocalInternal(compilation, container, syntax, local, isLValue:=isLValue)
        End Function

        Private Overloads Shared Function RewriteLocalInternal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            local As LocalSymbol,
            isLValue As Boolean) As BoundExpression

            Dim parameterType = compilation.GetSpecialType(SpecialType.System_String)
            Dim getValueMethod = container.GetOrAddSynthesizedMethod(
                ExpressionCompilerConstants.GetVariableValueMethodName,
                Function(c, n, s)
                    Dim returnType = compilation.GetSpecialType(SpecialType.System_Object)
                    Return New PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        returnType,
                        Function(m) ImmutableArray.Create(Of ParameterSymbol)(New SynthesizedParameterSymbol(m, parameterType, ordinal:=0, isByRef:=False)))
                End Function)
            Dim getAddressMethod = container.GetOrAddSynthesizedMethod(
                ExpressionCompilerConstants.GetVariableAddressMethodName,
                Function(c, n, s)
                    Dim returnType = compilation.GetSpecialType(SpecialType.System_Object)
                    Return New PlaceholderMethodSymbol(
                        c,
                        s,
                        n,
                        Function(m) ImmutableArray.Create(Of TypeParameterSymbol)(New SimpleTypeParameterSymbol(m, 0, "<>T")),
                        Function(m) m.TypeParameters(0), ' return type is <>T&
                        Function(m) ImmutableArray.Create(Of ParameterSymbol)(New SynthesizedParameterSymbol(m, parameterType, ordinal:=0, isByRef:=False)),
                        returnValueIsByRef:=True)
                End Function)
            Dim variable = New BoundPseudoVariable(
                syntax,
                local,
                isLValue:=True,
                emitExpressions:=New ObjectIdExpressions(compilation, getValueMethod, getAddressMethod),
                type:=local.Type).MakeCompilerGenerated()
            If isLValue Then
                Return variable
            End If
            Return variable.MakeRValue()
        End Function

        Private NotInheritable Class ObjectIdExpressions
            Inherits PseudoVariableExpressions

            Private ReadOnly _compilation As VisualBasicCompilation
            Private ReadOnly _getValueMethod As MethodSymbol
            Private ReadOnly _getAddressMethod As MethodSymbol

            Friend Sub New(
                compilation As VisualBasicCompilation,
                getValueMethod As MethodSymbol,
                getAddressMethod As MethodSymbol)

                _compilation = compilation
                _getValueMethod = getValueMethod
                _getAddressMethod = getAddressMethod
            End Sub

            Friend Overrides Function GetValue(variable As BoundPseudoVariable) As BoundExpression
                Dim local = variable.LocalSymbol
                Dim expr = InvokeGetMethod(_getValueMethod, variable.Syntax, local.Name)
                Return ConvertToLocalType(_compilation, expr, local.Type)
            End Function

            Friend Overrides Function GetAddress(variable As BoundPseudoVariable) As BoundExpression
                Dim local = variable.LocalSymbol
                Return InvokeGetMethod(_getAddressMethod.Construct(local.Type), variable.Syntax, local.Name)
            End Function

            Private Shared Function InvokeGetMethod(method As MethodSymbol, syntax As VisualBasicSyntaxNode, name As String) As BoundExpression
                Dim argument As New BoundLiteral(
                    syntax,
                    Microsoft.CodeAnalysis.ConstantValue.Create(name),
                    method.Parameters(0).Type)
                Dim result As New BoundCall(
                    syntax,
                    method,
                    methodGroupOpt:=Nothing,
                    receiverOpt:=Nothing,
                    arguments:=ImmutableArray.Create(Of BoundExpression)(argument),
                    constantValueOpt:=Nothing,
                    suppressObjectClone:=False,
                    type:=method.ReturnType)
                Return result.MakeCompilerGenerated()
            End Function
        End Class

    End Class

End Namespace
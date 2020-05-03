' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class ObjectIdLocalSymbol
        Inherits PlaceholderLocalSymbol

        Private ReadOnly _isReadOnly As Boolean

        Friend Sub New(method As MethodSymbol, type As TypeSymbol, name As String, displayName As String, isReadOnly As Boolean)
            MyBase.New(method, name, displayName, type)
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
            syntax As SyntaxNode,
            isLValue As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

            Return RewriteLocalInternal(compilation, container, syntax, Me, isLValue:=isLValue)
        End Function

        Friend Overloads Shared Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As SyntaxNode,
            local As LocalSymbol,
            isLValue As Boolean) As BoundExpression

            Return RewriteLocalInternal(compilation, container, syntax, local, isLValue:=isLValue)
        End Function

        Private Overloads Shared Function RewriteLocalInternal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As SyntaxNode,
            local As LocalSymbol,
            isLValue As Boolean) As BoundExpression

            Dim variable = New BoundPseudoVariable(
                syntax,
                local,
                isLValue:=True,
                emitExpressions:=New ObjectIdExpressions(compilation),
                type:=local.Type).MakeCompilerGenerated()
            If isLValue Then
                Return variable
            End If
            Return variable.MakeRValue()
        End Function

        Private NotInheritable Class ObjectIdExpressions
            Inherits PseudoVariableExpressions

            Private ReadOnly _compilation As VisualBasicCompilation

            Friend Sub New(compilation As VisualBasicCompilation)
                _compilation = compilation
            End Sub

            Friend Overrides Function GetValue(variable As BoundPseudoVariable, diagnostics As DiagnosticBag) As BoundExpression
                Dim method = GetIntrinsicMethod(_compilation, ExpressionCompilerConstants.GetVariableValueMethodName)
                Dim local = variable.LocalSymbol
                Dim expr = InvokeGetMethod(method, variable.Syntax, local.Name)
                Return ConvertToLocalType(_compilation, expr, local.Type, diagnostics)
            End Function

            Friend Overrides Function GetAddress(variable As BoundPseudoVariable) As BoundExpression
                Dim method = GetIntrinsicMethod(_compilation, ExpressionCompilerConstants.GetVariableAddressMethodName)
                ' Currently the MetadataDecoder does not support byref return types
                ' so the return type of GetVariableAddress(Of T)(name As String)
                ' is an error type. Since the method is only used for emit, an
                ' updated placeholder method is used instead.

                ' TODO: refs are available
                'Debug.Assert(method.ReturnType.TypeKind = TypeKind.Error) ' If byref return types are supported in the future, use method as is.
                method = New PlaceholderMethodSymbol(
                    method.ContainingType,
                    method.Name,
                    Function(m) method.TypeParameters.SelectAsArray(Function(t) DirectCast(New SimpleTypeParameterSymbol(m, t.Ordinal, t.Name), TypeParameterSymbol)),
                    Function(m) m.TypeParameters(0), ' return type is <>T&
                    Function(m) method.Parameters.SelectAsArray(Function(p) DirectCast(New SynthesizedParameterSymbol(m, p.Type, p.Ordinal, p.IsByRef, p.Name), ParameterSymbol)))
                Dim local = variable.LocalSymbol
                Return InvokeGetMethod(method.Construct(local.Type), variable.Syntax, local.Name)
            End Function

            Private Shared Function InvokeGetMethod(method As MethodSymbol, syntax As SyntaxNode, name As String) As BoundExpression
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

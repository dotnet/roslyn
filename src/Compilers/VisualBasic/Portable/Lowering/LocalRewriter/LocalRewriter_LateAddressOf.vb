' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitLateAddressOfOperator(node As BoundLateAddressOfOperator) As BoundNode
            If _inExpressionLambda Then
                ' just preserve the node to report an error in ExpressionLambdaRewriter
                Return MyBase.VisitLateAddressOfOperator(node)
            End If

            Dim targetType = DirectCast(node.Type, NamedTypeSymbol)
            Dim lambda = BuildDelegateRelaxationLambda(node.Syntax, targetType, node.MemberAccess, node.Binder, Me._diagnostics)

            Return Me.VisitExpressionNode(lambda)
        End Function

        Private Shared Function BuildDelegateRelaxationLambda(
                syntaxNode As SyntaxNode,
                targetType As NamedTypeSymbol,
                boundMember As BoundLateMemberAccess,
                binder As Binder,
                diagnostics As BindingDiagnosticBag
            ) As BoundExpression

            Dim delegateInvoke = targetType.DelegateInvokeMethod
            Debug.Assert(delegateInvoke.MethodKind = MethodKind.DelegateInvoke)

            ' build lambda symbol parameters matching the invocation method exactly. To do this,
            ' we'll create a BoundLambdaParameterSymbol for each parameter of the invoke method.
            Dim delegateInvokeReturnType = delegateInvoke.ReturnType
            Dim invokeParameters = delegateInvoke.Parameters
            Dim invokeParameterCount = invokeParameters.Length

            Dim lambdaSymbolParameters(invokeParameterCount - 1) As BoundLambdaParameterSymbol
            Dim addressOfLocation As Location = syntaxNode.GetLocation()

            For parameterIndex = 0 To invokeParameterCount - 1
                Dim parameter = invokeParameters(parameterIndex)
                lambdaSymbolParameters(parameterIndex) = New BoundLambdaParameterSymbol(GeneratedNames.MakeDelegateRelaxationParameterName(parameterIndex),
                                                                                        parameter.Ordinal,
                                                                                        parameter.Type,
                                                                                        parameter.IsByRef,
                                                                                        syntaxNode,
                                                                                        addressOfLocation)
            Next

            ' even if the return value is dropped, we're using the delegate's return type for 
            ' this lambda symbol.
            Dim lambdaSymbol = New SynthesizedLambdaSymbol(SynthesizedLambdaKind.LateBoundAddressOfLambda,
                                                           syntaxNode,
                                                           lambdaSymbolParameters.AsImmutableOrNull,
                                                           delegateInvokeReturnType,
                                                           binder)

            ' the body of the lambda only contains a call to the target (or a return of the return value of 
            ' the call in case of a function)

            ' for each parameter of the lambda symbol/invoke method we will create a bound parameter, except
            ' we are implementing a zero argument relaxation.
            ' These parameters will be used in the method invocation as passed parameters.
            Dim lambdaBoundParameters(invokeParameterCount - 1) As BoundExpression

            For parameterIndex = 0 To lambdaSymbolParameters.Length - 1
                Dim lambdaSymbolParameter = lambdaSymbolParameters(parameterIndex)
                Dim boundParameter = New BoundParameter(syntaxNode,
                                                        lambdaSymbolParameter,
                                                        lambdaSymbolParameter.Type)
                boundParameter.SetWasCompilerGenerated()
                lambdaBoundParameters(parameterIndex) = boundParameter
            Next

            'The invocation of the target method must be bound in the context of the lambda
            'The reason is that binding the invoke may introduce local symbols and they need 
            'to be properly parented to the lambda and not to the outer method.
            Dim lambdaBinder = New LambdaBodyBinder(lambdaSymbol, binder)

            ' Dev10 ignores the type characters used in the operand of an AddressOf operator.
            ' NOTE: we suppress suppressAbstractCallDiagnostics because it 
            '       should have been reported already
            Dim boundInvocationExpression As BoundExpression = lambdaBinder.BindLateBoundInvocation(syntaxNode,
                                                                                                    Nothing,
                                                                                                    boundMember,
                                                                                                    lambdaBoundParameters.AsImmutableOrNull,
                                                                                                    Nothing,
                                                                                                    diagnostics,
                                                                                                    suppressLateBindingResolutionDiagnostics:=True)

            boundInvocationExpression.SetWasCompilerGenerated()

            ' In case of a function target that got assigned to a sub delegate, the return value will be dropped
            Dim statementList As ImmutableArray(Of BoundStatement) = Nothing
            If lambdaSymbol.IsSub Then

                If boundInvocationExpression.IsLateBound() Then
                    boundInvocationExpression = boundInvocationExpression.SetLateBoundAccessKind(LateBoundAccessKind.Call)
                End If

                Dim statements(1) As BoundStatement
                Dim boundStatement As BoundStatement = New BoundExpressionStatement(syntaxNode, boundInvocationExpression)
                boundStatement.SetWasCompilerGenerated()
                statements(0) = boundStatement
                boundStatement = New BoundReturnStatement(syntaxNode, Nothing, Nothing, Nothing)
                boundStatement.SetWasCompilerGenerated()
                statements(1) = boundStatement
                statementList = statements.AsImmutableOrNull
            Else
                ' process conversions between the return types of the target and invoke function if needed.
                boundInvocationExpression = lambdaBinder.ApplyImplicitConversion(syntaxNode,
                                                                                 delegateInvokeReturnType,
                                                                                 boundInvocationExpression,
                                                                                 diagnostics)

                Dim returnstmt As BoundStatement = New BoundReturnStatement(syntaxNode,
                                                                            boundInvocationExpression,
                                                                            Nothing,
                                                                            Nothing)
                returnstmt.SetWasCompilerGenerated()
                statementList = ImmutableArray.Create(returnstmt)
            End If

            Dim lambdaBody = New BoundBlock(syntaxNode,
                                            Nothing,
                                            ImmutableArray(Of LocalSymbol).Empty,
                                            statementList)
            lambdaBody.SetWasCompilerGenerated()

            Dim boundLambda = New BoundLambda(syntaxNode,
                                          lambdaSymbol,
                                          lambdaBody,
                                          ReadOnlyBindingDiagnostic(Of AssemblySymbol).Empty,
                                          Nothing,
                                          ConversionKind.DelegateRelaxationLevelWidening,
                                          MethodConversionKind.Identity)
            boundLambda.SetWasCompilerGenerated()

            Dim result = New BoundDirectCast(syntaxNode,
                                             boundLambda,
                                             ConversionKind.DelegateRelaxationLevelWidening,
                                             targetType)

            Return result
        End Function

    End Class
End Namespace

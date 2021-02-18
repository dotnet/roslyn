﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class UnboundLambda
        Inherits BoundExpression

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert((Flags And Not (SourceMemberFlags.Async Or SourceMemberFlags.Iterator)) = 0)
        End Sub
#End If

        ''' <summary>
        ''' Should this lambda be treated as a single line lambda?
        ''' </summary>
        Public ReadOnly Property IsSingleLine As Boolean
            Get
                Debug.Assert(TypeOf Me.Syntax Is LambdaExpressionSyntax)
                Dim kind As SyntaxKind = Me.Syntax.Kind

                Return kind = SyntaxKind.SingleLineFunctionLambdaExpression OrElse
                       kind = SyntaxKind.SingleLineSubLambdaExpression
            End Get
        End Property

        ''' <summary>
        ''' Is this a function lambda
        ''' </summary>
        Public ReadOnly Property IsFunctionLambda As Boolean
            Get
                Debug.Assert(TypeOf Me.Syntax Is LambdaExpressionSyntax)
                Dim kind As SyntaxKind = Me.Syntax.Kind

                Return kind = SyntaxKind.SingleLineFunctionLambdaExpression OrElse
                       kind = SyntaxKind.MultiLineFunctionLambdaExpression
            End Get
        End Property

        Public Function Bind(target As TargetSignature) As BoundLambda
            Debug.Assert(target IsNot Nothing)
            Dim result As BoundLambda = _BindingCache.BoundLambdas.GetOrAdd(target, AddressOf DoBind)
            Debug.Assert(result IsNot Nothing)
            Return result
        End Function

        ''' <summary>
        ''' target.ReturnType is ignored and must be Void, only parameter types are taken into consideration.
        ''' </summary>
        Public Function InferReturnType(target As TargetSignature) As KeyValuePair(Of TypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))
            Debug.Assert(target IsNot Nothing AndAlso target.ReturnType.IsVoidType())

            If Me.ReturnType IsNot Nothing Then
                Dim result = New KeyValuePair(Of TypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))(If(Me.IsFunctionLambda AndAlso Me.ReturnType.IsVoidType(),
                                                                               LambdaSymbol.ReturnTypeVoidReplacement,
                                                                         Me.ReturnType),
                                                                      Nothing)

                Return _BindingCache.InferredReturnType.GetOrAdd(target, result)
            End If

            Debug.Assert(Me.IsFunctionLambda)

            Return _BindingCache.InferredReturnType.GetOrAdd(target, AddressOf DoInferFunctionLambdaReturnType)
        End Function

        Public Function BindForErrorRecovery() As BoundLambda
            Return _Binder.BindLambdaForErrorRecovery(Me)
        End Function

        Public Function GetBoundLambda(target As TargetSignature) As BoundLambda
            Dim result As BoundLambda = Nothing

            If _BindingCache.BoundLambdas.TryGetValue(target, result) Then
                Return result
            End If

            Return Nothing
        End Function

        Private Function GetSingletonBoundLambda() As BoundLambda

            Dim result As BoundLambda = _BindingCache.BoundLambdas.Values.FirstOrDefault()

            If _BindingCache.BoundLambdas.Count = 1 Then
                Return result
            End If

            Return Nothing
        End Function

        Private Function DoBind(target As TargetSignature) As BoundLambda
            Return _Binder.BindUnboundLambda(Me, target)
        End Function

        Private Function DoInferFunctionLambdaReturnType(target As TargetSignature) As KeyValuePair(Of TypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))
            Return _Binder.InferFunctionLambdaReturnType(Me, target)
        End Function

        Public ReadOnly Property InferredAnonymousDelegate As KeyValuePair(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))
            Get
                Dim info As Tuple(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol)) = _BindingCache.AnonymousDelegate
                If info Is Nothing Then
                    Dim delegateInfo As KeyValuePair(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol)) = _Binder.InferAnonymousDelegateForLambda(Me)

                    Interlocked.CompareExchange(_BindingCache.AnonymousDelegate,
                                                New Tuple(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))(delegateInfo.Key, delegateInfo.Value),
                                                Nothing)

                    info = _BindingCache.AnonymousDelegate
                End If

                Return New KeyValuePair(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))(info.Item1, info.Item2)
            End Get
        End Property

        Public Function IsInferredDelegateForThisLambda(delegateType As NamedTypeSymbol) As Boolean
            Dim info As Tuple(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol)) = _BindingCache.AnonymousDelegate
            If info Is Nothing Then
                Return False
            End If

            Return delegateType Is info.Item1
        End Function

        Public ReadOnly Property WithDependencies As Boolean
            Get
                Return _BindingCache.WithDependencies
            End Get
        End Property

        Friend Class TargetSignature
            Public ReadOnly ParameterTypes As ImmutableArray(Of TypeSymbol)
            Public ReadOnly ReturnType As TypeSymbol
            Public ReadOnly ReturnsByRef As Boolean
            Public ReadOnly ParameterIsByRef As BitVector

            Public Sub New(parameterTypes As ImmutableArray(Of TypeSymbol), parameterIsByRef As BitVector, returnType As TypeSymbol, returnsByRef As Boolean)
                Debug.Assert(Not parameterTypes.IsDefault)
                Debug.Assert(Not parameterIsByRef.IsNull)
                Debug.Assert(returnType IsNot Nothing)
                Me.ParameterTypes = parameterTypes
                Me.ParameterIsByRef = parameterIsByRef
                Me.ReturnType = returnType
                Me.ReturnsByRef = returnsByRef
            End Sub

            Public Sub New(params As ImmutableArray(Of ParameterSymbol), returnType As TypeSymbol, returnsByRef As Boolean)
                Debug.Assert(Not params.IsDefault)
                Debug.Assert(returnType IsNot Nothing)

                Dim isByRef = BitVector.Empty

                If params.Length = 0 Then
                    Me.ParameterTypes = ImmutableArray(Of TypeSymbol).Empty
                Else
                    Dim types(params.Length - 1) As TypeSymbol
                    Dim i As Integer

                    For i = 0 To params.Length - 1
                        types(i) = params(i).Type
                        If params(i).IsByRef Then
                            isByRef(i) = True
                        End If
                    Next

                    Me.ParameterTypes = types.AsImmutableOrNull
                End If

                Me.ParameterIsByRef = isByRef
                Me.ReturnType = returnType
                Me.ReturnsByRef = returnsByRef
            End Sub

            Public Sub New(method As MethodSymbol)
                Me.New(method.Parameters, method.ReturnType, method.ReturnsByRef)
            End Sub

            Public Overrides Function GetHashCode() As Integer
                Dim hashVal As Integer = 0

                For Each item In ParameterTypes
                    hashVal = Hash.Combine(item, hashVal)
                Next

                hashVal = Hash.Combine(ReturnType, hashVal)

                Return hashVal
            End Function

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, TargetSignature)

                If other Is Nothing OrElse other.ParameterTypes.Length <> Me.ParameterTypes.Length Then
                    Return False
                End If

                For i As Integer = 0 To ParameterTypes.Length - 1
                    If Not TypeSymbol.Equals(Me.ParameterTypes(i), other.ParameterTypes(i), TypeCompareKind.ConsiderEverything) OrElse
                       Me.ParameterIsByRef(i) <> other.ParameterIsByRef(i) Then
                        Return False
                    End If
                Next

                Return Me.ReturnsByRef = other.ReturnsByRef AndAlso TypeSymbol.Equals(Me.ReturnType, other.ReturnType, TypeCompareKind.ConsiderEverything)
            End Function
        End Class

        ''' <summary>
        ''' This class is used to cache various information about a lambda in the course of binding an expression/statement
        ''' containing the lambda. Even though the members are public, they shouldn't be accessed directly by any code
        ''' outside of the UnboundLambda class.
        ''' </summary>
        Public Class UnboundLambdaBindingCache
            Public ReadOnly WithDependencies As Boolean
            Public AnonymousDelegate As Tuple(Of NamedTypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol))
            Public ReadOnly InferredReturnType As New ConcurrentDictionary(Of TargetSignature, KeyValuePair(Of TypeSymbol, ImmutableBindingDiagnostic(Of AssemblySymbol)))()
            Public ReadOnly BoundLambdas As New ConcurrentDictionary(Of TargetSignature, BoundLambda)()
            Public ErrorRecoverySignature As TargetSignature

            Public Sub New(withDependencies As Boolean)
                Me.WithDependencies = withDependencies
            End Sub
        End Class
    End Class

End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SourceLambdaSymbol
        Inherits LambdaSymbol

        Private ReadOnly m_UnboundLambda As UnboundLambda

        ' The anonymous type symbol associated with this lambda
        Private m_lazyAnonymousDelegateSymbol As NamedTypeSymbol = ErrorTypeSymbol.UnknownResultType

        Public Sub New(
            syntaxNode As VisualBasicSyntaxNode,
            unboundLambda As UnboundLambda,
            parameters As ImmutableArray(Of BoundLambdaParameterSymbol),
            returnType As TypeSymbol,
            binder As Binder)

            MyBase.New(syntaxNode, parameters, returnType, binder)

            Debug.Assert(returnType IsNot ReturnTypePendingDelegate)
            Debug.Assert(unboundLambda IsNot Nothing)

            m_UnboundLambda = unboundLambda
        End Sub

        Public ReadOnly Property UnboundLambda As UnboundLambda
            Get
                Return m_UnboundLambda
            End Get
        End Property

        Public Overrides ReadOnly Property SynthesizedKind As SynthesizedLambdaKind
            Get
                Return SynthesizedLambdaKind.UserDefined
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return (m_UnboundLambda.Flags And SourceMemberFlags.Async) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return (m_UnboundLambda.Flags And SourceMemberFlags.Iterator) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedAnonymousDelegate As NamedTypeSymbol
            Get
                If Me.m_lazyAnonymousDelegateSymbol Is ErrorTypeSymbol.UnknownResultType Then
                    Dim newValue As NamedTypeSymbol = MakeAssociatedAnonymousDelegate()
                    Dim oldValue As NamedTypeSymbol = Interlocked.CompareExchange(Me.m_lazyAnonymousDelegateSymbol, newValue, ErrorTypeSymbol.UnknownResultType)
                    Debug.Assert(oldValue Is ErrorTypeSymbol.UnknownResultType OrElse oldValue Is newValue)
                End If
                Return Me.m_lazyAnonymousDelegateSymbol
            End Get
        End Property

        Friend Function MakeAssociatedAnonymousDelegate() As NamedTypeSymbol
            Dim anonymousDelegateSymbol As NamedTypeSymbol = Me.m_UnboundLambda.InferredAnonymousDelegate.Key
            Dim targetSignature As New UnboundLambda.TargetSignature(anonymousDelegateSymbol.DelegateInvokeMethod)
            Dim boundLambda As BoundLambda = Me.m_UnboundLambda.Bind(targetSignature)

            ' NOTE: If the lambda does not have an associated anonymous delegate, but 
            ' NOTE: the target signature of the lambda is the same as its anonymous delegate 
            ' NOTE: would have had if it were created, we still return this delegate. 
            ' NOTE: This is caused by performance trade-offs made in lambda binding

            If boundLambda.LambdaSymbol IsNot Me Then
                Return Nothing
            End If

            Return anonymousDelegateSymbol
        End Function
    End Class
End Namespace

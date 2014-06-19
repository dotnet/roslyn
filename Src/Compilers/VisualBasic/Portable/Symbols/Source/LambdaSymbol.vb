' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Threading

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a method symbol for a lambda method.
    ''' </summary>
    Friend Class LambdaSymbol
        Inherits MethodSymbol

        ''' <summary>
        ''' This symbol is used as the return type of a LambdaSymbol when we are interpreting 
        ''' lambda's body in order to infer its return type.
        ''' </summary>
        Friend Shared ReadOnly ReturnTypeIsBeingInferred As TypeSymbol = New ErrorTypeSymbol()

        ''' <summary>
        ''' This symbol is used as the return type of a LambdaSymbol when we failed to 
        ''' infer lambda's return type, but still want to interpret its body.
        ''' </summary>
        Friend Shared ReadOnly ReturnTypeIsUnknown As TypeSymbol = New ErrorTypeSymbol()

        ''' <summary>
        ''' This symbol is used as the return type of a LambdaSymbol when we are dealing with
        ''' query lambda and the return type should be taken from the target delegate upon
        ''' successful conversion. The LambdaSymbol will be mutated then. 
        ''' </summary>
        Friend Shared ReadOnly ReturnTypePendingDelegate As TypeSymbol = New ErrorTypeSymbol()

        ''' <summary>
        ''' This symbol is used as the return type of a LambdaSymbol when System.Void is used in code.
        ''' </summary>
        Friend Shared ReadOnly ReturnTypeVoidReplacement As TypeSymbol = New ErrorTypeSymbol()

        ''' <summary>
        ''' This symbol is used as a sentinel while we are binding a lambda in error recovery mode.
        ''' </summary>
        Friend Shared ReadOnly ErrorRecoveryInferenceError As TypeSymbol = New ErrorTypeSymbol()

        Private ReadOnly m_SyntaxNode As VisualBasicSyntaxNode
        Private ReadOnly m_UnboundLambdaOpt As UnboundLambda
        Private ReadOnly m_Parameters As ImmutableArray(Of ParameterSymbol)

        ''' <summary>
        ''' Can mutate for a query lambda from ReturnTypePendingDelegate 
        ''' to the return type of the target delegate.
        ''' </summary>
        Protected m_ReturnType As TypeSymbol

        ' The binder associated with the block containing this lambda
        Private ReadOnly m_Binder As Binder

        ' The anonymous type symbol associated with this lambda
        Private m_lazyAnonymousDelegateSymbol As NamedTypeSymbol = ErrorTypeSymbol.UnknownResultType

        Public Sub New(
            syntaxNode As VisualBasicSyntaxNode,
            unboundLambdaOpt As UnboundLambda,
            parameters As ImmutableArray(Of BoundLambdaParameterSymbol),
            returnType As TypeSymbol,
            binder As Binder
        )
            Debug.Assert(syntaxNode IsNot Nothing)
            Debug.Assert(returnType IsNot Nothing)
            Debug.Assert((returnType Is ReturnTypePendingDelegate) = Me.IsQueryLambdaMethod)

            m_SyntaxNode = syntaxNode
            m_UnboundLambdaOpt = unboundLambdaOpt
            m_Parameters = StaticCast(Of ParameterSymbol).From(parameters)
            m_ReturnType = returnType
            m_Binder = binder

            For Each param In parameters
                param.SetLambdaSymbol(Me)
            Next
        End Sub

        Public ReadOnly Property UnboundLambdaOpt As UnboundLambda
            Get
                Return m_UnboundLambdaOpt
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedAnonymousDelegate As NamedTypeSymbol
            Get
                If Me.m_lazyAnonymousDelegateSymbol Is ErrorTypeSymbol.UnknownResultType Then
                    Dim newValue As NamedTypeSymbol = MakeAssociatedAnonymousDelegate()
                    Dim oldValue As NamedTypeSymbol = Interlocked.CompareExchange(Me.m_lazyAnonymousDelegateSymbol, newValue,
                                                                                  DirectCast(ErrorTypeSymbol.UnknownResultType, NamedTypeSymbol))
                    Debug.Assert(oldValue Is ErrorTypeSymbol.UnknownResultType OrElse oldValue Is newValue)
                End If
                Return Me.m_lazyAnonymousDelegateSymbol
            End Get
        End Property

        Friend Function MakeAssociatedAnonymousDelegate() As NamedTypeSymbol
            If Me.m_UnboundLambdaOpt Is Nothing Then
                Return Nothing
            End If

            Dim anonymousDelegateSymbol As NamedTypeSymbol = Me.m_UnboundLambdaOpt.InferredAnonymousDelegate.Key
            Dim targetSignature As New UnboundLambda.TargetSignature(anonymousDelegateSymbol.DelegateInvokeMethod)
            Dim boundLambda As BoundLambda = Me.m_UnboundLambdaOpt.Bind(targetSignature)

            ' NOTE: If the lambda does not have an associated anonymous delegate, but 
            ' NOTE: the target signature of the lambda is the same as its anonymous delegate 
            ' NOTE: would have had if it were created, we still return this delegate. 
            ' NOTE: This is caused by performance trade-offs made in lambda binding

            If boundLambda.LambdaSymbol IsNot Me Then
                Return Nothing
            End If

            Return anonymousDelegateSymbol
        End Function

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                ' lambdas contain user code
                Return True
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return Microsoft.Cci.CallingConvention.Default
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_Binder.ContainingMember
            End Get
        End Property

        Friend ReadOnly Property ContainingBinder As Binder
            Get
                Return m_Binder
            End Get
        End Property

        ''' <summary>
        ''' "Me" parameter for this lambda will be that of the containing symbol
        ''' </summary>
        Friend Overrides ReadOnly Property MeParameter As ParameterSymbol
            Get
                Debug.Assert(ContainingSymbol IsNot Nothing)
                Select Case ContainingSymbol.Kind
                    Case SymbolKind.Field
                        Return DirectCast(ContainingSymbol, FieldSymbol).MeParameter
                    Case SymbolKind.Property
                        Return DirectCast(ContainingSymbol, PropertySymbol).MeParameter
                    Case SymbolKind.Method
                        Return DirectCast(ContainingSymbol, MethodSymbol).MeParameter
                    Case Else
                        Return Nothing
                End Select
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Dim container As Symbol = ContainingSymbol

                Select Case container.Kind
                    Case SymbolKind.Field, SymbolKind.Property, SymbolKind.Method
                        Return container.IsShared
                    Case Else
                        Return True
                End Select
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return m_ReturnType.IsVoidType()
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return m_UnboundLambdaOpt IsNot Nothing AndAlso (m_UnboundLambdaOpt.Flags And SourceMemberFlags.Async) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return m_UnboundLambdaOpt IsNot Nothing AndAlso (m_UnboundLambdaOpt.Flags And SourceMemberFlags.Iterator) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(Of Location)(m_SyntaxNode.GetLocation())
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray.Create(Of SyntaxReference)(m_SyntaxNode.GetReference())
            End Get
        End Property

        Friend Overrides ReadOnly Property IsLambdaMethod As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.LambdaMethod
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return m_Parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_ReturnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return m_SyntaxNode
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return False
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            End If

            Dim symbol = TryCast(obj, LambdaSymbol)

            Return symbol IsNot Nothing AndAlso
                symbol.m_SyntaxNode Is Me.m_SyntaxNode AndAlso
                Equals(symbol.ContainingSymbol, Me.ContainingSymbol) AndAlso
                MethodSignatureComparer.AllAspectsSignatureComparer.Equals(symbol, Me)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hc As Integer = Hash.Combine(Me.Syntax.GetHashCode(), Me.m_Parameters.Length)
            hc = Hash.Combine(hc, Me.ReturnType.GetHashCode())
            For i = 0 To Me.m_Parameters.Length - 1
                hc = Hash.Combine(hc, Me.m_Parameters(i).Type.GetHashCode())
            Next
            Return hc
        End Function

    End Class

    Friend Class QueryLambdaSymbol
        Inherits SynthesizedLambdaSymbol

        Public Sub New(
            syntaxNode As VisualBasicSyntaxNode,
            parameters As ImmutableArray(Of BoundLambdaParameterSymbol),
            binder As Binder
        )
            MyBase.New(syntaxNode,
                       parameters,
                       ReturnTypePendingDelegate,
                       binder,
                       isDelegateRelaxationStub:=False)
        End Sub

        Public Sub SetQueryLambdaReturnType(returnType As TypeSymbol)
            Debug.Assert(m_ReturnType Is ReturnTypePendingDelegate)
            m_ReturnType = returnType
        End Sub

        Friend Overrides ReadOnly Property IsQueryLambdaMethod As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace


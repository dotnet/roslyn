' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a method symbol for a lambda method.
    ''' </summary>
    Friend MustInherit Class LambdaSymbol
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

        Private ReadOnly _syntaxNode As SyntaxNode
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        ''' <summary>
        ''' Can mutate for a query lambda from ReturnTypePendingDelegate 
        ''' to the return type of the target delegate.
        ''' </summary>
        Protected m_ReturnType As TypeSymbol

        ' The binder associated with the block containing this lambda
        Private ReadOnly _binder As Binder

        Protected Sub New(
            syntaxNode As SyntaxNode,
            parameters As ImmutableArray(Of BoundLambdaParameterSymbol),
            returnType As TypeSymbol,
            binder As Binder
        )
            Debug.Assert(syntaxNode IsNot Nothing)
            Debug.Assert(returnType IsNot Nothing)

            _syntaxNode = syntaxNode
            _parameters = StaticCast(Of ParameterSymbol).From(parameters)
            m_ReturnType = returnType
            _binder = binder

            For Each param In parameters
                param.SetLambdaSymbol(Me)
            Next
        End Sub

        Public MustOverride ReadOnly Property SynthesizedKind As SynthesizedLambdaKind

        Friend NotOverridable Overrides ReadOnly Property IsQueryLambdaMethod As Boolean
            Get
                Return SynthesizedKind.IsQueryLambda
            End Get
        End Property

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
                Return Cci.CallingConvention.Default
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _binder.ContainingMember
            End Get
        End Property

        Friend ReadOnly Property ContainingBinder As Binder
            Get
                Return _binder
            End Get
        End Property

        ''' <summary>
        ''' "Me" parameter for this lambda will be that of the containing symbol
        ''' </summary>
        Friend Overrides Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            Debug.Assert(ContainingSymbol IsNot Nothing)
            Select Case ContainingSymbol.Kind
                Case SymbolKind.Field
                    meParameter = DirectCast(ContainingSymbol, FieldSymbol).MeParameter
                Case SymbolKind.Property
                    meParameter = DirectCast(ContainingSymbol, PropertySymbol).MeParameter
                Case SymbolKind.Method
                    meParameter = DirectCast(ContainingSymbol, MethodSymbol).MeParameter
                Case Else
                    meParameter = Nothing
            End Select
            Return True
        End Function

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

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(_syntaxNode.GetLocation())
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray.Create(_syntaxNode.GetReference())
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
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return False
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

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return _syntaxNode
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

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                ' lambdas contain user code
                Return True
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Me Then
                Return True
            End If

            Dim symbol = TryCast(obj, LambdaSymbol)

            Return symbol IsNot Nothing AndAlso
                symbol._syntaxNode Is Me._syntaxNode AndAlso
                Equals(symbol.ContainingSymbol, Me.ContainingSymbol) AndAlso
                MethodSignatureComparer.AllAspectsSignatureComparer.Equals(symbol, Me)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hc As Integer = Hash.Combine(Me.Syntax.GetHashCode(), Me._parameters.Length)
            hc = Hash.Combine(hc, Me.ReturnType.GetHashCode())
            For i = 0 To Me._parameters.Length - 1
                hc = Hash.Combine(hc, Me._parameters(i).Type.GetHashCode())
            Next
            Return hc
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace


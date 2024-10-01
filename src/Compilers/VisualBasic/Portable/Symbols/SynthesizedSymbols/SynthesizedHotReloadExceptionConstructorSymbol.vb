' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class SynthesizedHotReloadExceptionConstructorSymbol
        Inherits SynthesizedConstructorBase

        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        Public Sub New(container As NamedTypeSymbol, stringType As TypeSymbol, intType As TypeSymbol)
            MyBase.New(VisualBasicSyntaxTree.DummyReference, container, isShared:=False, binder:=Nothing, diagnostics:=Nothing)

            _parameters = ImmutableArray.Create(Of ParameterSymbol)(
                New SynthesizedParameterSymbol(Me, stringType, ordinal:=0, isByRef:=False),
                New SynthesizedParameterSymbol(Me, intType, ordinal:=1, isByRef:=False))
        End Sub

        ''' <summary>
        ''' Exception message.
        ''' </summary>
        Public ReadOnly Property MessageParameter As ParameterSymbol
            Get
                Return _parameters(0)
            End Get
        End Property

        ''' <summary>
        ''' Integer value of <see cref="HotReloadExceptionCode"/>.
        ''' </summary>
        Public ReadOnly Property CodeParameter As ParameterSymbol
            Get
                Return _parameters(1)
            End Get
        End Property

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return _parameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, <Out> ByRef Optional methodBodyBinder As Binder = Nothing) As BoundBlock
            Dim containingExceptionType = DirectCast(ContainingType, SynthesizedHotReloadExceptionSymbol)

            Dim factory = New SyntheticBoundNodeFactory(Me, Me, Syntax, compilationState, diagnostics)

            Dim exceptionConstructor = factory.WellKnownMember(Of MethodSymbol)(WellKnownMember.System_Exception__ctorString, isOptional:=True)
            If exceptionConstructor Is Nothing Then
                diagnostics.Add(ERRID.ERR_EncUpdateFailedMissingSymbol,
                    Location.None,
                    CodeAnalysisResources.Constructor,
                    "System.Exception..ctor(string)")

                Return factory.Block()
            End If

            Dim block = factory.Block(
                ImmutableArray.Create(Of BoundStatement)(
                    factory.ExpressionStatement(factory.Call(
                        factory.Me(),
                        exceptionConstructor,
                        factory.Parameter(MessageParameter, isLValue:=False))),
                    factory.Assignment(factory.Field(factory.Me(), containingExceptionType.CodeField, isLValue:=True), factory.Parameter(CodeParameter, isLValue:=False)),
                    factory.Return()
                ))

            Return block
        End Function
    End Class
End Namespace

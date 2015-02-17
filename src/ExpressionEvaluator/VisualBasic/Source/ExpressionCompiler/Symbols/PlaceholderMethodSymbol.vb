' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class PlaceholderMethodSymbol
        Inherits SynthesizedMethodBase
        Implements Cci.ISignature

        Friend Delegate Function GetTypeParameters(method As PlaceholderMethodSymbol) As ImmutableArray(Of TypeParameterSymbol)
        Friend Delegate Function GetParameters(method As PlaceholderMethodSymbol) As ImmutableArray(Of ParameterSymbol)
        Friend Delegate Function GetReturnType(method As PlaceholderMethodSymbol) As TypeSymbol

        Private ReadOnly _syntax As VisualBasicSyntaxNode
        Private ReadOnly _name As String
        Private ReadOnly _locations As ImmutableArray(Of Location)
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _returnValueIsByRef As Boolean

        Friend Sub New(
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            name As String,
            getTypeParameters As GetTypeParameters,
            getReturnType As GetReturnType,
            getParameters As GetParameters,
            returnValueIsByRef As Boolean)

            MyClass.New(container, syntax, name)

            _typeParameters = getTypeParameters(Me)
            _returnType = getReturnType(Me)
            _parameters = getParameters(Me)
            _returnValueIsByRef = returnValueIsByRef
        End Sub

        Friend Sub New(
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            name As String,
            returnType As TypeSymbol,
            getParameters As GetParameters)

            MyClass.New(container, syntax, name)

            Debug.Assert(
                (returnType.SpecialType = SpecialType.System_Void) OrElse
                (returnType.SpecialType = SpecialType.System_Object) OrElse
                (returnType.Name = "Exception"))

            _typeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
            _returnType = returnType
            _parameters = getParameters(Me)
        End Sub

        Private Sub New(
            container As EENamedTypeSymbol,
            syntax As VisualBasicSyntaxNode,
            name As String)

            MyBase.New(container)
            _syntax = syntax
            _name = name
            _locations = ImmutableArray.Create(syntax.GetLocation())
        End Sub

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Friend
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

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

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _returnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Private ReadOnly Property ReturnValueIsByRef As Boolean Implements Cci.ISignature.ReturnValueIsByRef
            Get
                Return _returnValueIsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(_typeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _locations
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(diagnostics As DiagnosticBag, <Out> ByRef Optional methodBodyBinder As Binder = Nothing) As BoundBlock
            ' The method body is "throw null;" although the body
            ' is arbitrary since the method will not be invoked.
            Dim exceptionType = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Exception)
            Dim value = New BoundLiteral(_syntax, ConstantValue.Nothing, exceptionType)
            Dim statement As BoundStatement = New BoundThrowStatement(_syntax, value)
            Return New BoundBlock(_syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(statement)).MakeCompilerGenerated()
        End Function

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class

End Namespace
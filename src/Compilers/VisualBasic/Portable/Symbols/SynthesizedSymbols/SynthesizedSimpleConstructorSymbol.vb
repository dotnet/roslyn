' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents a simple implementation of compiler generated constructors
    ''' </summary>
    Friend Class SynthesizedSimpleConstructorSymbol
        Inherits SynthesizedConstructorBase
        Implements ISynthesizedMethodBodyImplementationSymbol

        Private _parameters As ImmutableArray(Of ParameterSymbol)

        Public Sub New(container As NamedTypeSymbol)
            MyBase.New(VisualBasicSyntaxTree.DummyReference, container, False, Nothing, Nothing)
        End Sub

        ' Note: This should be called at most once, immediately after the symbol is constructed. The parameters aren't 
        ' Note: passed to the constructor because they need to have their container set correctly.
        ''' <summary>
        ''' Sets the parameters.
        ''' </summary>
        ''' <param name="parameters">The parameters.</param>
        Friend Sub SetParameters(parameters As ImmutableArray(Of ParameterSymbol))
            Debug.Assert(Not parameters.IsDefault)
            Debug.Assert(Me._parameters.IsDefault)

            Me._parameters = parameters
        End Sub

        Friend NotOverridable Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return Me._parameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return Me._parameters
            End Get
        End Property

        Public ReadOnly Property HasMethodBodyDependency As Boolean Implements ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Method As IMethodSymbolInternal Implements ISynthesizedMethodBodyImplementationSymbol.Method
            Get
                Dim symbol As ISynthesizedMethodBodyImplementationSymbol = CType(ContainingSymbol, ISynthesizedMethodBodyImplementationSymbol)
                Return symbol.Method
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class

End Namespace

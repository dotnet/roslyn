' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Copy constructor has one parameter of the same type as the enclosing type.
    ''' The purpose is to copy all the lifted values from previous version of the 
    ''' frame if there was any into the new one.
    ''' </summary>
    Friend Class SynthesizedLambdaCopyConstructor
        Inherits SynthesizedLambdaConstructor

        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        Friend Sub New(syntaxNode As SyntaxNode, containingType As LambdaFrame)
            MyBase.New(syntaxNode, containingType)

            _parameters = ImmutableArray.Create(Of ParameterSymbol)(New SourceSimpleParameterSymbol(Me, "arg0", 0, containingType, Nothing))
        End Sub

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
    End Class
End Namespace

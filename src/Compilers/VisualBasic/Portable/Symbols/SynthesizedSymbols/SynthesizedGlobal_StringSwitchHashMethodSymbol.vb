' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents a simple customizable hash method to hash the string constants
    ''' corresponding to the case clause string constants.
    ''' If we have at least one string type select case statement in a module that needs a
    ''' hash table based jump table, we generate a single public string hash synthesized
    ''' method (SynthesizedStringSwitchHashMethod) that is shared across the module.
    ''' We must emit this function into the compiler generated PrivateImplementationDetails class.
    ''' </summary>
    Partial Friend NotInheritable Class SynthesizedStringSwitchHashMethod
        Inherits SynthesizedGlobalMethodBase

        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnType As TypeSymbol

        Public Sub New(container As SourceModuleSymbol, privateImplType As PrivateImplementationDetails)
            MyBase.New(container, PrivateImplementationDetails.SynthesizedStringHashFunctionName, privateImplType)

            ' Signature:  uint ComputeStringHash(s as String)
            Dim compilation = Me.DeclaringCompilation

            _parameters = ImmutableArray.Create(Of ParameterSymbol)(New SynthesizedParameterSimpleSymbol(Me, compilation.GetSpecialType(SpecialType.System_String), 0, "s"))
            _returnType = compilation.GetSpecialType(SpecialType.System_UInt32)
        End Sub

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 1
            End Get
        End Property

        ''' <summary>
        ''' The parameters forming part of this signature.
        ''' </summary>
        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean  
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

    End Class

End Namespace

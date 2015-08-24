' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class ParameterTypeInformation
        Implements Cci.IParameterTypeInformation

        Private ReadOnly _underlyingParameter As ParameterSymbol

        Public Sub New(underlyingParameter As ParameterSymbol)
            Debug.Assert(underlyingParameter IsNot Nothing)
            Me._underlyingParameter = underlyingParameter
        End Sub

        Private ReadOnly Property IParameterTypeInformationCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.IParameterTypeInformation.CustomModifiers
            Get
                Return _underlyingParameter.CustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationIsByReference As Boolean Implements Cci.IParameterTypeInformation.IsByReference
            Get
                Return _underlyingParameter.IsByRef
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationCountOfCustomModifiersPrecedingByRef As UShort Implements Cci.IParameterTypeInformation.CountOfCustomModifiersPrecedingByRef
            Get
                Return _underlyingParameter.CountOfCustomModifiersPrecedingByRef
            End Get
        End Property

        Private Function IParameterTypeInformationGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.IParameterTypeInformation.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim paramType As TypeSymbol = _underlyingParameter.Type
            Return moduleBeingBuilt.Translate(paramType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements Cci.IParameterListEntry.Index
            Get
                Return CType(_underlyingParameter.Ordinal, UShort)
            End Get
        End Property
    End Class
End Namespace

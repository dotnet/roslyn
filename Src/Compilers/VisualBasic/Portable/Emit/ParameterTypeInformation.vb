' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class ParameterTypeInformation
        Implements Cci.IParameterTypeInformation

        Private ReadOnly m_UnderlyingParameter As ParameterSymbol

        Public Sub New(underlyingParameter As ParameterSymbol)
            Debug.Assert(underlyingParameter IsNot Nothing)
            Me.m_UnderlyingParameter = underlyingParameter
        End Sub

        Private ReadOnly Property IParameterTypeInformationCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.IParameterTypeInformation.CustomModifiers
            Get
                Return m_UnderlyingParameter.CustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationIsByReference As Boolean Implements Cci.IParameterTypeInformation.IsByReference
            Get
                Return m_UnderlyingParameter.IsByRef
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationHasByRefBeforeCustomModifiers As Boolean Implements Cci.IParameterTypeInformation.HasByRefBeforeCustomModifiers
            Get
                Return m_UnderlyingParameter.HasByRefBeforeCustomModifiers
            End Get
        End Property

        Private Function IParameterTypeInformationGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.IParameterTypeInformation.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim paramType As TypeSymbol = m_UnderlyingParameter.Type
            Return moduleBeingBuilt.Translate(paramType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VBSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements Cci.IParameterListEntry.Index
            Get
                Return CType(m_UnderlyingParameter.Ordinal, UShort)
            End Get
        End Property
    End Class
End Namespace

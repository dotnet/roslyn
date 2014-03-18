' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class ParameterTypeInformation
        Implements Microsoft.Cci.IParameterTypeInformation

        Private ReadOnly m_UnderlyingParameter As ParameterSymbol

        Public Sub New(underlyingParameter As ParameterSymbol)
            Debug.Assert(underlyingParameter IsNot Nothing)
            Me.m_UnderlyingParameter = underlyingParameter
        End Sub

        Private ReadOnly Property IParameterTypeInformationCustomModifiers As IEnumerable(Of Microsoft.Cci.ICustomModifier) Implements Microsoft.Cci.IParameterTypeInformation.CustomModifiers
            Get
                Return m_UnderlyingParameter.CustomModifiers
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationIsByReference As Boolean Implements Microsoft.Cci.IParameterTypeInformation.IsByReference
            Get
                Return m_UnderlyingParameter.IsByRef
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationIsModified As Boolean Implements Microsoft.Cci.IParameterTypeInformation.IsModified
            Get
                Return m_UnderlyingParameter.CustomModifiers.Length <> 0
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationHasByRefBeforeCustomModifiers As Boolean Implements Microsoft.Cci.IParameterTypeInformation.HasByRefBeforeCustomModifiers
            Get
                Return m_UnderlyingParameter.HasByRefBeforeCustomModifiers
            End Get
        End Property

        Private Function IParameterTypeInformationGetType(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeReference Implements Microsoft.Cci.IParameterTypeInformation.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim paramType As TypeSymbol = m_UnderlyingParameter.Type
            Return moduleBeingBuilt.Translate(paramType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements Microsoft.Cci.IParameterListEntry.Index
            Get
                Return CType(m_UnderlyingParameter.Ordinal, UShort)
            End Get
        End Property
    End Class
End Namespace

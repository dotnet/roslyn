' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private ReadOnly Property IParameterTypeInformationRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.IParameterTypeInformation.RefCustomModifiers
            Get
                Return _underlyingParameter.RefCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private Function IParameterTypeInformationGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.IParameterTypeInformation.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim paramType As TypeSymbol = _underlyingParameter.Type
            Return moduleBeingBuilt.Translate(paramType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements Cci.IParameterListEntry.Index
            Get
                Return CType(_underlyingParameter.Ordinal, UShort)
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function GetHashCode() As Integer
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class MethodReference
        Inherits TypeMemberReference
        Implements Cci.IMethodReference

        Protected ReadOnly m_UnderlyingMethod As MethodSymbol

        Public Sub New(underlyingMethod As MethodSymbol)
            Debug.Assert(underlyingMethod IsNot Nothing)
            Me.m_UnderlyingMethod = underlyingMethod
        End Sub

        Protected Overrides ReadOnly Property UnderlyingSymbol As Symbol
            Get
                Return m_UnderlyingMethod
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceAcceptsExtraArguments As Boolean Implements Cci.IMethodReference.AcceptsExtraArguments
            Get
                Return m_UnderlyingMethod.IsVararg
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceGenericParameterCount As UShort Implements Cci.IMethodReference.GenericParameterCount
            Get
                Return CType(m_UnderlyingMethod.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceParameterCount As UShort Implements Cci.IMethodReference.ParameterCount
            Get
                Return CType(m_UnderlyingMethod.ParameterCount, UShort)
            End Get
        End Property
        Private Function IMethodReferenceGetResolvedMethod(context As EmitContext) As Cci.IMethodDefinition Implements Cci.IMethodReference.GetResolvedMethod
            Return Nothing
        End Function

        Private ReadOnly Property IMethodReferenceExtraParameters As ImmutableArray(Of Cci.IParameterTypeInformation) Implements Cci.IMethodReference.ExtraParameters
            Get
                Return ImmutableArray(Of Cci.IParameterTypeInformation).Empty
            End Get
        End Property

        Private ReadOnly Property ISignatureCallingConvention As Cci.CallingConvention Implements Cci.ISignature.CallingConvention
            Get
                Return m_UnderlyingMethod.CallingConvention
            End Get
        End Property

        Private Function ISignatureGetParameters(context As EmitContext) As ImmutableArray(Of Cci.IParameterTypeInformation) Implements Cci.ISignature.GetParameters
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Return moduleBeingBuilt.Translate(m_UnderlyingMethod.Parameters)
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.ISignature.ReturnValueCustomModifiers
            Get
                Return m_UnderlyingMethod.ReturnTypeCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.ISignature.RefCustomModifiers
            Get
                Return m_UnderlyingMethod.RefCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements Cci.ISignature.ReturnValueIsByRef
            Get
                Return m_UnderlyingMethod.ReturnsByRef
            End Get
        End Property

        Private Function ISignatureGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.ISignature.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim returnType As TypeSymbol = m_UnderlyingMethod.ReturnType

            Return moduleBeingBuilt.Translate(returnType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overridable ReadOnly Property AsGenericMethodInstanceReference As Cci.IGenericMethodInstanceReference Implements Cci.IMethodReference.AsGenericMethodInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property AsSpecializedMethodReference As Cci.ISpecializedMethodReference Implements Cci.IMethodReference.AsSpecializedMethodReference
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class MethodReference
        Inherits TypeMemberReference
        Implements Microsoft.Cci.IMethodReference

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

        Private ReadOnly Property IMethodReferenceAcceptsExtraArguments As Boolean Implements Microsoft.Cci.IMethodReference.AcceptsExtraArguments
            Get
                Return m_UnderlyingMethod.IsVararg
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceGenericParameterCount As UShort Implements Microsoft.Cci.IMethodReference.GenericParameterCount
            Get
                Return CType(m_UnderlyingMethod.Arity, UShort)
            End Get
        End Property
        Private ReadOnly Property IMethodReferenceIsGeneric As Boolean Implements Microsoft.Cci.IMethodReference.IsGeneric
            Get
                Return m_UnderlyingMethod.IsGenericMethod
            End Get
        End Property

        Private ReadOnly Property IMethodReferenceParameterCount As UShort Implements Microsoft.Cci.IMethodReference.ParameterCount
            Get
                Return CType(m_UnderlyingMethod.ParameterCount, UShort)
            End Get
        End Property
        Private Function IMethodReferenceGetResolvedMethod(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IMethodDefinition Implements Microsoft.Cci.IMethodReference.GetResolvedMethod
            Return Nothing
        End Function

        Private ReadOnly Property IMethodReferenceExtraParameters As ImmutableArray(Of Microsoft.Cci.IParameterTypeInformation) Implements Microsoft.Cci.IMethodReference.ExtraParameters
            Get
                Return ImmutableArray(Of Microsoft.Cci.IParameterTypeInformation).Empty
            End Get
        End Property

        Private ReadOnly Property ISignatureCallingConvention As Microsoft.Cci.CallingConvention Implements Microsoft.Cci.ISignature.CallingConvention
            Get
                Return m_UnderlyingMethod.CallingConvention
            End Get
        End Property

        Private Function ISignatureGetParameters(context As Microsoft.CodeAnalysis.Emit.Context) As ImmutableArray(Of Microsoft.Cci.IParameterTypeInformation) Implements Microsoft.Cci.ISignature.GetParameters
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Return moduleBeingBuilt.Translate(m_UnderlyingMethod.Parameters)
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As IEnumerable(Of Microsoft.Cci.ICustomModifier) Implements Microsoft.Cci.ISignature.ReturnValueCustomModifiers
            Get
                Return m_UnderlyingMethod.ReturnTypeCustomModifiers
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements Microsoft.Cci.ISignature.ReturnValueIsByRef
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsModified As Boolean Implements Microsoft.Cci.ISignature.ReturnValueIsModified
            Get
                Return m_UnderlyingMethod.ReturnTypeCustomModifiers.Length <> 0
            End Get
        End Property

        Private Function ISignatureGetType(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeReference Implements Microsoft.Cci.ISignature.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim returnType As TypeSymbol = m_UnderlyingMethod.ReturnType

            Return moduleBeingBuilt.Translate(returnType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overridable ReadOnly Property AsGenericMethodInstanceReference As Microsoft.Cci.IGenericMethodInstanceReference Implements Microsoft.Cci.IMethodReference.AsGenericMethodInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property AsSpecializedMethodReference As Microsoft.Cci.ISpecializedMethodReference Implements Microsoft.Cci.IMethodReference.AsSpecializedMethodReference
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

#If Not DEBUG Then
Imports MethodSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.MethodSymbol
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedMethod
        Inherits EmbeddedTypesManager.CommonEmbeddedMethod

        Public Sub New(containingType As EmbeddedType, underlyingMethod As MethodSymbolAdapter)
            MyBase.New(containingType, underlyingMethod)
        End Sub

        Friend Overrides ReadOnly Property TypeManager As EmbeddedTypesManager
            Get
                Return ContainingType.TypeManager
            End Get
        End Property

        Protected Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingMethod.AdaptedMethodSymbol.GetCustomAttributesToEmit(moduleBuilder)
        End Function

        Protected Overrides Function GetParameters() As ImmutableArray(Of EmbeddedParameter)
            Return EmbeddedTypesManager.EmbedParameters(Me, UnderlyingMethod.AdaptedMethodSymbol.Parameters)
        End Function

        Protected Overrides Function GetTypeParameters() As ImmutableArray(Of EmbeddedTypeParameter)
            Return UnderlyingMethod.AdaptedMethodSymbol.TypeParameters.SelectAsArray(map:=Function(typeParameter, container) New EmbeddedTypeParameter(container, typeParameter.GetCciAdapter()), arg:=Me)
        End Function

        Protected Overrides ReadOnly Property IsAbstract As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsMustOverride
            End Get
        End Property

        Protected Overrides ReadOnly Property IsAccessCheckedOnOverride As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsAccessCheckedOnOverride
            End Get
        End Property

        Protected Overrides ReadOnly Property IsConstructor As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.MethodKind = MethodKind.Constructor
            End Get
        End Property

        Protected Overrides ReadOnly Property IsExternal As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsExternal
            End Get
        End Property

        Protected Overrides ReadOnly Property IsHiddenBySignature As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsHiddenBySignature
            End Get
        End Property

        Protected Overrides ReadOnly Property IsNewSlot As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsMetadataNewSlot()
            End Get
        End Property

        Protected Overrides ReadOnly Property PlatformInvokeData As Cci.IPlatformInvokeInformation
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.GetDllImportData()
            End Get
        End Property

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSealed As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsMetadataFinal
            End Get
        End Property

        Protected Overrides ReadOnly Property IsStatic As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsShared
            End Get
        End Property

        Protected Overrides ReadOnly Property IsVirtual As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsMetadataVirtual()
            End Get
        End Property

        Protected Overrides Function GetImplementationAttributes(context As EmitContext) As Reflection.MethodImplAttributes
            Return UnderlyingMethod.AdaptedMethodSymbol.ImplementationAttributes
        End Function

        Protected Overrides ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.ReturnValueIsMarshalledExplicitly
            End Get
        End Property

        Protected Overrides ReadOnly Property ReturnValueMarshallingInformation As Cci.IMarshallingInformation
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.ReturnTypeMarshallingInformation
            End Get
        End Property

        Protected Overrides ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.ReturnValueMarshallingDescriptor
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.MetadataVisibility
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.MetadataName
            End Get
        End Property

        Protected Overrides ReadOnly Property AcceptsExtraArguments As Boolean
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.IsVararg
            End Get
        End Property

        Protected Overrides ReadOnly Property UnderlyingMethodSignature As Cci.ISignature
            Get
                Return DirectCast(UnderlyingMethod, Cci.ISignature)
            End Get
        End Property

        Protected Overrides ReadOnly Property ContainingNamespace As INamespace
            Get
                Return UnderlyingMethod.AdaptedMethodSymbol.ContainingNamespace.GetCciAdapter()
            End Get
        End Property
    End Class
End Namespace

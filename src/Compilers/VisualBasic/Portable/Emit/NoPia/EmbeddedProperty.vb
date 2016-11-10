' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedProperty
        Inherits EmbeddedTypesManager.CommonEmbeddedProperty

        Public Sub New(underlyingProperty As PropertySymbol, getter As EmbeddedMethod, setter As EmbeddedMethod)
            MyBase.New(underlyingProperty, getter, setter)
        End Sub

        Protected Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return UnderlyingProperty.GetCustomAttributesToEmit(compilationState)
        End Function

        Protected Overrides Function GetParameters() As ImmutableArray(Of EmbeddedParameter)
            Return EmbeddedTypesManager.EmbedParameters(Me, UnderlyingProperty.Parameters)
        End Function

        Protected Overrides ReadOnly Property IsRuntimeSpecial As Boolean
            Get
                Return UnderlyingProperty.HasRuntimeSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property IsSpecialName As Boolean
            Get
                Return UnderlyingProperty.HasSpecialName
            End Get
        End Property

        Protected Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
            Get
                Return UnderlyingProperty.CallingConvention
            End Get
        End Property

        Protected Overrides ReadOnly Property ReturnValueIsModified As Boolean
            Get
                Return UnderlyingProperty.TypeCustomModifiers.Length <> 0
            End Get
        End Property

        Protected Overrides ReadOnly Property ReturnValueCustomModifiers As ImmutableArray(Of Cci.ICustomModifier)
            Get
                Return UnderlyingProperty.TypeCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Protected Overrides ReadOnly Property ReturnValueIsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function [GetType](moduleBuilder As PEModuleBuilder, syntaxNodeOpt As SyntaxNode, diagnostics As DiagnosticBag) As Cci.ITypeReference
            Return moduleBuilder.Translate(UnderlyingProperty.Type, syntaxNodeOpt, diagnostics)
        End Function

        Protected Overrides ReadOnly Property ContainingType As EmbeddedType
            Get
                Return AnAccessor.ContainingType
            End Get
        End Property

        Protected Overrides ReadOnly Property Visibility As Cci.TypeMemberVisibility
            Get
                Return PEModuleBuilder.MemberVisibility(UnderlyingProperty)
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingProperty.MetadataName
            End Get
        End Property

    End Class

End Namespace

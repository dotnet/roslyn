' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class PropertySymbol
        Implements IPropertyDefinition

        Private ReadOnly Property IPropertyDefinitionAccessors As IEnumerable(Of IMethodReference) Implements IPropertyDefinition.Accessors
            Get
                CheckDefinitionInvariant()

                If Me.GetMethod IsNot Nothing And Me.SetMethod IsNot Nothing Then
                    Return {Me.GetMethod, Me.SetMethod}
                ElseIf Me.GetMethod IsNot Nothing Then
                    Return SpecializedCollections.SingletonEnumerable(Me.GetMethod)
                ElseIf Me.SetMethod IsNot Nothing Then
                    Return SpecializedCollections.SingletonEnumerable(Me.SetMethod)
                Else
                    Return SpecializedCollections.EmptyEnumerable(Of IMethodReference)()
                End If
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionDefaultValue As IMetadataConstant Implements IPropertyDefinition.DefaultValue
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionGetter As IMethodReference Implements IPropertyDefinition.Getter
            Get
                CheckDefinitionInvariant()
                Return Me.GetMethod
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionHasDefaultValue As Boolean Implements IPropertyDefinition.HasDefaultValue
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionIsRuntimeSpecial As Boolean Implements IPropertyDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return Me.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overridable ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionIsSpecialName As Boolean Implements IPropertyDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return Me.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionParameters As ImmutableArray(Of IParameterDefinition) Implements IPropertyDefinition.Parameters
            Get
                CheckDefinitionInvariant()
                Return StaticCast(Of IParameterDefinition).From(Me.Parameters)
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionSetter As IMethodReference Implements IPropertyDefinition.Setter
            Get
                CheckDefinitionInvariant()
                Return Me.SetMethod
            End Get
        End Property

        Private ReadOnly Property ISignatureCallingConvention As CallingConvention Implements ISignature.CallingConvention
            Get
                CheckDefinitionInvariant()
                Return Me.CallingConvention
            End Get
        End Property

        Private ReadOnly Property ISignatureParameterCount As UShort Implements ISignature.ParameterCount
            Get
                CheckDefinitionInvariant()
                Return CType(Me.ParameterCount, UShort)
            End Get
        End Property

        Private Function ISignatureGetParameters(context As EmitContext) As ImmutableArray(Of IParameterTypeInformation) Implements ISignature.GetParameters
            CheckDefinitionInvariant()
            Return StaticCast(Of IParameterTypeInformation).From(Me.Parameters)
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements ISignature.ReturnValueCustomModifiers
            Get
                CheckDefinitionInvariant()
                Return Me.TypeCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements ISignature.ReturnValueIsByRef
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property

        Private Function ISignatureGetType(context As EmitContext) As ITypeReference Implements ISignature.GetType
            CheckDefinitionInvariant()
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(Me.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return Me.ContainingType
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As TypeMemberVisibility Implements ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return PEModuleBuilder.MemberVisibility(Me)
            End Get
        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As ITypeReference Implements ITypeMemberReference.GetContainingType
            CheckDefinitionInvariant()
            Return Me.ContainingType
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            CheckDefinitionInvariant()
            visitor.Visit(DirectCast(Me, IPropertyDefinition))
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As IDefinition ' Implements IReference.AsDefinition
            CheckDefinitionInvariant()
            Return Me
        End Function

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                CheckDefinitionInvariant()
                Return Me.MetadataName
            End Get
        End Property
    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class ParameterSymbol
        Implements IParameterTypeInformation
        Implements IParameterDefinition

        Private ReadOnly Property IParameterTypeInformationCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements IParameterTypeInformation.CustomModifiers
            Get
                Return Me.CustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationIsByReference As Boolean Implements IParameterTypeInformation.IsByReference
            Get
                Return Me.IsByRef
            End Get
        End Property

        Private Function IParameterTypeInformationGetType(context As EmitContext) As ITypeReference Implements IParameterTypeInformation.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim paramType As TypeSymbol = Me.Type
            Return moduleBeingBuilt.Translate(paramType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IParameterTypeInformationCountOfCustomModifiersPrecedingByRef As UShort Implements IParameterTypeInformation.CountOfCustomModifiersPrecedingByRef
            Get
                Return Me.CountOfCustomModifiersPrecedingByRef
            End Get
        End Property

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements IParameterListEntry.Index
            Get
                Return CType(Me.Ordinal, UShort)
            End Get
        End Property

        Private Function IParameterDefinition_GetDefaultValue(context As EmitContext) As IMetadataConstant Implements IParameterDefinition.GetDefaultValue
            CheckDefinitionInvariant()
            Return Me.GetMetadataConstantValue(context)
        End Function

        Friend Function GetMetadataConstantValue(context As EmitContext) As IMetadataConstant
            If Me.HasMetadataConstantValue Then
                Return DirectCast(context.Module, PEModuleBuilder).CreateConstant(Me.Type, Me.ExplicitDefaultConstantValue.Value, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            Else
                Return Nothing
            End If
        End Function

        Friend Overridable ReadOnly Property HasMetadataConstantValue As Boolean
            Get
                CheckDefinitionInvariant()
                If Me.HasExplicitDefaultValue Then
                    Dim value = Me.ExplicitDefaultConstantValue
                    Return Not (value.Discriminator = ConstantValueTypeDiscriminator.DateTime OrElse value.Discriminator = ConstantValueTypeDiscriminator.Decimal)
                End If
                Return False
            End Get
        End Property

        Private ReadOnly Property IParameterDefinition_HasDefaultValue As Boolean Implements IParameterDefinition.HasDefaultValue
            Get
                CheckDefinitionInvariant()
                Return Me.HasMetadataConstantValue
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsOptional As Boolean Implements IParameterDefinition.IsOptional
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataOptional
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMetadataOptional As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.IsOptional OrElse GetAttributes().Any(Function(a) a.IsTargetAttribute(Me, AttributeDescription.OptionalAttribute))
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsIn As Boolean Implements IParameterDefinition.IsIn
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataIn
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsOut As Boolean Implements IParameterDefinition.IsOut
            Get
                CheckDefinitionInvariant()
                Return Me.IsMetadataOut
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsMarshalledExplicitly As Boolean Implements IParameterDefinition.IsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return Me.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return MarshallingInformation IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionMarshallingInformation As IMarshallingInformation Implements IParameterDefinition.MarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return MarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionMarshallingDescriptor As ImmutableArray(Of Byte) Implements IParameterDefinition.MarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return Me.MarshallingDescriptor
            End Get
        End Property

        Friend Overridable ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                visitor.Visit(DirectCast(Me, IParameterTypeInformation))
            Else
                If Me.ContainingModule = (DirectCast(visitor.Context.Module, PEModuleBuilder)).SourceModule Then
                    visitor.Visit(DirectCast(Me, IParameterDefinition))
                Else
                    visitor.Visit(DirectCast(Me, IParameterTypeInformation))
                End If
            End If
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As IDefinition ' Implements IReference.AsDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            If Me.IsDefinition AndAlso Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                Return Me.MetadataName
            End Get
        End Property
    End Class
End Namespace

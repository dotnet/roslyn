' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class ParameterSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class ParameterSymbol
#End If
        Implements IParameterTypeInformation
        Implements IParameterDefinition

        Private ReadOnly Property IDefinition_IsEncDeleted As Boolean Implements Cci.IDefinition.IsEncDeleted
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements IParameterTypeInformation.CustomModifiers
            Get
                Return AdaptedParameterSymbol.CustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements IParameterTypeInformation.RefCustomModifiers
            Get
                Return AdaptedParameterSymbol.RefCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property IParameterTypeInformationIsByReference As Boolean Implements IParameterTypeInformation.IsByReference
            Get
                Return AdaptedParameterSymbol.IsByRef
            End Get
        End Property

        Private Function IParameterTypeInformationGetType(context As EmitContext) As ITypeReference Implements IParameterTypeInformation.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim paramType As TypeSymbol = AdaptedParameterSymbol.Type
            Return moduleBeingBuilt.Translate(paramType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements IParameterListEntry.Index
            Get
                Return CType(AdaptedParameterSymbol.Ordinal, UShort)
            End Get
        End Property

        Private Function IParameterDefinition_GetDefaultValue(context As EmitContext) As MetadataConstant Implements IParameterDefinition.GetDefaultValue
            CheckDefinitionInvariant()
            Return Me.GetMetadataConstantValue(context)
        End Function

        Friend Function GetMetadataConstantValue(context As EmitContext) As MetadataConstant
            If AdaptedParameterSymbol.HasMetadataConstantValue Then
                Return DirectCast(context.Module, PEModuleBuilder).CreateConstant(AdaptedParameterSymbol.Type, AdaptedParameterSymbol.ExplicitDefaultConstantValue.Value, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            Else
                Return Nothing
            End If
        End Function

        Private ReadOnly Property IParameterDefinition_HasDefaultValue As Boolean Implements IParameterDefinition.HasDefaultValue
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.HasMetadataConstantValue
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsOptional As Boolean Implements IParameterDefinition.IsOptional
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.IsMetadataOptional
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsIn As Boolean Implements IParameterDefinition.IsIn
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.IsMetadataIn
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsOut As Boolean Implements IParameterDefinition.IsOut
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.IsMetadataOut
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionIsMarshalledExplicitly As Boolean Implements IParameterDefinition.IsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.IsMarshalledExplicitly
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionMarshallingInformation As IMarshallingInformation Implements IParameterDefinition.MarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.MarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IParameterDefinitionMarshallingDescriptor As ImmutableArray(Of Byte) Implements IParameterDefinition.MarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return AdaptedParameterSymbol.MarshallingDescriptor
            End Get
        End Property

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not AdaptedParameterSymbol.IsDefinition Then
                visitor.Visit(DirectCast(Me, IParameterTypeInformation))
            Else
                If AdaptedParameterSymbol.ContainingModule = (DirectCast(visitor.Context.Module, PEModuleBuilder)).SourceModule Then
                    visitor.Visit(DirectCast(Me, IParameterDefinition))
                Else
                    visitor.Visit(DirectCast(Me, IParameterTypeInformation))
                End If
            End If
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As IDefinition ' Implements IReference.AsDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            If AdaptedParameterSymbol.IsDefinition AndAlso AdaptedParameterSymbol.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                Return AdaptedParameterSymbol.MetadataName
            End Get
        End Property
    End Class

    Partial Friend Class ParameterSymbol
#If DEBUG Then
        Private _lazyAdapter As ParameterSymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As ParameterSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, New ParameterSymbolAdapter(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedParameterSymbol As ParameterSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As ParameterSymbol
            Return Me
        End Function
#End If

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

        Friend Overridable ReadOnly Property IsMetadataOptional As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.IsOptional OrElse GetAttributes().Any(Function(a) a.IsTargetAttribute(AttributeDescription.OptionalAttribute))
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return MarshallingInformation IsNot Nothing
            End Get
        End Property

        Friend Overridable ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property
    End Class

#If DEBUG Then
    Partial Friend NotInheritable Class ParameterSymbolAdapter
        Friend ReadOnly Property AdaptedParameterSymbol As ParameterSymbol

        Friend Sub New(underlyingParameterSymbol As ParameterSymbol)
            AdaptedParameterSymbol = underlyingParameterSymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedParameterSymbol
            End Get
        End Property
    End Class
#End If
End Namespace

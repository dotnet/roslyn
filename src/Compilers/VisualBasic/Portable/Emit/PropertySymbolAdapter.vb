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
    Partial Friend Class PropertySymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class PropertySymbol
#End If
        Implements IPropertyDefinition

        Private ReadOnly Property IDefinition_IsEncDeleted As Boolean Implements Cci.IDefinition.IsEncDeleted
            Get
                Return False
            End Get
        End Property

        Private Iterator Function IPropertyDefinitionAccessors(context As EmitContext) As IEnumerable(Of IMethodReference) Implements IPropertyDefinition.GetAccessors
            CheckDefinitionInvariant()

            Dim getter = AdaptedPropertySymbol.GetMethod?.GetCciAdapter()
            If getter IsNot Nothing AndAlso getter.ShouldInclude(context) Then
                Yield getter
            End If

            Dim setter = AdaptedPropertySymbol.SetMethod?.GetCciAdapter()
            If setter IsNot Nothing AndAlso setter.ShouldInclude(context) Then
                Yield setter
            End If
        End Function

        Private ReadOnly Property IPropertyDefinitionDefaultValue As MetadataConstant Implements IPropertyDefinition.DefaultValue
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionGetter As IMethodReference Implements IPropertyDefinition.Getter
            Get
                CheckDefinitionInvariant()
                Return AdaptedPropertySymbol.GetMethod?.GetCciAdapter()
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
                Return AdaptedPropertySymbol.HasRuntimeSpecialName
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionIsSpecialName As Boolean Implements IPropertyDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return AdaptedPropertySymbol.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionParameters As ImmutableArray(Of IParameterDefinition) Implements IPropertyDefinition.Parameters
            Get
                CheckDefinitionInvariant()

#If DEBUG Then
                Return AdaptedPropertySymbol.Parameters.SelectAsArray(Of IParameterDefinition)(Function(p) p.GetCciAdapter())
#Else
                Return StaticCast(Of IParameterDefinition).From(AdaptedPropertySymbol.Parameters)
#End If
            End Get
        End Property

        Private ReadOnly Property IPropertyDefinitionSetter As IMethodReference Implements IPropertyDefinition.Setter
            Get
                CheckDefinitionInvariant()
                Return AdaptedPropertySymbol.SetMethod?.GetCciAdapter()
            End Get
        End Property

        <Conditional("DEBUG")>
        Protected Friend Sub CheckDefinitionInvariantAllowEmbedded()
            ' can't be generic instantiation
            Debug.Assert(AdaptedPropertySymbol.IsDefinition)

            ' must be declared in the module we are building
            Debug.Assert(TypeOf AdaptedPropertySymbol.ContainingModule Is SourceModuleSymbol OrElse AdaptedPropertySymbol.ContainingAssembly.IsLinked)
        End Sub

        Private ReadOnly Property ISignatureCallingConvention As CallingConvention Implements ISignature.CallingConvention
            Get
                CheckDefinitionInvariantAllowEmbedded()
                Return AdaptedPropertySymbol.CallingConvention
            End Get
        End Property

        Private ReadOnly Property ISignatureParameterCount As UShort Implements ISignature.ParameterCount
            Get
                CheckDefinitionInvariant()
                Return CType(AdaptedPropertySymbol.ParameterCount, UShort)
            End Get
        End Property

        Private Function ISignatureGetParameters(context As EmitContext) As ImmutableArray(Of IParameterTypeInformation) Implements ISignature.GetParameters
            CheckDefinitionInvariant()
#If DEBUG Then
            Return AdaptedPropertySymbol.Parameters.SelectAsArray(Of IParameterTypeInformation)(Function(p) p.GetCciAdapter())
#Else
            Return StaticCast(Of IParameterTypeInformation).From(AdaptedPropertySymbol.Parameters)
#End If
        End Function

        Private ReadOnly Property ISignatureReturnValueCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements ISignature.ReturnValueCustomModifiers
            Get
                CheckDefinitionInvariantAllowEmbedded()
                Return AdaptedPropertySymbol.TypeCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements ISignature.RefCustomModifiers
            Get
                CheckDefinitionInvariantAllowEmbedded()
                Return AdaptedPropertySymbol.RefCustomModifiers.As(Of Cci.ICustomModifier)
            End Get
        End Property

        Private ReadOnly Property ISignatureReturnValueIsByRef As Boolean Implements ISignature.ReturnValueIsByRef
            Get
                CheckDefinitionInvariantAllowEmbedded()
                Return AdaptedPropertySymbol.ReturnsByRef
            End Get
        End Property

        Private Function ISignatureGetType(context As EmitContext) As ITypeReference Implements ISignature.GetType
            CheckDefinitionInvariantAllowEmbedded()
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(AdaptedPropertySymbol.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return AdaptedPropertySymbol.ContainingType.GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As TypeMemberVisibility Implements ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return AdaptedPropertySymbol.MetadataVisibility
            End Get
        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As ITypeReference Implements ITypeMemberReference.GetContainingType
            CheckDefinitionInvariant()
            Return AdaptedPropertySymbol.ContainingType.GetCciAdapter()
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
                Return AdaptedPropertySymbol.MetadataName
            End Get
        End Property
    End Class

    Partial Friend Class PropertySymbol
#If DEBUG Then
        Private _lazyAdapter As PropertySymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As PropertySymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, New PropertySymbolAdapter(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedPropertySymbol As PropertySymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As PropertySymbol
            Return Me
        End Function
#End If

        Friend Overridable ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                CheckDefinitionInvariant()
                Return False
            End Get
        End Property
    End Class

#If DEBUG Then
    Partial Friend NotInheritable Class PropertySymbolAdapter
        Friend ReadOnly Property AdaptedPropertySymbol As PropertySymbol

        Friend Sub New(underlyingPropertySymbol As PropertySymbol)
            AdaptedPropertySymbol = underlyingPropertySymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedPropertySymbol
            End Get
        End Property
    End Class
#End If
End Namespace

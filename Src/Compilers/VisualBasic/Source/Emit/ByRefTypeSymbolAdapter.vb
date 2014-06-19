Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Roslyn.Compilers.VisualBasic.Emit

Namespace Roslyn.Compilers.VisualBasic

    Partial Class ByRefTypeSymbol
        Implements IManagedPointerTypeReference

        Private Function IManagedPointerTypeReferenceGetTargetType(ByVal context As Object) As ITypeReference Implements IManagedPointerTypeReference.GetTargetType
            Return (DirectCast(context, [Module])).Translate(Me.ReferencedType)
        End Function

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements ITypeReference.IsEnum
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements ITypeReference.IsValueType
            Get
                Return False
            End Get
        End Property

        Private Function ITypeReferenceGetResolvedType(ByVal context As Object) As ITypeDefinition Implements ITypeReference.GetResolvedType
            Return Nothing
        End Function

        Private Function ITypeReferenceTypeCode(ByVal context As Object) As PrimitiveTypeCode Implements ITypeReference.TypeCode
            Return PrimitiveTypeCode.Reference
        End Function

        Private ReadOnly Property ITypeReferenceTypeDefRowId As UInteger Implements ITypeReference.TypeDefRowId
            Get
                Return 0
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericMethodParameterReference As IGenericMethodParameterReference Implements ITypeReference.AsGenericMethodParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeInstanceReference As IGenericTypeInstanceReference Implements ITypeReference.AsGenericTypeInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As IGenericTypeParameterReference Implements ITypeReference.AsGenericTypeParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(ByVal context As Object) As INamespaceTypeDefinition Implements ITypeReference.AsNamespaceTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNamespaceTypeReference As INamespaceTypeReference Implements ITypeReference.AsNamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNestedTypeDefinition(ByVal context As Object) As INestedTypeDefinition Implements ITypeReference.AsNestedTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNestedTypeReference As INestedTypeReference Implements ITypeReference.AsNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsSpecializedNestedTypeReference As ISpecializedNestedTypeReference Implements ITypeReference.AsSpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsTypeDefinition(ByVal context As Object) As ITypeDefinition Implements ITypeReference.AsTypeDefinition
            Return Nothing
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(ByVal visitor As IMetadataVisitor) ' Implements IReference.Dispatch
            visitor.Visit(DirectCast(Me, Microsoft.Cci.IManagedPointerTypeReference))
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(ByVal context As Object) As IDefinition ' Implements IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace

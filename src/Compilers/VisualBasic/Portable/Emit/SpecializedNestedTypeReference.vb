' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a type nested in an instantiation of a generic type.
    ''' e.g. 
    ''' A{int}.B
    ''' A.B{int}.C.D
    ''' </summary>
    Friend Class SpecializedNestedTypeReference
        Inherits NamedTypeReference
        Implements Cci.ISpecializedNestedTypeReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)
        End Sub

        Private Function ISpecializedNestedTypeReferenceGetUnspecializedVersion(context As EmitContext) As Cci.INestedTypeReference Implements Cci.ISpecializedNestedTypeReference.GetUnspecializedVersion
            Debug.Assert(m_UnderlyingNamedType.OriginalDefinition Is m_UnderlyingNamedType.OriginalDefinition.OriginalDefinition)
            Dim result = (DirectCast(context.Module, PEModuleBuilder)).Translate(m_UnderlyingNamedType.OriginalDefinition, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode),
                                                                                 diagnostics:=context.Diagnostics, needDeclaration:=True).AsNestedTypeReference
            Debug.Assert(result IsNot Nothing)
            Return result
        End Function

        Public Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.ISpecializedNestedTypeReference))
        End Sub

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(m_UnderlyingNamedType.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overrides ReadOnly Property AsGenericTypeInstanceReference As Cci.IGenericTypeInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AsNamespaceTypeReference As Cci.INamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AsNestedTypeReference As Cci.INestedTypeReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsSpecializedNestedTypeReference As Cci.ISpecializedNestedTypeReference
            Get
                Return Me
            End Get
        End Property
    End Class
End Namespace

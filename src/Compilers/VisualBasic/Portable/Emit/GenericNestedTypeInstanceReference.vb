' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a generic type instantiation that is nested in a non-generic type.
    ''' e.g. A.B{int}
    ''' </summary>
    Friend NotInheritable Class GenericNestedTypeInstanceReference
        Inherits GenericTypeInstanceReference
        Implements Cci.INestedTypeReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)
        End Sub

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(m_UnderlyingNamedType.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overrides ReadOnly Property AsGenericTypeInstanceReference As Cci.IGenericTypeInstanceReference
            Get
                Return Me
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
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property INestedTypeReferenceInheritsEnclosingTypeTypeParameters As Boolean Implements Cci.INestedTypeReference.InheritsEnclosingTypeTypeParameters
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace

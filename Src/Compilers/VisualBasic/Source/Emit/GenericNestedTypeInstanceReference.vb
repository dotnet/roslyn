' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a generic type instantiation that is nested in a non-generic type.
    ''' e.g. A.B{int}
    ''' </summary>
    Friend NotInheritable Class GenericNestedTypeInstanceReference
        Inherits GenericTypeInstanceReference
        Implements Microsoft.Cci.INestedTypeReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)
        End Sub

        Private Function ITypeMemberReferenceGetContainingType(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeReference Implements Microsoft.Cci.ITypeMemberReference.GetContainingType
            Return (DirectCast(context.Module, PEModuleBuilder)).Translate(m_UnderlyingNamedType.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overrides ReadOnly Property AsGenericTypeInstanceReference As Microsoft.Cci.IGenericTypeInstanceReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsNamespaceTypeReference As Microsoft.Cci.INamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AsNestedTypeReference As Microsoft.Cci.INestedTypeReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsSpecializedNestedTypeReference As Microsoft.Cci.ISpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

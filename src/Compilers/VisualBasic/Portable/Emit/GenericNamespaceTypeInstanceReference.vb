' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a generic type instantiation that is not nested.
    ''' e.g. MyNamespace.A{int}
    ''' </summary>
    Friend NotInheritable Class GenericNamespaceTypeInstanceReference
        Inherits GenericTypeInstanceReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)
        End Sub

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
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AsSpecializedNestedTypeReference As Microsoft.Cci.ISpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

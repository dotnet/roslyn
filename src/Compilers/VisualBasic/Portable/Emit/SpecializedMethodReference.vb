' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a method of a generic type instantiation.
    ''' e.g. 
    ''' A{int}.M()
    ''' A.B{int}.C.M()
    ''' </summary>
    Friend Class SpecializedMethodReference
        Inherits MethodReference
        Implements Microsoft.Cci.ISpecializedMethodReference

        Public Sub New(underlyingMethod As MethodSymbol)
            MyBase.New(underlyingMethod)
        End Sub

        Public Overrides Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Microsoft.Cci.ISpecializedMethodReference))
        End Sub

        Private ReadOnly Property ISpecializedMethodReferenceUnspecializedVersion As Microsoft.Cci.IMethodReference Implements Microsoft.Cci.ISpecializedMethodReference.UnspecializedVersion
            Get
                Return m_UnderlyingMethod.OriginalDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property AsSpecializedMethodReference As Microsoft.Cci.ISpecializedMethodReference
            Get
                Return Me
            End Get
        End Property
    End Class
End Namespace

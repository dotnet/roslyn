' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a type parameter that is based on another type parameter.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedTypeParameterSymbol
        Inherits TypeParameterSymbol

        Protected _underlyingTypeParameter As TypeParameterSymbol

        Public ReadOnly Property UnderlyingTypeParameter As TypeParameterSymbol
            Get
                Return Me._underlyingTypeParameter
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me._underlyingTypeParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return Me._underlyingTypeParameter.TypeParameterKind
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return Me._underlyingTypeParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return Me._underlyingTypeParameter.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return Me._underlyingTypeParameter.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return Me._underlyingTypeParameter.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property AllowsRefLikeType As Boolean
            Get
                Return Me._underlyingTypeParameter.AllowsRefLikeType
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return Me._underlyingTypeParameter.Variance
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._underlyingTypeParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me._underlyingTypeParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._underlyingTypeParameter.Name
            End Get
        End Property

        Public Sub New(underlyingTypeParameter As TypeParameterSymbol)
            Debug.Assert(underlyingTypeParameter IsNot Nothing)
            Me._underlyingTypeParameter = underlyingTypeParameter

            Debug.Assert(Me.TypeParameterKind = If(TypeOf Me.ContainingSymbol Is MethodSymbol, TypeParameterKind.Method,
                                                If(TypeOf Me.ContainingSymbol Is NamedTypeSymbol, TypeParameterKind.Type,
                                                TypeParameterKind.Cref)),
                $"Container is {Me.ContainingSymbol?.Kind}, TypeParameterKind is {Me.TypeParameterKind}")
        End Sub

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return Me._underlyingTypeParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            Me._underlyingTypeParameter.EnsureAllConstraintsAreResolved()
        End Sub
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Represents a type parameter in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    ''' another TypeParameterSymbol that is responsible for retargeting symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' </summary>
    Friend NotInheritable Class RetargetingTypeParameterSymbol
        Inherits SubstitutableTypeParameterSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying TypeParameterSymbol, cannot be another RetargetingTypeParameterSymbol.
        ''' </summary>
        Private ReadOnly _underlyingTypeParameter As TypeParameterSymbol

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingTypeParameter As TypeParameterSymbol)

            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingTypeParameter IsNot Nothing)

            If TypeOf underlyingTypeParameter Is RetargetingTypeParameterSymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingTypeParameter = underlyingTypeParameter
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingTypeParameter As TypeParameterSymbol
            Get
                Return _underlyingTypeParameter
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return _underlyingTypeParameter.TypeParameterKind
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _underlyingTypeParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _underlyingTypeParameter.Ordinal
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return RetargetingTranslator.Retarget(_underlyingTypeParameter.ConstraintTypesNoUseSiteDiagnostics)
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return _underlyingTypeParameter.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return _underlyingTypeParameter.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return _underlyingTypeParameter.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return _underlyingTypeParameter.Variance
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingTypeParameter.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingTypeParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingTypeParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return _underlyingTypeParameter.GetAttributes()
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _retargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingTypeParameter.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingTypeParameter.MetadataName
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            _underlyingTypeParameter.EnsureAllConstraintsAreResolved()
        End Sub

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingTypeParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

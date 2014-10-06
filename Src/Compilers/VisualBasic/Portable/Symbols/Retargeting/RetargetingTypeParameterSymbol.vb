' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Inherits TypeParameterSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly m_RetargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying TypeParameterSymbol, cannot be another RetargetingTypeParameterSymbol.
        ''' </summary>
        Private ReadOnly m_UnderlyingTypeParameter As TypeParameterSymbol

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingTypeParameter As TypeParameterSymbol)

            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingTypeParameter IsNot Nothing)

            If TypeOf underlyingTypeParameter Is RetargetingTypeParameterSymbol Then
                Throw New ArgumentException()
            End If

            m_RetargetingModule = retargetingModule
            m_UnderlyingTypeParameter = underlyingTypeParameter
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return m_RetargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingTypeParameter As TypeParameterSymbol
            Get
                Return m_UnderlyingTypeParameter
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return m_UnderlyingTypeParameter.TypeParameterKind
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return m_UnderlyingTypeParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_UnderlyingTypeParameter.Ordinal
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingTypeParameter.ConstraintTypesNoUseSiteDiagnostics)
            End Get
        End Property

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return m_UnderlyingTypeParameter.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return m_UnderlyingTypeParameter.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return m_UnderlyingTypeParameter.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return m_UnderlyingTypeParameter.Variance
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingTypeParameter.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_UnderlyingTypeParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_UnderlyingTypeParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return m_UnderlyingTypeParameter.GetAttributes()
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_RetargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_UnderlyingTypeParameter.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return m_UnderlyingTypeParameter.MetadataName
            End Get
        End Property

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            m_UnderlyingTypeParameter.EnsureAllConstraintsAreResolved()
        End Sub

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VBCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return m_UnderlyingTypeParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
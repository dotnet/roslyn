' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a parameter that is based on another parameter.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedParameterSymbol
        Inherits ParameterSymbol

        Protected _underlyingParameter As ParameterSymbol

        Public ReadOnly Property UnderlyingParameter As ParameterSymbol
            Get
                Return Me._underlyingParameter
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._underlyingParameter.Type
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return Me._underlyingParameter.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return Me._underlyingParameter.IsMetadataIn
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return Me._underlyingParameter.IsMetadataOut
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._underlyingParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me._underlyingParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return Me._underlyingParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return Me._underlyingParameter.IsParamArray
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOptional As Boolean
            Get
                Return Me._underlyingParameter.IsMetadataOptional
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me._underlyingParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._underlyingParameter.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return Me._underlyingParameter.MetadataName
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingParameter.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._underlyingParameter.RefCustomModifiers
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me._underlyingParameter.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingType As UnmanagedType
            Get
                Return Me._underlyingParameter.MarshallingType
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return Me._underlyingParameter.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return Me._underlyingParameter.IsIUnknownConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return Me._underlyingParameter.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return Me._underlyingParameter.IsOptional
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return Me._underlyingParameter.HasExplicitDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return Me._underlyingParameter.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return Me._underlyingParameter.HasOptionCompare
            End Get
        End Property

        Protected Sub New(underlyingParameter As ParameterSymbol)
            Debug.Assert(underlyingParameter IsNot Nothing)
            Me._underlyingParameter = underlyingParameter
        End Sub

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me._underlyingParameter.GetAttributes()
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            Me._underlyingParameter.AddSynthesizedAttributes(moduleBuilder, attributes)
        End Sub

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return Me._underlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

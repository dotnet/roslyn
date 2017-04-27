' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading

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


        Public ReadOnly Property UnderlyingParameter As ParameterSymbol

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return UnderlyingParameter.Type
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return UnderlyingParameter.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return UnderlyingParameter.IsMetadataIn
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return UnderlyingParameter.IsMetadataOut
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return UnderlyingParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return UnderlyingParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return UnderlyingParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return UnderlyingParameter.IsParamArray
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOptional As Boolean
            Get
                Return UnderlyingParameter.IsMetadataOptional
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return UnderlyingParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingParameter.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return UnderlyingParameter.MetadataName
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingParameter.CustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return UnderlyingParameter.RefCustomModifiers
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return UnderlyingParameter.MarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingType As UnmanagedType
            Get
                Return UnderlyingParameter.MarshallingType
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return UnderlyingParameter.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return UnderlyingParameter.IsIUnknownConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return UnderlyingParameter.IsCallerLineNumber
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return UnderlyingParameter.IsCallerFilePath
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return UnderlyingParameter.IsCallerMemberName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return UnderlyingParameter.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return UnderlyingParameter.IsOptional
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return UnderlyingParameter.HasExplicitDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return UnderlyingParameter.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return UnderlyingParameter.HasOptionCompare
            End Get
        End Property

        Protected Sub New(underlyingParameter As ParameterSymbol)
            Debug.Assert(underlyingParameter IsNot Nothing)
            Me.UnderlyingParameter = underlyingParameter
        End Sub

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return UnderlyingParameter.GetAttributes()
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            UnderlyingParameter.AddSynthesizedAttributes(compilationState, attributes)
        End Sub

        Public Overrides Function GetDocumentationCommentXml(
                                                     Optional preferredCulture As CultureInfo = Nothing,
                                                     Optional expandIncludes As Boolean = False,
                                                     Optional cancellationToken As CancellationToken = Nothing
                                                            ) As String
            Return UnderlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

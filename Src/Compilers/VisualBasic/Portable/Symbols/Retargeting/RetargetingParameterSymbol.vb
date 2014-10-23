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
    ''' Represents a parameter of a RetargetingMethodSymbol. Essentially this is a wrapper around 
    ''' another ParameterSymbol that is responsible for retargeting symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' </summary>
    Friend MustInherit Class RetargetingParameterSymbol
        Inherits ParameterSymbol

        ''' <summary>
        ''' The underlying ParameterSymbol, cannot be another RetargetingParameterSymbol.
        ''' </summary>
        Private ReadOnly m_UnderlyingParameter As ParameterSymbol

        Private m_LazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Public Shared Function CreateMethodParameter(retargetingMethod As RetargetingMethodSymbol, underlyingParameter As ParameterSymbol) As RetargetingParameterSymbol
            Return New RetargetingMethodParameterSymbol(retargetingMethod, underlyingParameter)
        End Function

        Public Shared Function CreatePropertyParameter(retargetingProperty As RetargetingPropertySymbol, underlyingParameter As ParameterSymbol) As RetargetingParameterSymbol
            Return New RetargetingPropertyParameterSymbol(retargetingProperty, underlyingParameter)
        End Function

        Protected Sub New(underlyingParameter As ParameterSymbol)

            Debug.Assert(underlyingParameter IsNot Nothing)

            If TypeOf underlyingParameter Is RetargetingParameterSymbol Then
                Throw New ArgumentException()
            End If

            m_UnderlyingParameter = underlyingParameter
        End Sub

        Public ReadOnly Property UnderlyingParameter As ParameterSymbol
            Get
                Return m_UnderlyingParameter
            End Get
        End Property

        Protected MustOverride ReadOnly Property RetargetingModule As RetargetingModuleSymbol

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return RetargetingModule.RetargetingTranslator
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return m_UnderlyingParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingParameter.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(m_UnderlyingParameter.CustomModifiers, m_LazyCustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return m_UnderlyingParameter.IsParamArray
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return m_UnderlyingParameter.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return m_UnderlyingParameter.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return m_UnderlyingParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return m_UnderlyingParameter.IsOptional
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return m_UnderlyingParameter.IsMetadataOut
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return m_UnderlyingParameter.IsMetadataIn
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return m_UnderlyingParameter.HasExplicitDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return m_UnderlyingParameter.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.RetargetingTranslator.Retarget(Me.UnderlyingParameter.MarshallingInformation)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return m_UnderlyingParameter.HasOptionCompare
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return m_UnderlyingParameter.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return m_UnderlyingParameter.IsIUnknownConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return m_UnderlyingParameter.IsCallerLineNumber
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return m_UnderlyingParameter.IsCallerMemberName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return m_UnderlyingParameter.IsCallerFilePath
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasByRefBeforeCustomModifiers As Boolean
            Get
                Return m_UnderlyingParameter.HasByRefBeforeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingParameter.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_UnderlyingParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_UnderlyingParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(m_UnderlyingParameter, m_LazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(m_UnderlyingParameter.GetCustomAttributesToEmit(compilationState))
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return RetargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_UnderlyingParameter.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return m_UnderlyingParameter.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasMetadataConstantValue As Boolean
            Get
                Return m_UnderlyingParameter.HasMetadataConstantValue
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOptional As Boolean
            Get
                Return m_UnderlyingParameter.IsMetadataOptional
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return m_UnderlyingParameter.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return m_UnderlyingParameter.MarshallingDescriptor
            End Get
        End Property

        Private NotInheritable Class RetargetingMethodParameterSymbol
            Inherits RetargetingParameterSymbol

            ''' <summary>
            ''' Owning RetargetingMethodSymbol.
            ''' </summary>
            Private ReadOnly m_RetargetingMethod As RetargetingMethodSymbol

            Public Sub New(retargetingMethod As RetargetingMethodSymbol, underlyingParameter As ParameterSymbol)
                MyBase.New(underlyingParameter)

                Debug.Assert(retargetingMethod IsNot Nothing)
                m_RetargetingMethod = retargetingMethod
            End Sub

            Protected Overrides ReadOnly Property RetargetingModule As RetargetingModuleSymbol
                Get
                    Return m_RetargetingMethod.RetargetingModule
                End Get
            End Property
        End Class

        Private NotInheritable Class RetargetingPropertyParameterSymbol
            Inherits RetargetingParameterSymbol

            ''' <summary>
            ''' Owning RetargetingPropertySymbol.
            ''' </summary>
            Private ReadOnly m_RetargetingProperty As RetargetingPropertySymbol

            Public Sub New(retargetingProperty As RetargetingPropertySymbol, underlyingParameter As ParameterSymbol)
                MyBase.New(underlyingParameter)

                Debug.Assert(retargetingProperty IsNot Nothing)
                m_RetargetingProperty = retargetingProperty
            End Sub

            Protected Overrides ReadOnly Property RetargetingModule As RetargetingModuleSymbol
                Get
                    Return m_RetargetingProperty.RetargetingModule
                End Get
            End Property
        End Class

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend NotOverridable Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return m_UnderlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
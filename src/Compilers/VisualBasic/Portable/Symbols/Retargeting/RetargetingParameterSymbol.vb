' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

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
        Private ReadOnly _underlyingParameter As ParameterSymbol

        Private _lazyCustomModifiers As CustomModifiersTuple

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

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

            _underlyingParameter = underlyingParameter
        End Sub

        Public ReadOnly Property UnderlyingParameter As ParameterSymbol
            Get
                Return _underlyingParameter
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
                Return _underlyingParameter.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingParameter.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return CustomModifiersTuple.TypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return CustomModifiersTuple.RefCustomModifiers
            End Get
        End Property

        Private ReadOnly Property CustomModifiersTuple As CustomModifiersTuple
            Get
                Return RetargetingTranslator.RetargetModifiers(_underlyingParameter.CustomModifiers, _underlyingParameter.RefCustomModifiers, _lazyCustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return _underlyingParameter.IsParamArray
            End Get
        End Property

        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _underlyingParameter.IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return _underlyingParameter.IsExplicitByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _underlyingParameter.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return _underlyingParameter.IsOptional
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return _underlyingParameter.IsMetadataOut
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return _underlyingParameter.IsMetadataIn
            End Get
        End Property

        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return _underlyingParameter.HasExplicitDefaultValue
            End Get
        End Property

        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return _underlyingParameter.ExplicitDefaultConstantValue(inProgress)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.RetargetingTranslator.Retarget(Me.UnderlyingParameter.MarshallingInformation)
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return _underlyingParameter.HasOptionCompare
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return _underlyingParameter.IsIDispatchConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return _underlyingParameter.IsIUnknownConstant
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return _underlyingParameter.IsCallerLineNumber
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return _underlyingParameter.IsCallerMemberName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return _underlyingParameter.IsCallerFilePath
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                Return _underlyingParameter.CallerArgumentExpressionParameterIndex
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingParameter.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingParameter.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingParameter.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingParameter, _lazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(_underlyingParameter.GetCustomAttributesToEmit(moduleBuilder))
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
                Return _underlyingParameter.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingParameter.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasMetadataConstantValue As Boolean
            Get
                Return _underlyingParameter.HasMetadataConstantValue
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOptional As Boolean
            Get
                Return _underlyingParameter.IsMetadataOptional
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return _underlyingParameter.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return _underlyingParameter.MarshallingDescriptor
            End Get
        End Property

        Private NotInheritable Class RetargetingMethodParameterSymbol
            Inherits RetargetingParameterSymbol

            ''' <summary>
            ''' Owning RetargetingMethodSymbol.
            ''' </summary>
            Private ReadOnly _retargetingMethod As RetargetingMethodSymbol

            Public Sub New(retargetingMethod As RetargetingMethodSymbol, underlyingParameter As ParameterSymbol)
                MyBase.New(underlyingParameter)

                Debug.Assert(retargetingMethod IsNot Nothing)
                _retargetingMethod = retargetingMethod
            End Sub

            Protected Overrides ReadOnly Property RetargetingModule As RetargetingModuleSymbol
                Get
                    Return _retargetingMethod.RetargetingModule
                End Get
            End Property
        End Class

        Private NotInheritable Class RetargetingPropertyParameterSymbol
            Inherits RetargetingParameterSymbol

            ''' <summary>
            ''' Owning RetargetingPropertySymbol.
            ''' </summary>
            Private ReadOnly _retargetingProperty As RetargetingPropertySymbol

            Public Sub New(retargetingProperty As RetargetingPropertySymbol, underlyingParameter As ParameterSymbol)
                MyBase.New(underlyingParameter)

                Debug.Assert(retargetingProperty IsNot Nothing)
                _retargetingProperty = retargetingProperty
            End Sub

            Protected Overrides ReadOnly Property RetargetingModule As RetargetingModuleSymbol
                Get
                    Return _retargetingProperty.RetargetingModule
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
            Return _underlyingParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

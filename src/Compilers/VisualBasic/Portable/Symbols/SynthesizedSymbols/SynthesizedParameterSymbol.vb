' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Base (simplified) class for synthesized parameter symbols of methods that have been synthesized. E.g. the parameters of delegate methods
    ''' </summary>
    Friend Class SynthesizedParameterSimpleSymbol
        Inherits ParameterSymbol

        Protected ReadOnly _container As MethodSymbol
        Protected ReadOnly _type As TypeSymbol
        Protected ReadOnly _ordinal As Integer
        Protected ReadOnly _name As String

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedParameterSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing symbol</param>
        ''' <param name="type">The type of this parameter</param>
        ''' <param name="ordinal">The ordinal number of this parameter</param>
        ''' <param name="name">The name of this parameter</param>
        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, name As String)
            Me._container = container
            Me._type = type
            Me._ordinal = ordinal
            Me._name = name
        End Sub

        ''' <summary>
        ''' Gets the containing symbol.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._container
            End Get
        End Property

        ''' <summary>
        ''' The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
        ''' </summary>
        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        ''' <summary>
        ''' A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
        ''' </summary>
        Friend Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
        ''' </summary>
        Public Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is by ref.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is by ref; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataIn As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataOut As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Shared Function IsMarshalAsAttributeApplicable(propertySetter As MethodSymbol) As Boolean
            Debug.Assert(propertySetter.MethodKind = MethodKind.PropertySet)

            Return propertySetter.ContainingType.IsInterface
        End Function

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                ' Dev11 uses marshalling data of a return type of the containing method for the Value parameter 
                ' of an interface property setter.
                Dim method = DirectCast(Me.ContainingSymbol, MethodSymbol)
                If method.MethodKind = MethodKind.PropertySet AndAlso
                   IsMarshalAsAttributeApplicable(method) Then

                    Return DirectCast(method.AssociatedSymbol, SourcePropertySymbol).ReturnTypeMarshallingInformation
                End If

                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExplicitByRef As Boolean
            Get
                Return IsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property HasOptionCompare As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIDispatchConstant As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsIUnknownConstant As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerLineNumber As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerMemberName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsCallerFilePath As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property CallerArgumentExpressionParameterIndex As Integer
            Get
                Return -1
            End Get
        End Property

        ''' <summary>
        ''' True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
        ''' </summary>
        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is param array.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is param array; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether the symbol was generated by the compiler
        ''' rather than declared explicitly.
        ''' </summary>
        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' A potentially empty collection of locations that correspond to this instance.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets the ordinal.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        ''' <summary>
        ''' Gets the type.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        ''' <summary>
        ''' Gets the name.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend NotOverridable Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            Dim compilation = Me.DeclaringCompilation
            If Type.ContainsTupleNames() AndAlso
                compilation.HasTupleNamesAttributes Then
                AddSynthesizedAttribute(attributes, compilation.SynthesizeTupleNamesAttribute(Type))
            End If
        End Sub
    End Class

    ''' <summary>
    ''' Base class for synthesized parameter symbols of methods that have been synthesized. E.g. the parameters of delegate methods
    ''' </summary>
    Friend Class SynthesizedParameterSymbol
        Inherits SynthesizedParameterSimpleSymbol

        Private ReadOnly _isByRef As Boolean
        Private ReadOnly _isOptional As Boolean
        Private ReadOnly _defaultValue As ConstantValue

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedParameterSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing symbol</param>
        ''' <param name="type">The type of this parameter</param>
        ''' <param name="ordinal">The ordinal number of this parameter</param>
        ''' <param name="isByRef">Whether the parameter is ByRef or not</param>
        ''' <param name="name">The name of this parameter</param>
        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, Optional name As String = "")
            Me.New(container, type, ordinal, isByRef, name, False, Nothing)
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedParameterSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing symbol</param>
        ''' <param name="type">The type of this parameter</param>
        ''' <param name="ordinal">The ordinal number of this parameter</param>
        ''' <param name="isByRef">Whether the parameter is ByRef or not</param>
        ''' <param name="name">The name of this parameter</param>
        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, name As String, isOptional As Boolean, defaultValue As ConstantValue)
            MyBase.New(container, type, ordinal, name)

            Me._isByRef = isByRef
            Me._isOptional = isOptional
            Me._defaultValue = defaultValue
        End Sub

        Public Shared Function Create(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, name As String,
                       customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier)
        ) As SynthesizedParameterSymbol

            If customModifiers.IsEmpty AndAlso refCustomModifiers.IsEmpty Then
                Return New SynthesizedParameterSymbol(container, type, ordinal, isByRef, name, isOptional:=False, defaultValue:=Nothing)
            End If

            Return New SynthesizedParameterSymbolWithCustomModifiers(container, type, ordinal, isByRef, name, customModifiers, refCustomModifiers)
        End Function

        Friend Shared Function CreateSetAccessorValueParameter(setter As MethodSymbol, propertySymbol As PropertySymbol, parameterName As String) As ParameterSymbol
            Dim valueParameterType As TypeSymbol = propertySymbol.Type
            Dim valueParameterCustomModifiers = propertySymbol.TypeCustomModifiers

            Dim overriddenMethod = setter.OverriddenMethod
            If overriddenMethod IsNot Nothing Then
                Dim overriddenParameter = overriddenMethod.Parameters(propertySymbol.ParameterCount)

                If overriddenParameter.Type.IsSameTypeIgnoringAll(valueParameterType) Then
                    valueParameterType = overriddenParameter.Type
                    valueParameterCustomModifiers = overriddenParameter.CustomModifiers
                End If
            End If

            If valueParameterCustomModifiers.IsEmpty Then
                Return New SynthesizedParameterSimpleSymbol(setter,
                                                            valueParameterType,
                                                            propertySymbol.ParameterCount,
                                                            parameterName)
            End If

            Return New SynthesizedParameterSymbolWithCustomModifiers(setter,
                                                                     valueParameterType,
                                                                     propertySymbol.ParameterCount,
                                                                     False,
                                                                     parameterName,
                                                                     valueParameterCustomModifiers,
                                                                     ImmutableArray(Of CustomModifier).Empty) ' Never ByRef
        End Function

        ''' <summary>
        ''' Gets a value indicating whether this instance is by ref.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is by ref; otherwise, <c>false</c>.
        ''' </value>
        Public NotOverridable Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _isByRef
            End Get
        End Property

        ''' <summary>
        ''' True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return _isOptional
            End Get
        End Property

        ''' <summary>
        ''' A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property ExplicitDefaultConstantValue(inProgress As SymbolsInProgress(Of ParameterSymbol)) As ConstantValue
            Get
                Return _defaultValue
            End Get
        End Property

        ''' <summary>
        ''' True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property HasExplicitDefaultValue As Boolean
            Get
                Return _defaultValue IsNot Nothing
            End Get
        End Property

    End Class

    Friend Class SynthesizedParameterSymbolWithCustomModifiers
        Inherits SynthesizedParameterSymbol

        Private ReadOnly _customModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _refCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedParameterSymbolWithCustomModifiers" /> class.
        ''' </summary>
        ''' <param name="container">The containing symbol</param>
        ''' <param name="type">The type of this parameter</param>
        ''' <param name="ordinal">The ordinal number of this parameter</param>
        ''' <param name="isByRef">Whether the parameter is ByRef or not</param>
        ''' <param name="name">The name of this parameter</param>
        ''' <param name="customModifiers">The custom modifiers of this parameter type</param>
        ''' <param name="refCustomModifiers">The custom modifiers of ref modifier</param>
        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, name As String,
                       customModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier))
            MyBase.New(container, type, ordinal, isByRef, name, isOptional:=False, defaultValue:=Nothing)

            Debug.Assert(Not customModifiers.IsEmpty OrElse Not refCustomModifiers.IsEmpty)
            Me._customModifiers = customModifiers
            Me._refCustomModifiers = refCustomModifiers

            Debug.Assert(Me._refCustomModifiers.IsEmpty OrElse Me.IsByRef)
        End Sub

        ''' <summary>
        ''' The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._customModifiers
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return Me._refCustomModifiers
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Extends SynthesizedParameterSymbol to also accept a location
    ''' </summary>
    Friend NotInheritable Class SynthesizedParameterWithLocationSymbol
        Inherits SynthesizedParameterSymbol

        Private ReadOnly _locations As ImmutableArray(Of Location)

        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, name As String, location As Location)
            MyBase.New(container, type, ordinal, isByRef, name)
            Me._locations = ImmutableArray.Create(Of location)(location)
        End Sub

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me._locations
            End Get
        End Property

    End Class

End Namespace

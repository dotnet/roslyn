' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all properties imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PEPropertySymbol
        Inherits PropertySymbol

        Private ReadOnly m_name As String
        Private ReadOnly m_flags As PropertyAttributes
        Private ReadOnly m_containingType As PENamedTypeSymbol
        Private ReadOnly m_signatureHeader As SignatureHeader
        Private ReadOnly m_parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly m_propertyType As TypeSymbol
        Private ReadOnly m_getMethod As PEMethodSymbol
        Private ReadOnly m_setMethod As PEMethodSymbol
        Private ReadOnly m_handle As PropertyDefinitionHandle
        Private ReadOnly m_typeCustomModifiers As ImmutableArray(Of CustomModifier)
        Private m_lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private m_lazyDocComment As Tuple(Of CultureInfo, String)
        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        ' mutable because we only know this part after the property is constructed.
        ' Integer because we want to use CMPXCHG on it
        Private m_isWithEvents As Integer = ThreeState.Unknown

        ' Distinct accessibility value to represent unset.
        Private Const UnsetAccessibility As Integer = -1
        Private m_lazyDeclaredAccessibility As Integer = UnsetAccessibility
        Private m_lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        Friend Sub New(
                      moduleSymbol As PEModuleSymbol,
                      containingType As PENamedTypeSymbol,
                      handle As PropertyDefinitionHandle,
                      getMethod As PEMethodSymbol,
                      setMethod As PEMethodSymbol)

            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(Not handle.IsNil)
            Debug.Assert((getMethod IsNot Nothing) OrElse (setMethod IsNot Nothing))

            m_containingType = containingType
            m_handle = handle
            Dim [module] = moduleSymbol.Module
            Dim mrEx As BadImageFormatException = Nothing

            Try
                [module].GetPropertyDefPropsOrThrow(handle, m_name, m_flags)
            Catch mrEx
                If m_name Is Nothing Then
                    m_name = String.Empty
                End If
            End Try

            m_getMethod = getMethod
            m_setMethod = setMethod

            Dim metadataDecoder = New MetadataDecoder(moduleSymbol, containingType)
            Dim propEx As BadImageFormatException = Nothing
            Dim propertyParams = MetadataDecoder.GetSignatureForProperty(handle, m_signatureHeader, propEx)
            Debug.Assert(propertyParams.Length > 0)

            Dim unusedSignatureHeader As SignatureHeader = Nothing
            Dim getEx As BadImageFormatException = Nothing
            Dim getParams = If(m_getMethod Is Nothing, Nothing, MetadataDecoder.GetSignatureForMethod(m_getMethod.Handle, unusedSignatureHeader, getEx))
            Dim setEx As BadImageFormatException = Nothing
            Dim setParams = If(m_setMethod Is Nothing, Nothing, MetadataDecoder.GetSignatureForMethod(m_setMethod.Handle, unusedSignatureHeader, setEx))

            Dim signaturesMatch = DoSignaturesMatch(metadataDecoder, propertyParams, m_getMethod, getParams, m_setMethod, setParams)
            Dim parametersMatch = True
            m_parameters = GetParameters(Me, m_getMethod, m_setMethod, propertyParams, parametersMatch)

            If Not signaturesMatch OrElse Not parametersMatch OrElse
               propEx IsNot Nothing OrElse getEx IsNot Nothing OrElse setEx IsNot Nothing OrElse mrEx IsNot Nothing Then
                m_lazyUseSiteErrorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedProperty1, CustomSymbolDisplayFormatter.QualifiedName(Me))
            End If

            If m_getMethod IsNot Nothing Then
                m_getMethod.SetAssociatedProperty(Me, MethodKind.PropertyGet)
            End If

            If m_setMethod IsNot Nothing Then
                m_setMethod.SetAssociatedProperty(Me, MethodKind.PropertySet)
            End If

            m_propertyType = propertyParams(0).Type
            m_typeCustomModifiers = VisualBasicCustomModifier.Convert(propertyParams(0).CustomModifiers)
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Friend ReadOnly Property PropertyFlags As PropertyAttributes
            Get
                Return m_flags
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (m_flags And PropertyAttributes.SpecialName) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                If Me.m_lazyDeclaredAccessibility = UnsetAccessibility Then
                    Interlocked.CompareExchange(Me.m_lazyDeclaredAccessibility, GetDeclaredAccessibility(Me), UnsetAccessibility)
                End If

                Return DirectCast(Me.m_lazyDeclaredAccessibility, Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Dim method = Me.GetterOrSetter
                Return If((method IsNot Nothing), method.IsMustOverride, False)
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Dim method = Me.GetterOrSetter
                Return If((method IsNot Nothing), method.IsNotOverridable, False)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Dim method = Me.GetterOrSetter
                Return If((method IsNot Nothing), method.IsOverridable, False)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Dim method = Me.GetterOrSetter
                Return If((method IsNot Nothing), method.IsOverrides, False)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Dim method = Me.GetterOrSetter
                Return If((method IsNot Nothing), method.IsOverloads, False)
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Dim method = Me.GetterOrSetter
                Return If((method IsNot Nothing), method.IsShared, True)
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Dim defaultPropertyName = m_containingType.DefaultPropertyName
                Return (Not String.IsNullOrEmpty(defaultPropertyName)) AndAlso
                    IdentifierComparison.Equals(defaultPropertyName, m_name)
            End Get
        End Property

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                'NOTE: If noone set the IsWithEvents, getting the value will permanently set it to Unknown.
                If Me.m_isWithEvents = ThreeState.Unknown Then
                    SetIsWithEvents(MyBase.IsWithEvents)
                End If

                Debug.Assert(Me.m_isWithEvents = ThreeState.True OrElse Me.m_isWithEvents = ThreeState.False)
                Return Me.m_isWithEvents = ThreeState.True
            End Get
        End Property

        ''' <summary>
        ''' Marks property as definitely IsWithEvents or not.
        ''' The effects of this change cannot be undone.
        ''' Will have no effect if someone already asked if property is IsWithEvents (and will assert since it is not supposed to happen).
        ''' </summary>
        Friend Sub SetIsWithEvents(value As Boolean)
            Dim newValue = If(value, ThreeState.True, ThreeState.False)
            Dim origValue = Threading.Interlocked.CompareExchange(Me.m_isWithEvents, newValue, ThreeState.Unknown)
            Debug.Assert(origValue = ThreeState.Unknown OrElse origValue = newValue, "Tried changing already known IsWithEvent value.")
        End Sub

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return m_parameters
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return m_propertyType
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return m_typeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return m_getMethod
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return m_setMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As CallingConvention
            Get
                Return CType(m_signatureHeader.RawValue, CallingConvention)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_containingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(m_lazyObsoleteAttributeData, m_handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return m_lazyObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If m_lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(m_handle, m_lazyCustomAttributes)
            End If
            Return m_lazyCustomAttributes
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return GetAttributes()
        End Function

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                If (Me.m_getMethod Is Nothing OrElse Me.m_getMethod.ExplicitInterfaceImplementations.Length = 0) AndAlso (Me.m_setMethod Is Nothing OrElse Me.m_setMethod.ExplicitInterfaceImplementations.Length = 0) Then
                    Return ImmutableArray(Of PropertySymbol).Empty
                End If

                Dim propertiesWithImplementedGetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(Me.m_getMethod)
                Dim propertiesWithImplementedSetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(Me.m_setMethod)
                Dim builder = ArrayBuilder(Of PropertySymbol).GetInstance()
                For Each prop In propertiesWithImplementedGetters
                    If prop.SetMethod Is Nothing OrElse propertiesWithImplementedSetters.Contains(prop) Then
                        builder.Add(prop)
                    End If
                Next

                For Each prop In propertiesWithImplementedSetters
                    If prop.GetMethod Is Nothing Then
                        builder.Add(prop)
                    End If
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Private ReadOnly Property GetterOrSetter As MethodSymbol
            Get
                Return If(Me.m_getMethod, Me.m_setMethod)
            End Get
        End Property

        ''' <summary>
        ''' For the property to be considered valid, accessor signatures must be consistent
        ''' with each other, and accessor signatures must be consistent with the property
        ''' signature ignoring ByRef. These requirements are stricter than Dev11 which
        ''' allows many inconsistencies including different numbers of parameters.
        ''' </summary>
        Private Shared Function DoSignaturesMatch(
                                                 metadataDecoder As MetadataDecoder,
                                                 propertyParams As MetadataDecoder.ParamInfo(),
                                                 getMethodOpt As PEMethodSymbol,
                                                 getMethodParamsOpt As MetadataDecoder.ParamInfo(),
                                                 setMethodOpt As PEMethodSymbol,
                                                 setMethodParamsOpt As MetadataDecoder.ParamInfo()) As Boolean
            ' Compare getter or setter with property.
            If getMethodOpt IsNot Nothing Then
                If Not metadataDecoder.DoPropertySignaturesMatch(propertyParams, getMethodParamsOpt, comparingToSetter:=False, compareParamByRef:=False, compareReturnType:=False) Then
                    Return False
                End If
            Else
                If Not metadataDecoder.DoPropertySignaturesMatch(propertyParams, setMethodParamsOpt, comparingToSetter:=True, compareParamByRef:=False, compareReturnType:=False) Then
                    Return False
                End If
            End If

            If (getMethodOpt Is Nothing) OrElse (setMethodOpt Is Nothing) Then
                Return True
            End If

            ' Compare getter with setter.
            If Not metadataDecoder.DoPropertySignaturesMatch(getMethodParamsOpt, setMethodParamsOpt, comparingToSetter:=True, compareParamByRef:=True, compareReturnType:=False) Then
                Return False
            End If

            If (getMethodOpt.IsMustOverride <> setMethodOpt.IsMustOverride) OrElse
                (getMethodOpt.IsNotOverridable <> setMethodOpt.IsNotOverridable) OrElse
                (getMethodOpt.IsOverrides <> setMethodOpt.IsOverrides) OrElse
                (getMethodOpt.IsShared <> setMethodOpt.IsShared) Then
                Return False
            End If

            Return True
        End Function

        ' Properties from metadata do not have an explicit accessibility. Instead,
        ' the accessibility reported for the PEPropertySymbol is the most
        ' restrictive level that is no more restrictive than the getter and setter.
        Private Shared Function GetDeclaredAccessibility([property] As PropertySymbol) As Accessibility
            Dim getMethod = [property].GetMethod
            Dim setMethod = [property].SetMethod
            If getMethod Is Nothing Then
                Return If((setMethod Is Nothing), Accessibility.NotApplicable, setMethod.DeclaredAccessibility)
            ElseIf setMethod Is Nothing Then
                Return getMethod.DeclaredAccessibility
            End If

            Dim getAccessibility = getMethod.DeclaredAccessibility
            Dim setAccessibility = setMethod.DeclaredAccessibility
            Dim minAccessibility = If((getAccessibility > setAccessibility), setAccessibility, getAccessibility)
            Dim maxAccessibility = If((getAccessibility > setAccessibility), getAccessibility, setAccessibility)
            Return If(((minAccessibility = Accessibility.Protected) AndAlso (maxAccessibility = Accessibility.Friend)), Accessibility.ProtectedOrFriend, maxAccessibility)
        End Function

        Private Shared Function GetParameters(
                                             [property] As PEPropertySymbol,
                                             getMethod As PEMethodSymbol,
                                             setMethod As PEMethodSymbol,
                                             propertyParams As MetadataDecoder.ParamInfo(),
                                             ByRef parametersMatch As Boolean) As ImmutableArray(Of ParameterSymbol)
            parametersMatch = True

            ' First parameter is the property type.
            If propertyParams.Length < 2 Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If

            Dim parameters(propertyParams.Length - 2) As ParameterSymbol
            For i As Integer = 0 To propertyParams.Length - 2
                Dim info = propertyParams(i + 1)
                Dim getParameter = GetAccessorParameter(getMethod, i)
                Dim setParameter = GetAccessorParameter(setMethod, i)
                Dim accessorParameter = If(getParameter, setParameter)

                ' Property parameters are not included in the metadata parameter table,
                ' so we cannot determine parameter names from the property signature
                ' directly. Instead, use the parameters from the getter or setter.
                Dim name As String = Nothing
                Dim isByRef As Boolean = False
                Dim hasByRefBeforeCustomModifiers As Boolean = False
                Dim defaultValue As ConstantValue = Nothing
                Dim flags As ParameterAttributes = 0
                Dim paramHandle = info.Handle
                Dim isParamArray = False
                Dim hasOptionCompare = False

                If accessorParameter IsNot Nothing Then
                    name = accessorParameter.Name
                    isByRef = accessorParameter.IsByRef
                    hasByRefBeforeCustomModifiers = accessorParameter.HasByRefBeforeCustomModifiers
                    defaultValue = accessorParameter.ExplicitDefaultConstantValue
                    flags = accessorParameter.ParamFlags
                    paramHandle = accessorParameter.Handle
                    isParamArray = accessorParameter.IsParamArray
                    hasOptionCompare = accessorParameter.HasOptionCompare
                End If

                If (getParameter IsNot Nothing) AndAlso (setParameter IsNot Nothing) Then
                    ' Do not set Optional unless accessors match, and do not set
                    ' a default value unless both accessors use the same value.
                    If (flags And ParameterAttributes.Optional) <> (setParameter.ParamFlags And ParameterAttributes.Optional) Then
                        flags = flags And Not ParameterAttributes.Optional
                        defaultValue = Nothing
                    ElseIf defaultValue <> setParameter.ExplicitDefaultConstantValue Then
                        defaultValue = Nothing
                        ' Strip off the OptionalAttribute, if any, from the parameter
                        ' since we don't want the resulting property parameter to
                        ' be marked optional but have no default value.
                        flags = flags And Not ParameterAttributes.Optional
                    End If

                    ' Do not set a parameter name unless accessors match. This prevents
                    ' binding o.P(x:=1, y:=2) where the arguments x  and y have different
                    ' parameter indices in get_P and set_P.
                    If Not IdentifierComparison.Equals(name, setParameter.Name) Then
                        name = Nothing
                    End If

                    ' Set if either is byref - it's illegal in VB, so we want to fail if either is set.
                    If setParameter.IsByRef Then
                        isByRef = True
                    End If

                    If setParameter.HasByRefBeforeCustomModifiers Then
                        hasByRefBeforeCustomModifiers = True
                    End If

                    ' Do not set IsParamArray unless both accessors use ParamArray.
                    If Not setParameter.IsParamArray Then
                        isParamArray = False
                    End If

                    ' If OptionCompare is not consistent, the parameters do not match.
                    If hasOptionCompare <> setParameter.HasOptionCompare Then
                        hasOptionCompare = False
                        parametersMatch = False
                    End If
                End If

                parameters(i) = New PEParameterSymbol(
                    [property],
                    name,
                    isByRef:=isByRef,
                    hasByRefBeforeCustomModifiers:=hasByRefBeforeCustomModifiers,
                    type:=info.Type,
                    handle:=paramHandle,
                    flags:=flags,
                    isParamArray:=isParamArray,
                    hasOptionCompare:=hasOptionCompare,
                    ordinal:=i,
                    defaultValue:=defaultValue,
                    customModifiers:=VisualBasicCustomModifier.Convert(info.CustomModifiers))
            Next
            Return parameters.AsImmutableOrNull()
        End Function

        Private Shared Function GetAccessorParameter(accessor As PEMethodSymbol, index As Integer) As PEParameterSymbol
            If accessor IsNot Nothing Then
                Dim parameters = accessor.Parameters
                If index < parameters.Length Then
                    Return DirectCast(parameters(index), PEParameterSymbol)
                End If
            End If
            Return Nothing
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return PEDocumentationCommentUtils.GetDocumentationComment(Me, m_containingType.ContainingPEModule, preferredCulture, cancellationToken, m_lazyDocComment)
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                m_lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return m_lazyUseSiteErrorInfo
        End Function

        Friend ReadOnly Property Handle As PropertyDefinitionHandle
            Get
                Return m_handle
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (m_flags And PropertyAttributes.RTSpecialName) <> 0
            End Get
        End Property

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

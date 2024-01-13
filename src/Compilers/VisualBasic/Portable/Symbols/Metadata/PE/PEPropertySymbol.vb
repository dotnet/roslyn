' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.PooledObjects
Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all properties imported from a PE/module.
    ''' </summary>
    Friend Class PEPropertySymbol
        Inherits PropertySymbol

        Private ReadOnly _name As String
        Private ReadOnly _flags As PropertyAttributes
        Private ReadOnly _containingType As PENamedTypeSymbol
        Private ReadOnly _signatureHeader As SignatureHeader
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnsByRef As Boolean
        Private ReadOnly _propertyType As TypeSymbol
        Private ReadOnly _getMethod As PEMethodSymbol
        Private ReadOnly _setMethod As PEMethodSymbol
        Private ReadOnly _handle As PropertyDefinitionHandle
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyDocComment As Tuple(Of CultureInfo, String)
        Private _lazyCachedUseSiteInfo As CachedUseSiteInfo(Of AssemblySymbol) = CachedUseSiteInfo(Of AssemblySymbol).Uninitialized ' Indicates unknown state. 

        ' mutable because we only know this part after the property is constructed.
        ' Integer because we want to use CMPXCHG on it
        Private _isWithEvents As Integer = ThreeState.Unknown

        ' Distinct accessibility value to represent unset.
        Private Const s_unsetAccessibility As Integer = -1
        Private _lazyDeclaredAccessibility As Integer = s_unsetAccessibility
        Private _lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        Private _lazyIsRequired As ThreeState = ThreeState.Unknown

        Friend Shared Function Create(
            moduleSymbol As PEModuleSymbol,
            containingType As PENamedTypeSymbol,
            handle As PropertyDefinitionHandle,
            getMethod As PEMethodSymbol,
            setMethod As PEMethodSymbol
        ) As PEPropertySymbol
            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(Not handle.IsNil)
            Debug.Assert((getMethod IsNot Nothing) OrElse (setMethod IsNot Nothing))

            Dim metadataDecoder = New MetadataDecoder(moduleSymbol, containingType)
            Dim signatureHeader As SignatureHeader
            Dim propEx As BadImageFormatException = Nothing
            Dim propertyParams = metadataDecoder.GetSignatureForProperty(handle, signatureHeader, propEx)
            Debug.Assert(propertyParams.Length > 0)
            Dim returnInfo As ParamInfo(Of TypeSymbol) = propertyParams(0)

            Dim result As PEPropertySymbol

            If returnInfo.CustomModifiers.IsDefaultOrEmpty AndAlso returnInfo.RefCustomModifiers.IsDefaultOrEmpty Then
                result = New PEPropertySymbol(moduleSymbol, containingType, handle, getMethod, setMethod, metadataDecoder, signatureHeader, propertyParams)
            Else
                result = New PEPropertySymbolWithCustomModifiers(moduleSymbol, containingType, handle, getMethod, setMethod, metadataDecoder, signatureHeader, propertyParams)
            End If

            If propEx IsNot Nothing Then
                result._lazyCachedUseSiteInfo.Initialize(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedProperty1, CustomSymbolDisplayFormatter.QualifiedName(result)))
            End If

            Return result
        End Function

        Private Sub New(
            moduleSymbol As PEModuleSymbol,
            containingType As PENamedTypeSymbol,
            handle As PropertyDefinitionHandle,
            getMethod As PEMethodSymbol,
            setMethod As PEMethodSymbol,
            metadataDecoder As MetadataDecoder,
            signatureHeader As SignatureHeader,
            propertyParams As ParamInfo(Of TypeSymbol)()
        )
            _signatureHeader = signatureHeader
            _containingType = containingType
            _handle = handle
            Dim [module] = moduleSymbol.Module
            Dim mrEx As BadImageFormatException = Nothing

            Try
                [module].GetPropertyDefPropsOrThrow(handle, _name, _flags)
            Catch mrEx
                If _name Is Nothing Then
                    _name = String.Empty
                End If
            End Try

            _getMethod = getMethod
            _setMethod = setMethod

            Dim unusedSignatureHeader As SignatureHeader = Nothing
            Dim getEx As BadImageFormatException = Nothing
            Dim getParams = If(_getMethod Is Nothing, Nothing, metadataDecoder.GetSignatureForMethod(_getMethod.Handle, unusedSignatureHeader, getEx))
            Dim setEx As BadImageFormatException = Nothing
            Dim setParams = If(_setMethod Is Nothing, Nothing, metadataDecoder.GetSignatureForMethod(_setMethod.Handle, unusedSignatureHeader, setEx))

            Dim signaturesMatch = DoSignaturesMatch(metadataDecoder, propertyParams, _getMethod, getParams, _setMethod, setParams)
            Dim parametersMatch = True
            _parameters = GetParameters(Me, _getMethod, _setMethod, propertyParams, parametersMatch)

            If Not signaturesMatch OrElse Not parametersMatch OrElse
               getEx IsNot Nothing OrElse setEx IsNot Nothing OrElse mrEx IsNot Nothing OrElse
               propertyParams.Any(Function(p) p.RefCustomModifiers.AnyRequired() OrElse p.CustomModifiers.AnyRequired()) Then
                _lazyCachedUseSiteInfo.Initialize(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedProperty1, CustomSymbolDisplayFormatter.QualifiedName(Me)))
            End If

            If _getMethod IsNot Nothing Then
                _getMethod.SetAssociatedProperty(Me, MethodKind.PropertyGet)
            End If

            If _setMethod IsNot Nothing Then
                _setMethod.SetAssociatedProperty(Me, MethodKind.PropertySet)
            End If

            Dim returnInfo As ParamInfo(Of TypeSymbol) = propertyParams(0)

            _returnsByRef = returnInfo.IsByRef
            _propertyType = returnInfo.Type
            _propertyType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(_propertyType, handle, moduleSymbol)
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataToken As Integer
            Get
                Return MetadataTokens.GetToken(_handle)
            End Get
        End Property

        Friend ReadOnly Property PropertyFlags As PropertyAttributes
            Get
                Return _flags
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (_flags And PropertyAttributes.SpecialName) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                If Me._lazyDeclaredAccessibility = s_unsetAccessibility Then
                    Interlocked.CompareExchange(Me._lazyDeclaredAccessibility, GetDeclaredAccessibility(Me), s_unsetAccessibility)
                End If

                Return DirectCast(Me._lazyDeclaredAccessibility, Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return (Me._getMethod IsNot Nothing AndAlso Me._getMethod.IsMustOverride) OrElse
                       (Me._setMethod IsNot Nothing AndAlso Me._setMethod.IsMustOverride)
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return (Me._getMethod Is Nothing OrElse Me._getMethod.IsNotOverridable) AndAlso
                       (Me._setMethod Is Nothing OrElse Me._setMethod.IsNotOverridable)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return Not IsMustOverride AndAlso Not IsOverrides AndAlso
                       ((Me._getMethod IsNot Nothing AndAlso Me._getMethod.IsOverridable) OrElse
                        (Me._setMethod IsNot Nothing AndAlso Me._setMethod.IsOverridable))
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return (Me._getMethod IsNot Nothing AndAlso Me._getMethod.IsOverrides) OrElse
                       (Me._setMethod IsNot Nothing AndAlso Me._setMethod.IsOverrides)
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return (Me._getMethod IsNot Nothing AndAlso Me._getMethod.IsOverloads) OrElse
                       (Me._setMethod IsNot Nothing AndAlso Me._setMethod.IsOverloads)
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (Me._getMethod Is Nothing OrElse Me._getMethod.IsShared) AndAlso
                       (Me._setMethod Is Nothing OrElse Me._setMethod.IsShared)
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Dim defaultPropertyName = _containingType.DefaultPropertyName
                Return (Not String.IsNullOrEmpty(defaultPropertyName)) AndAlso
                    IdentifierComparison.Equals(defaultPropertyName, _name)
            End Get
        End Property

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                'NOTE: If no-one set the IsWithEvents, getting the value will permanently set it to Unknown.
                If Me._isWithEvents = ThreeState.Unknown Then
                    SetIsWithEvents(MyBase.IsWithEvents)
                End If

                Debug.Assert(Me._isWithEvents = ThreeState.True OrElse Me._isWithEvents = ThreeState.False)
                Return Me._isWithEvents = ThreeState.True
            End Get
        End Property

        ''' <summary>
        ''' Marks property as definitely IsWithEvents or not.
        ''' The effects of this change cannot be undone.
        ''' Will have no effect if someone already asked if property is IsWithEvents (and will assert since it is not supposed to happen).
        ''' </summary>
        Friend Sub SetIsWithEvents(value As Boolean)
            Dim newValue = If(value, ThreeState.True, ThreeState.False)
            Dim origValue = Threading.Interlocked.CompareExchange(Me._isWithEvents, newValue, ThreeState.Unknown)
            Debug.Assert(origValue = ThreeState.Unknown OrElse origValue = newValue, "Tried changing already known IsWithEvent value.")
        End Sub

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _returnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _propertyType
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return _getMethod
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return _setMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As CallingConvention
            Get
                Return CType(_signatureHeader.RawValue, CallingConvention)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _containingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(_lazyObsoleteAttributeData, _handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return _lazyObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(_handle, _lazyCustomAttributes)
            End If
            Return _lazyCustomAttributes
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return GetAttributes()
        End Function

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                If (Me._getMethod Is Nothing OrElse Me._getMethod.ExplicitInterfaceImplementations.Length = 0) AndAlso (Me._setMethod Is Nothing OrElse Me._setMethod.ExplicitInterfaceImplementations.Length = 0) Then
                    Return ImmutableArray(Of PropertySymbol).Empty
                End If

                Dim propertiesWithImplementedGetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(Me._getMethod)
                Dim propertiesWithImplementedSetters = PEPropertyOrEventHelpers.GetPropertiesForExplicitlyImplementedAccessor(Me._setMethod)
                Dim builder = ArrayBuilder(Of PropertySymbol).GetInstance()
                For Each prop In propertiesWithImplementedGetters
                    Dim setMethod As MethodSymbol = prop.SetMethod

                    If setMethod Is Nothing OrElse Not setMethod.RequiresImplementation() OrElse propertiesWithImplementedSetters.Contains(prop) Then
                        builder.Add(prop)
                    End If
                Next

                For Each prop In propertiesWithImplementedSetters
                    Dim getMethod As MethodSymbol = prop.GetMethod

                    If getMethod Is Nothing OrElse Not getMethod.RequiresImplementation() Then
                        builder.Add(prop)
                    End If
                Next

                Return builder.ToImmutableAndFree()
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
                                                 propertyParams As ParamInfo(Of TypeSymbol)(),
                                                 getMethodOpt As PEMethodSymbol,
                                                 getMethodParamsOpt As ParamInfo(Of TypeSymbol)(),
                                                 setMethodOpt As PEMethodSymbol,
                                                 setMethodParamsOpt As ParamInfo(Of TypeSymbol)()) As Boolean
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
                                             propertyParams As ParamInfo(Of TypeSymbol)(),
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
                Dim defaultValue As ConstantValue = Nothing
                Dim flags As ParameterAttributes = 0
                Dim paramHandle = info.Handle
                Dim isParamArray = False
                Dim hasOptionCompare = False

                If accessorParameter IsNot Nothing Then
                    name = accessorParameter.Name
                    isByRef = accessorParameter.IsByRef
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

                parameters(i) = PEParameterSymbol.Create(
                    [property],
                    name,
                    isByRef:=isByRef,
                    refCustomModifiers:=VisualBasicCustomModifier.Convert(info.RefCustomModifiers),
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
            Return PEDocumentationCommentUtils.GetDocumentationComment(Me, _containingType.ContainingPEModule, preferredCulture, cancellationToken, _lazyDocComment)
        End Function

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Dim primaryDependency As AssemblySymbol = Me.PrimaryDependency

            If Not _lazyCachedUseSiteInfo.IsInitialized Then
                Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = CalculateUseSiteInfo()
                Dim errorInfo = useSiteInfo.DiagnosticInfo
                DeriveCompilerFeatureRequiredDiagnostic(errorInfo)
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, useSiteInfo.AdjustDiagnosticInfo(errorInfo))
            End If

            Return _lazyCachedUseSiteInfo.ToUseSiteInfo(primaryDependency)
        End Function

        Private Sub DeriveCompilerFeatureRequiredDiagnostic(ByRef errorInfo As DiagnosticInfo)
            If errorInfo IsNot Nothing Then
                Return
            End If

            Dim containingModule = _containingType.ContainingPEModule
            Dim decoder = New MetadataDecoder(containingModule, _containingType)
            errorInfo = DeriveCompilerFeatureRequiredAttributeDiagnostic(
                Me,
                containingModule,
                Handle,
                CompilerFeatureRequiredFeatures.None,
                decoder)

            If errorInfo IsNot Nothing Then
                Return
            End If

            For Each param In Parameters
                errorInfo = DirectCast(param, PEParameterSymbol).DeriveCompilerFeatureRequiredDiagnostic(decoder)
                If errorInfo IsNot Nothing Then
                    Return
                End If
            Next

            errorInfo = _containingType.GetCompilerFeatureRequiredDiagnostic()
        End Sub

        Friend ReadOnly Property Handle As PropertyDefinitionHandle
            Get
                Return _handle
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (_flags And PropertyAttributes.RTSpecialName) <> 0
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

        Public Overrides ReadOnly Property IsRequired As Boolean
            Get
                If Not _lazyIsRequired.HasValue() Then
                    _lazyIsRequired = _containingType.ContainingPEModule.Module.HasAttribute(Handle, AttributeDescription.RequiredMemberAttribute).ToThreeState()
                End If

                Return _lazyIsRequired.Value()
            End Get
        End Property

        Private NotInheritable Class PEPropertySymbolWithCustomModifiers
            Inherits PEPropertySymbol

            Private ReadOnly _typeCustomModifiers As ImmutableArray(Of CustomModifier)
            Private ReadOnly _refCustomModifiers As ImmutableArray(Of CustomModifier)

            Public Sub New(
                moduleSymbol As PEModuleSymbol,
                containingType As PENamedTypeSymbol,
                handle As PropertyDefinitionHandle,
                getMethod As PEMethodSymbol,
                setMethod As PEMethodSymbol,
                metadataDecoder As MetadataDecoder,
                signatureHeader As SignatureHeader,
                propertyParams As ParamInfo(Of TypeSymbol)()
            )
                MyBase.New(moduleSymbol, containingType, handle, getMethod, setMethod, metadataDecoder, signatureHeader, propertyParams)

                Dim returnInfo As ParamInfo(Of TypeSymbol) = propertyParams(0)

                _typeCustomModifiers = VisualBasicCustomModifier.Convert(returnInfo.CustomModifiers)
                _refCustomModifiers = VisualBasicCustomModifier.Convert(returnInfo.RefCustomModifiers)
            End Sub

            Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _typeCustomModifiers
                End Get
            End Property

            Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _refCustomModifiers
                End Get
            End Property
        End Class
    End Class
End Namespace

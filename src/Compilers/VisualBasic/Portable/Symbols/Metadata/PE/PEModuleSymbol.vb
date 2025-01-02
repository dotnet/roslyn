' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' Represents a net-module imported from a PE. Can be a primary module of an assembly. 
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class PEModuleSymbol
        Inherits NonMissingModuleSymbol

        ''' <summary>
        ''' Owning AssemblySymbol. This can be a PEAssemblySymbol or a SourceAssemblySymbol.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _assemblySymbol As AssemblySymbol
        Private ReadOnly _ordinal As Integer

        ''' <summary>
        ''' A Module object providing metadata.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _module As PEModule

        ''' <summary>
        ''' Global namespace. 
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _globalNamespace As PENamespaceSymbol

        ''' <summary>
        ''' Cache the symbol for well-known type System.Type because we use it frequently
        ''' (for attributes).
        ''' </summary>
        Private _lazySystemTypeSymbol As NamedTypeSymbol

        ''' <summary>
        ''' The same value as ConcurrentDictionary.DEFAULT_CAPACITY
        ''' </summary>
        Private Const s_defaultTypeMapCapacity As Integer = 31

        ''' <summary>
        ''' This is a map from TypeDef handle to the target <see cref="TypeSymbol"/>. 
        ''' It is used by <see cref="MetadataDecoder"/> to speed up type reference resolution
        ''' for metadata coming from this module. The map is lazily populated
        ''' as we load types from the module.
        ''' </summary>
        Friend ReadOnly TypeHandleToTypeMap As New ConcurrentDictionary(Of TypeDefinitionHandle, TypeSymbol)(concurrencyLevel:=2, capacity:=s_defaultTypeMapCapacity)

        ''' <summary>
        ''' This is a map from TypeRef row id to the target <see cref="TypeSymbol"/>. 
        ''' It is used by <see cref="MetadataDecoder"/> to speed-up type reference resolution
        ''' for metadata coming from this module. The map is lazily populated
        ''' by <see cref="MetadataDecoder"/> as we resolve TypeRefs from the module.
        ''' </summary>
        Friend ReadOnly TypeRefHandleToTypeMap As New ConcurrentDictionary(Of TypeReferenceHandle, TypeSymbol)(concurrencyLevel:=2, capacity:=s_defaultTypeMapCapacity)

        Friend ReadOnly MetadataLocation As ImmutableArray(Of MetadataLocation) =
                                ImmutableArray.Create(Of MetadataLocation)(New MetadataLocation(Me))

        Friend ReadOnly ImportOptions As MetadataImportOptions

        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyAssemblyAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyTypeNames As ICollection(Of String)
        Private _lazyNamespaceNames As ICollection(Of String)

        Private _lazyCachedCompilerFeatureRequiredDiagnosticInfo As DiagnosticInfo = ErrorFactory.EmptyDiagnosticInfo

        Private _lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        Friend Sub New(assemblySymbol As PEAssemblySymbol, [module] As PEModule, importOptions As MetadataImportOptions, ordinal As Integer)
            Me.New(DirectCast(assemblySymbol, AssemblySymbol), [module], importOptions, ordinal)
            Debug.Assert(ordinal >= 0)
        End Sub

        Friend Sub New(assemblySymbol As SourceAssemblySymbol, [module] As PEModule, importOptions As MetadataImportOptions, ordinal As Integer)
            Me.New(DirectCast(assemblySymbol, AssemblySymbol), [module], importOptions, ordinal)
            Debug.Assert(ordinal >= 1)
        End Sub

        Friend Sub New(assemblySymbol As RetargetingAssemblySymbol, [module] As PEModule, importOptions As MetadataImportOptions, ordinal As Integer)
            Me.New(DirectCast(assemblySymbol, AssemblySymbol), [module], importOptions, ordinal)
            Debug.Assert(ordinal >= 1)
        End Sub

        Private Sub New(assemblySymbol As AssemblySymbol, [module] As PEModule, importOptions As MetadataImportOptions, ordinal As Integer)
            Debug.Assert(assemblySymbol IsNot Nothing)
            Debug.Assert([module] IsNot Nothing)
            _assemblySymbol = assemblySymbol
            _ordinal = ordinal
            _module = [module]
            _globalNamespace = New PEGlobalNamespaceSymbol(Me)
            Me.ImportOptions = importOptions
        End Sub

        Friend Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        Friend Overrides ReadOnly Property Machine As System.Reflection.PortableExecutable.Machine
            Get
                Return _module.Machine
            End Get
        End Property

        Friend Overrides ReadOnly Property Bit32Required As Boolean
            Get
                Return _module.Bit32Required
            End Get
        End Property

        Friend ReadOnly Property [Module] As PEModule
            Get
                Return _module
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _assemblySymbol
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                'TODO - Create a Module.Token to return token similar to Assembly.Token
                Me.LoadCustomAttributes(EntityHandle.ModuleDefinition, _lazyCustomAttributes)
            End If
            Return _lazyCustomAttributes
        End Function

        Friend Function GetAssemblyAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyAssemblyAttributes.IsDefault Then
                Dim moduleAssemblyAttributesBuilder As ArrayBuilder(Of VisualBasicAttributeData) = Nothing

                Dim corlibName As String = ContainingAssembly.CorLibrary.Name
                Dim assemblyMSCorLib As EntityHandle = [Module].GetAssemblyRef(corlibName)

                If Not assemblyMSCorLib.IsNil Then
                    For Each qualifier In Cci.MetadataWriter.dummyAssemblyAttributeParentQualifier
                        Dim typerefAssemblyAttributesGoHere As EntityHandle =
                            [Module].GetTypeRef(
                                assemblyMSCorLib,
                                Cci.MetadataWriter.dummyAssemblyAttributeParentNamespace,
                                Cci.MetadataWriter.dummyAssemblyAttributeParentName + qualifier)
                        If Not typerefAssemblyAttributesGoHere.IsNil Then
                            Try
                                For Each customAttributeHandle In [Module].GetCustomAttributesOrThrow(typerefAssemblyAttributesGoHere)
                                    If moduleAssemblyAttributesBuilder Is Nothing Then
                                        moduleAssemblyAttributesBuilder = ArrayBuilder(Of VisualBasicAttributeData).GetInstance()
                                    End If
                                    moduleAssemblyAttributesBuilder.Add(New PEAttributeData(Me, customAttributeHandle))
                                Next
                            Catch mrEx As BadImageFormatException
                            End Try
                        End If
                    Next
                End If

                ImmutableInterlocked.InterlockedCompareExchange(
                    _lazyAssemblyAttributes,
                    If((moduleAssemblyAttributesBuilder IsNot Nothing),
                       moduleAssemblyAttributesBuilder.ToImmutableAndFree(),
                       ImmutableArray(Of VisualBasicAttributeData).Empty),
                   Nothing)
            End If
            Return _lazyAssemblyAttributes
        End Function

        Friend Function GetCustomAttributesForToken(token As EntityHandle) As ImmutableArray(Of VisualBasicAttributeData)
            Return GetCustomAttributesForToken(token, Nothing, filterOut1:=Nothing)
        End Function

        Friend Function GetCustomAttributesForToken(token As EntityHandle,
                                                    <Out()> ByRef filteredOutAttribute1 As CustomAttributeHandle,
                                                    filterOut1 As AttributeDescription,
                                                    <Out()> Optional ByRef filteredOutAttribute2 As CustomAttributeHandle = Nothing,
                                                    Optional filterOut2 As AttributeDescription = Nothing) As ImmutableArray(Of VisualBasicAttributeData)
            Dim builder As ArrayBuilder(Of VisualBasicAttributeData) = Nothing

            filteredOutAttribute1 = Nothing
            filteredOutAttribute2 = Nothing

            Try
                For Each customAttributeHandle In Me.Module.GetCustomAttributesOrThrow(token)
                    If builder Is Nothing Then
                        builder = ArrayBuilder(Of VisualBasicAttributeData).GetInstance()
                    End If

                    If filterOut1.Signatures IsNot Nothing AndAlso
                        [Module].GetTargetAttributeSignatureIndex(
                            customAttributeHandle,
                           filterOut1) <> -1 Then
                        ' It is important to capture the last application of the attribute that we run into,
                        ' it makes a difference for default and constant values.
                        filteredOutAttribute1 = customAttributeHandle
                        Continue For
                    End If

                    If filterOut2.Signatures IsNot Nothing AndAlso
                           [Module].GetTargetAttributeSignatureIndex(
                               customAttributeHandle,
                               filterOut2) <> -1 Then
                        ' It is important to capture the last application of the attribute that we run into,
                        ' it makes a difference for default and constant values.
                        filteredOutAttribute2 = customAttributeHandle
                        Continue For
                    End If

                    builder.Add(New PEAttributeData(Me, customAttributeHandle))
                Next
            Catch mrEx As BadImageFormatException
            End Try

            If builder IsNot Nothing Then
                Return builder.ToImmutableAndFree()
            End If

            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        Friend Sub LoadCustomAttributes(token As EntityHandle, ByRef lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData))
            Dim attributes As ImmutableArray(Of VisualBasicAttributeData) = GetCustomAttributesForToken(token)

            ImmutableInterlocked.InterlockedCompareExchange(Of VisualBasicAttributeData)(
                lazyCustomAttributes,
                attributes,
                Nothing)
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _module.Name
            End Get
        End Property

        Public Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                Return _globalNamespace
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return StaticCast(Of Location).From(Me.MetadataLocation)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _assemblySymbol
            End Get
        End Property

        Friend Sub OnNewTypeDeclarationsLoaded(
            typesDict As Dictionary(Of String, ImmutableArray(Of PENamedTypeSymbol))
        )
            Dim keepLookingForDeclaredCorTypes As Boolean = (_ordinal = 0 AndAlso _assemblySymbol.KeepLookingForDeclaredSpecialTypes)

            For Each types In typesDict.Values
                For Each t In types
                    Dim added As Boolean
                    added = TypeHandleToTypeMap.TryAdd(t.Handle, t)
                    Debug.Assert(added)

                    ' Register newly loaded COR types
                    If (keepLookingForDeclaredCorTypes AndAlso t.SpecialType <> SpecialType.None) Then
                        _assemblySymbol.RegisterDeclaredSpecialType(t)
                        keepLookingForDeclaredCorTypes = _assemblySymbol.KeepLookingForDeclaredSpecialTypes
                    End If
                Next
            Next
        End Sub

        Friend Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                If _lazyTypeNames Is Nothing Then
                    Interlocked.CompareExchange(_lazyTypeNames, _module.TypeNames.AsCaseInsensitiveCollection(), Nothing)
                End If
                Return _lazyTypeNames
            End Get
        End Property

        Friend Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                If _lazyNamespaceNames Is Nothing Then
                    Interlocked.CompareExchange(_lazyNamespaceNames, _module.NamespaceNames.AsCaseInsensitiveCollection(), Nothing)
                End If
                Return _lazyNamespaceNames
            End Get
        End Property

        Friend Overrides Function GetHash(algorithmId As AssemblyHashAlgorithm) As ImmutableArray(Of Byte)
            Return _module.GetHash(algorithmId)
        End Function

        Friend ReadOnly Property DocumentationProvider As DocumentationProvider
            Get
                Dim assembly = TryCast(ContainingAssembly, PEAssemblySymbol)
                If assembly IsNot Nothing Then
                    Return assembly.DocumentationProvider
                Else
                    Return DocumentationProvider.Default
                End If
            End Get
        End Property

        Friend ReadOnly Property SystemTypeSymbol As NamedTypeSymbol
            Get
                If _lazySystemTypeSymbol Is Nothing Then
                    Interlocked.CompareExchange(_lazySystemTypeSymbol, GetWellKnownType(WellKnownType.System_Type), Nothing)
                    Debug.Assert(_lazySystemTypeSymbol IsNot Nothing)
                End If

                Return _lazySystemTypeSymbol
            End Get
        End Property

        Public Function GetEventRegistrationTokenType() As NamedTypeSymbol
            Return GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)
        End Function

        Private Function GetWellKnownType(type As WellKnownType) As NamedTypeSymbol
            Dim emittedName As MetadataTypeName = MetadataTypeName.FromFullName(type.GetMetadataName(), useCLSCompliantNameArityEncoding:=True)

            ' First, check this module
            Dim currentModuleResult As NamedTypeSymbol = Me.LookupTopLevelMetadataType(emittedName)
            Debug.Assert(If(Not currentModuleResult?.IsErrorType(), True))

            If currentModuleResult IsNot Nothing Then
                Debug.Assert(IsAcceptableSystemTypeSymbol(currentModuleResult))

                ' It doesn't matter if there's another System.Type in a referenced assembly -
                ' we prefer the one in the current module.
                Return currentModuleResult
            End If

            ' If we didn't find it in this module, check the referenced assemblies
            Dim referencedAssemblyResult As NamedTypeSymbol = Nothing
            For Each assembly As AssemblySymbol In Me.GetReferencedAssemblySymbols()
                Dim currResult As NamedTypeSymbol = assembly.LookupDeclaredOrForwardedTopLevelMetadataType(emittedName, visitedAssemblies:=Nothing)
                If IsAcceptableSystemTypeSymbol(currResult) Then
                    If referencedAssemblyResult Is Nothing Then
                        referencedAssemblyResult = currResult
                    Else
                        ' CONSIDER: setting result to null will result in a MissingMetadataTypeSymbol 
                        ' being returned.  Do we want to differentiate between no result and ambiguous
                        ' results?  There doesn't seem to be an existing error code for "duplicate well-
                        ' known type".
                        If referencedAssemblyResult IsNot currResult Then
                            referencedAssemblyResult = Nothing
                            Exit For
                        End If
                    End If
                End If
            Next

            If referencedAssemblyResult IsNot Nothing Then
                Debug.Assert(IsAcceptableSystemTypeSymbol(referencedAssemblyResult))
                Return referencedAssemblyResult
            End If

            Return New MissingMetadataTypeSymbol.TopLevel(Me, emittedName)
        End Function

        Private Shared Function IsAcceptableSystemTypeSymbol(candidate As NamedTypeSymbol) As Boolean
            Return candidate.Kind <> SymbolKind.ErrorType OrElse Not (TypeOf candidate Is MissingMetadataTypeSymbol)
        End Function

        Friend Overrides ReadOnly Property HasAssemblyCompilationRelaxationsAttribute As Boolean
            Get
                ' This API is called only for added modules. Assembly level attributes from added modules are 
                ' copied to the resulting assembly and that is done by using VisualBasicAttributeData for them.
                ' Therefore, it is acceptable to implement this property by using the same VisualBasicAttributeData
                ' objects rather than trying to avoid creating them and going to metadata directly.
                Dim assemblyAttributes As ImmutableArray(Of VisualBasicAttributeData) = GetAssemblyAttributes()
                Return assemblyAttributes.IndexOfAttribute(AttributeDescription.CompilationRelaxationsAttribute) >= 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasAssemblyRuntimeCompatibilityAttribute As Boolean
            Get
                ' This API is called only for added modules. Assembly level attributes from added modules are 
                ' copied to the resulting assembly and that is done by using VisualBasicAttributeData for them.
                ' Therefore, it is acceptable to implement this property by using the same VisualBasicAttributeData
                ' objects rather than trying to avoid creating them and going to metadata directly.
                Dim assemblyAttributes As ImmutableArray(Of VisualBasicAttributeData) = GetAssemblyAttributes()
                Return assemblyAttributes.IndexOfAttribute(AttributeDescription.RuntimeCompatibilityAttribute) >= 0
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultMarshallingCharSet As CharSet?
            Get
                Throw ExceptionUtilities.Unreachable
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

        Friend Overloads Function LookupTopLevelMetadataType(ByRef emittedName As MetadataTypeName, <Out> ByRef isNoPiaLocalType As Boolean) As NamedTypeSymbol
            Dim result As NamedTypeSymbol
            Dim scope = DirectCast(GlobalNamespace.LookupNestedNamespace(emittedName.NamespaceSegments), PENamespaceSymbol)

            If scope Is Nothing Then
                ' We failed to locate the namespace
                result = Nothing
            Else
                result = scope.LookupMetadataType(emittedName)

                If result Is Nothing Then
                    result = scope.UnifyIfNoPiaLocalType(emittedName)

                    If result IsNot Nothing Then
                        isNoPiaLocalType = True
                        Return result
                    End If
                End If
            End If

            isNoPiaLocalType = False
            Return If(result, New MissingMetadataTypeSymbol.TopLevel(Me, emittedName))
        End Function

        ''' <summary>
        ''' Returns a tuple of the assemblies this module forwards the given type to.
        ''' </summary>
        ''' <param name="fullName">Type to look up.</param>
        ''' <param name="ignoreCase">Pass true to look up fullName case-insensitively.  WARNING: more expensive.</param>
        ''' <param name="matchedName">Returns the actual casing of the matching name.</param>
        ''' <returns>A tuple of the forwarded to assemblies.</returns>
        ''' <remarks>
        ''' The returned assemblies may also forward the type.
        ''' </remarks>
        Friend Function GetAssembliesForForwardedType(ByRef fullName As MetadataTypeName, ignoreCase As Boolean, <Out> ByRef matchedName As String) As (FirstSymbol As AssemblySymbol, SecondSymbol As AssemblySymbol)
            Dim indices = Me.Module.GetAssemblyRefsForForwardedType(fullName.FullName, ignoreCase, matchedName)

            If indices.FirstIndex < 0 Then
                Return (Nothing, Nothing)
            End If

            Dim firstSymbol = GetReferencedAssemblySymbol(indices.FirstIndex)

            If indices.SecondIndex < 0 Then
                Return (firstSymbol, Nothing)
            End If

            Dim secondSymbol = GetReferencedAssemblySymbol(indices.SecondIndex)
            Return (firstSymbol, secondSymbol)
        End Function

        Friend Iterator Function GetForwardedTypes() As IEnumerable(Of NamedTypeSymbol)
            For Each forwarder As KeyValuePair(Of String, (FirstIndex As Integer, SecondIndex As Integer)) In Me.Module.GetForwardedTypes()
                Dim name = MetadataTypeName.FromFullName(forwarder.Key)

                Debug.Assert(forwarder.Value.FirstIndex >= 0, "First index should never be negative")
                Dim firstSymbol = GetReferencedAssemblySymbol(forwarder.Value.FirstIndex)
                Debug.Assert(firstSymbol IsNot Nothing, "Invalid indexes (out of bound) are discarded during reading metadata in PEModule.EnsureForwardTypeToAssemblyMap()")

                If forwarder.Value.SecondIndex >= 0 Then
                    Dim secondSymbol = GetReferencedAssemblySymbol(forwarder.Value.SecondIndex)
                    Debug.Assert(secondSymbol IsNot Nothing, "Invalid indexes (out of bound) are discarded during reading metadata in PEModule.EnsureForwardTypeToAssemblyMap()")

                    Yield ContainingAssembly.CreateMultipleForwardingErrorTypeSymbol(name, Me, firstSymbol, secondSymbol)
                Else
                    Yield firstSymbol.LookupDeclaredOrForwardedTopLevelMetadataType(name, visitedAssemblies:=Nothing)
                End If
            Next
        End Function

        Public Overrides Function GetMetadata() As ModuleMetadata
            Return _module.GetNonDisposableMetadata()
        End Function

        Friend Function GetCompilerFeatureRequiredDiagnostic() As DiagnosticInfo
            If _lazyCachedCompilerFeatureRequiredDiagnosticInfo Is ErrorFactory.EmptyDiagnosticInfo Then
                Interlocked.CompareExchange(
                    _lazyCachedCompilerFeatureRequiredDiagnosticInfo,
                    DeriveCompilerFeatureRequiredAttributeDiagnostic(Me, Me, EntityHandle.ModuleDefinition, CompilerFeatureRequiredFeatures.None, New MetadataDecoder(Me)),
                    ErrorFactory.EmptyDiagnosticInfo)
            End If

            Return If(_lazyCachedCompilerFeatureRequiredDiagnosticInfo,
                      TryCast(ContainingAssembly, PEAssemblySymbol)?.GetCompilerFeatureRequiredDiagnosticInfo())
        End Function

        Public Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim info = GetCompilerFeatureRequiredDiagnostic()
                If info IsNot Nothing Then
                    Return info.Code = DirectCast(ERRID.ERR_UnsupportedCompilerFeature, Integer) OrElse MyBase.HasUnsupportedMetadata
                End If

                Return MyBase.HasUnsupportedMetadata
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                If _lazyObsoleteAttributeData Is ObsoleteAttributeData.Uninitialized Then
                    Dim experimentalData = _module.TryDecodeExperimentalAttributeData(EntityHandle.ModuleDefinition, New MetadataDecoder(Me))
                    Interlocked.CompareExchange(_lazyObsoleteAttributeData, experimentalData, ObsoleteAttributeData.Uninitialized)
                End If

                Return _lazyObsoleteAttributeData
            End Get
        End Property

    End Class
End Namespace

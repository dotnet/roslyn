' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports CommonAssemblyWellKnownAttributeData = Microsoft.CodeAnalysis.CommonAssemblyWellKnownAttributeData(Of Microsoft.CodeAnalysis.VisualBasic.Symbols.NamedTypeSymbol)

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents an assembly built by compiler.
    ''' </summary>
    ''' <remarks></remarks>
    Partial Friend NotInheritable Class SourceAssemblySymbol
        Inherits MetadataOrSourceAssemblySymbol
        Implements ISourceAssemblySymbol, ISourceAssemblySymbolInternal, IAttributeTargetSymbol

        ''' <summary>
        ''' A Compilation the assembly is created for.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _compilation As VisualBasicCompilation

        Private _lazyStrongNameKeys As StrongNameKeys

        ''' <summary>
        ''' Assembly's identity.
        ''' </summary>
        ''' <remarks></remarks>
        Friend m_lazyIdentity As AssemblyIdentity

        ''' <summary>
        ''' A list of modules the assembly consists of. 
        ''' The first (index=0) module is a SourceModuleSymbol, which is a primary module, the rest are net-modules.
        ''' </summary>
        Private ReadOnly _modules As ImmutableArray(Of ModuleSymbol)

        Private _lazySourceAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        Private _lazyNetModuleAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ''' <summary>
        ''' Indices of duplicate assembly attributes, i.e. attributes that bind to the same constructor and have identical arguments, that must not be emitted.
        ''' </summary>
        ''' <remarks>
        ''' These indices correspond to the merged assembly attributes from source and added net modules, i.e. attributes returned by <see cref="GetAttributes"/> method.
        ''' </remarks>
        Private _lazyDuplicateAttributeIndices As HashSet(Of Integer)

        Private _lazyEmitExtensionAttribute As Byte = ThreeState.Unknown

        Private _lazyIsVbRuntime As ThreeState = ThreeState.Unknown

        Private _lazyAssemblyLevelDeclarationErrors As ImmutableArray(Of Diagnostic)
        Private _lazyAssemblyLevelDeclarationDependencies As ImmutableArray(Of AssemblySymbol)

        Private ReadOnly _assemblySimpleName As String

        'This maps from assembly name to a set of public keys. It uses concurrent dictionaries because it is built,
        'one attribute at a time, in the callback that validates an attribute's application to a symbol. It is assumed
        'to be complete after a call to GetAttributes(). The second dictionary is acting like a set. The value element is
        'only used when the key is empty in which case it stores the location and value of the attribute string which
        'may be used to construct a diagnostic if the assembly being compiled is found to be strong named.
        Private _lazyInternalsVisibleToMap As ConcurrentDictionary(Of String, ConcurrentDictionary(Of ImmutableArray(Of Byte), Tuple(Of Location, String)))

        Friend Sub New(compilation As VisualBasicCompilation,
                       assemblySimpleName As String,
                       moduleName As String,
                       netModules As ImmutableArray(Of PEModule))

            Debug.Assert(compilation IsNot Nothing)
            Debug.Assert(assemblySimpleName IsNot Nothing)
            Debug.Assert(Not String.IsNullOrWhiteSpace(moduleName))
            Debug.Assert(Not netModules.IsDefault)

            _compilation = compilation
            _assemblySimpleName = assemblySimpleName

            Dim moduleBuilder As New ArrayBuilder(Of ModuleSymbol)(1 + netModules.Length)

            moduleBuilder.Add(New SourceModuleSymbol(Me, compilation.Declarations, compilation.Options, moduleName))

            Dim importOptions = If(compilation.Options.MetadataImportOptions = MetadataImportOptions.All,
                                   MetadataImportOptions.All,
                                   MetadataImportOptions.Internal)

            For Each netModule As PEModule In netModules
                moduleBuilder.Add(New PEModuleSymbol(Me, netModule, importOptions, moduleBuilder.Count))
                ' SetReferences will be called later by the ReferenceManager (in CreateSourceAssemblyFullBind for 
                ' a fresh manager, in CreateSourceAssemblyReuseData for a reused one).
            Next

            _modules = moduleBuilder.ToImmutableAndFree()

            If Not compilation.Options.CryptoPublicKey.IsEmpty Then
                ' Private key Is Not necessary for assembly identity, only when emitting.  For this reason, the private key can remain null.
                _lazyStrongNameKeys = StrongNameKeys.Create(compilation.Options.CryptoPublicKey, privateKey:=Nothing, hasCounterSignature:=False, MessageProvider.Instance)
            End If
        End Sub

        ''' <summary>
        ''' This override is essential - it's a base case of the recursive definition.
        ''' </summary>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return _compilation
            End Get
        End Property

        Public Overrides ReadOnly Property IsInteractive As Boolean
            Get
                Return _compilation.IsSubmission
            End Get
        End Property

        Friend Function MightContainNoPiaLocalTypes() As Boolean

            For i As Integer = 1 To _modules.Length - 1
                Dim peModuleSymbol = DirectCast(_modules(i), Symbols.Metadata.PE.PEModuleSymbol)
                If peModuleSymbol.[Module].ContainsNoPiaLocalTypes() Then
                    Return True
                End If
            Next

            Return SourceModule.MightContainNoPiaLocalTypes()
        End Function

        Public Overrides ReadOnly Property Identity As AssemblyIdentity
            Get
                If m_lazyIdentity Is Nothing Then
                    Interlocked.CompareExchange(m_lazyIdentity, ComputeIdentity(), Nothing)
                End If

                Return m_lazyIdentity
            End Get
        End Property

        Friend Overrides Function GetSpecialTypeMember(member As SpecialMember) As Symbol
            If _compilation.IsMemberMissing(member) Then
                Return Nothing
            End If

            Return MyBase.GetSpecialTypeMember(member)
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _assemblySimpleName
            End Get
        End Property

        Private Function IsKnownAssemblyAttribute(attribute As VisualBasicAttributeData) As Boolean

            ' TODO: This list used to include AssemblyOperatingSystemAttribute and AssemblyProcessorAttribute,
            '       but it doesn't look like they are defined, cannot find them on MSDN.
            If attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyTitleAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyDescriptionAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyConfigurationAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyCultureAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyVersionAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyCompanyAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyProductAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyInformationalVersionAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyCopyrightAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyTrademarkAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyKeyFileAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyKeyNameAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyAlgorithmIdAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyFlagsAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyDelaySignAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblyFileVersionAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.SatelliteContractVersionAttribute) OrElse
               attribute.IsTargetAttribute(Me, AttributeDescription.AssemblySignatureKeyAttribute) Then

                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Gets unique source assembly attributes that should be emitted,
        ''' i.e. filters out attributes with errors and duplicate attributes.
        ''' </summary>
        Private Function GetUniqueSourceAssemblyAttributes(<Out> ByRef attributeIndicesToSkip As HashSet(Of Integer)) As HashSet(Of VisualBasicAttributeData)
            Dim appliedSourceAttributes As ImmutableArray(Of VisualBasicAttributeData) = Me.GetSourceAttributesBag().Attributes

            Dim uniqueAttributes As HashSet(Of VisualBasicAttributeData) = Nothing
            attributeIndicesToSkip = Nothing

            For i As Integer = 0 To appliedSourceAttributes.Length - 1
                Dim attribute As VisualBasicAttributeData = appliedSourceAttributes(i)
                If Not attribute.HasErrors Then
                    If Not AddUniqueAssemblyAttribute(attribute, uniqueAttributes) Then
                        If attributeIndicesToSkip Is Nothing Then
                            attributeIndicesToSkip = New HashSet(Of Integer)
                        End If

                        attributeIndicesToSkip.Add(i)
                    End If
                End If
            Next

            Return uniqueAttributes
        End Function

        Private Shared Function AddUniqueAssemblyAttribute(attribute As VisualBasicAttributeData, ByRef uniqueAttributes As HashSet(Of VisualBasicAttributeData)) As Boolean
            Debug.Assert(Not attribute.HasErrors)

            If uniqueAttributes Is Nothing Then
                uniqueAttributes = New HashSet(Of VisualBasicAttributeData)(comparer:=CommonAttributeDataComparer.Instance)
            End If

            Return uniqueAttributes.Add(attribute)
        End Function

        Private Function ValidateAttributeUsageForNetModuleAttribute(attribute As VisualBasicAttributeData, netModuleName As String, diagnostics As BindingDiagnosticBag, ByRef uniqueAttributes As HashSet(Of VisualBasicAttributeData)) As Boolean
            Debug.Assert(Not attribute.HasErrors)

            Dim attributeClass = attribute.AttributeClass

            If attributeClass.GetAttributeUsageInfo().AllowMultiple Then
                ' Duplicate attributes are allowed, but native compiler doesn't emit duplicate attributes, i.e. attributes with same constructor and arguments.
                Return AddUniqueAssemblyAttribute(attribute, uniqueAttributes)
            Else
                ' Duplicate attributes with same attribute type are not allowed.
                ' Check if there is an existing assembly attribute with same attribute type.
                If uniqueAttributes Is Nothing OrElse Not uniqueAttributes.Contains(Function(a) TypeSymbol.Equals(a.AttributeClass, attributeClass, TypeCompareKind.ConsiderEverything)) Then
                    ' Attribute with unique attribute type, not a duplicate.
                    Dim success As Boolean = AddUniqueAssemblyAttribute(attribute, uniqueAttributes)
                    Debug.Assert(success)
                    Return True
                Else
                    ' Duplicate attribute with same attribute type, we should report an error.

                    ' Native compiler suppresses the error for
                    ' (a) Duplicate well-known assembly attributes and
                    ' (b) Identical duplicates, i.e. attributes with same constructor and arguments.

                    ' For (a), native compiler picks the last of these duplicate well-known netmodule attributes, but these can vary based on the ordering of referenced netmodules.

                    If IsKnownAssemblyAttribute(attribute) Then
                        If Not uniqueAttributes.Contains(attribute) Then
                            ' This attribute application will be ignored.
                            diagnostics.Add(ERRID.WRN_AssemblyAttributeFromModuleIsOverridden, NoLocation.Singleton, attribute.AttributeClass, netModuleName)
                        End If
                    ElseIf AddUniqueAssemblyAttribute(attribute, uniqueAttributes) Then
                        ' Error
                        diagnostics.Add(ERRID.ERR_InvalidMultipleAttributeUsageInNetModule2, NoLocation.Singleton, attribute.AttributeClass.Name, netModuleName)
                    End If

                    Return False
                End If
            End If

            ' CONSIDER Handling badly targeted assembly attributes from netmodules
            'If (attributeUsage.ValidTargets And AttributeTargets.Assembly) = 0 Then
            '    If Not attribute.HasErrors Then
            '        'Error and skip
            '        diagnostics.Add(ERRID.ERR_InvalidAssemblyAttributeInNetModule2, NoLocation.Singleton, attribute.AttributeClass.Name, netModuleName, attributeUsage.GetValidTargetsString())
            '    End If
            'End If
        End Function

        Private Function GetNetModuleAttributes(<Out> ByRef netModuleNames As ImmutableArray(Of String)) As ImmutableArray(Of VisualBasicAttributeData)
            Dim netModuleNamesBuilder As ArrayBuilder(Of String) = Nothing
            Dim moduleAssemblyAttributesBuilder As ArrayBuilder(Of VisualBasicAttributeData) = Nothing

            For i As Integer = 1 To _modules.Length - 1
                Dim peModuleSymbol = DirectCast(_modules(i), Symbols.Metadata.PE.PEModuleSymbol)
                Dim netModuleName As String = peModuleSymbol.Name
                For Each attributeData In peModuleSymbol.GetAssemblyAttributes()
                    If netModuleNamesBuilder Is Nothing Then
                        netModuleNamesBuilder = ArrayBuilder(Of String).GetInstance()
                        moduleAssemblyAttributesBuilder = ArrayBuilder(Of VisualBasicAttributeData).GetInstance()
                    End If

                    netModuleNamesBuilder.Add(netModuleName)
                    moduleAssemblyAttributesBuilder.Add(attributeData)
                Next
            Next

            If netModuleNamesBuilder Is Nothing Then
                netModuleNames = ImmutableArray(Of String).Empty
                Return ImmutableArray(Of VisualBasicAttributeData).Empty
            End If

            netModuleNames = netModuleNamesBuilder.ToImmutableAndFree()
            Return moduleAssemblyAttributesBuilder.ToImmutableAndFree()
        End Function

        Private Function ValidateAttributeUsageAndDecodeWellKnownNetModuleAttributes(
            attributesFromNetModules As ImmutableArray(Of VisualBasicAttributeData),
            netModuleNames As ImmutableArray(Of String),
            diagnostics As BindingDiagnosticBag,
            <Out> ByRef attributeIndicesToSkip As HashSet(Of Integer)) As WellKnownAttributeData

            Debug.Assert(attributesFromNetModules.Any())
            Debug.Assert(netModuleNames.Any())
            Debug.Assert(attributesFromNetModules.Length = netModuleNames.Length)

            Dim tree = VisualBasicSyntaxTree.Dummy
            Dim node = tree.GetRoot()
            Dim binder As Binder = BinderBuilder.CreateSourceModuleBinder(Me.SourceModule)

            Dim netModuleAttributesCount As Integer = attributesFromNetModules.Length
            Dim sourceAttributesCount As Integer = Me.GetSourceAttributesBag().Attributes.Length

            ' Get unique source assembly attributes.
            Dim uniqueAttributes As HashSet(Of VisualBasicAttributeData) = GetUniqueSourceAssemblyAttributes(attributeIndicesToSkip)

            Dim arguments = New DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation)()
            arguments.AttributesCount = netModuleAttributesCount
            arguments.Diagnostics = diagnostics
            arguments.SymbolPart = AttributeLocation.None

            ' Attributes from the second added module should override attributes from the first added module, etc. 
            ' Attributes from source should override attributes from added modules.
            ' That is why we are iterating attributes backwards.
            For i As Integer = netModuleAttributesCount - 1 To 0 Step -1
                Dim attribute As VisualBasicAttributeData = attributesFromNetModules(i)
                If Not attribute.HasErrors AndAlso ValidateAttributeUsageForNetModuleAttribute(attribute, netModuleNames(i), diagnostics, uniqueAttributes) Then
                    arguments.Attribute = attribute
                    arguments.Index = i

                    ' CONSIDER Provide usable AttributeSyntax node for diagnostics of malformed netmodule assembly attributes
                    arguments.AttributeSyntaxOpt = Nothing

                    Me.DecodeWellKnownAttribute(arguments)
                Else
                    If attributeIndicesToSkip Is Nothing Then
                        attributeIndicesToSkip = New HashSet(Of Integer)
                    End If

                    attributeIndicesToSkip.Add(i + sourceAttributesCount)
                End If
            Next

            Return If(arguments.HasDecodedData, arguments.DecodedData, Nothing)
        End Function

        Private Sub LoadAndValidateNetModuleAttributes(ByRef lazyNetModuleAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData))

            ' Indices of duplicate assembly attributes, i.e. attributes that bind to the same constructor and have identical arguments, that must not be emitted.
            Dim attributeIndicesToSkip As HashSet(Of Integer) = Nothing

            If _compilation.Options.OutputKind.IsNetModule() Then
                ' Compute duplicate source assembly attributes, i.e. attributes with same constructor and arguments, that must not be emitted.
                Dim unused = GetUniqueSourceAssemblyAttributes(attributeIndicesToSkip)

                Interlocked.CompareExchange(lazyNetModuleAttributesBag, CustomAttributesBag(Of VisualBasicAttributeData).Empty, Nothing)
            Else
                Dim diagnostics = BindingDiagnosticBag.GetInstance()
                Dim netModuleNames As ImmutableArray(Of String) = Nothing
                Dim attributesFromNetModules As ImmutableArray(Of VisualBasicAttributeData) = GetNetModuleAttributes(netModuleNames)
                Dim wellKnownData As WellKnownAttributeData = Nothing

                If attributesFromNetModules.Any() Then
                    wellKnownData = ValidateAttributeUsageAndDecodeWellKnownNetModuleAttributes(attributesFromNetModules, netModuleNames, diagnostics, attributeIndicesToSkip)
                Else
                    ' Compute duplicate source assembly attributes, i.e. attributes with same constructor and arguments, that must not be emitted.
                    Dim unused = GetUniqueSourceAssemblyAttributes(attributeIndicesToSkip)
                End If

                ' Load type forwarders from modules
                Dim forwardedTypes As HashSet(Of NamedTypeSymbol) = Nothing

                ' Similar to attributes, type forwarders from the second added module should override type forwarders from the first added module, etc. 
                ' This affects only diagnostics.
                For i As Integer = _modules.Length - 1 To 1 Step -1
                    Dim peModuleSymbol = DirectCast(_modules(i), PEModuleSymbol)

                    For Each forwarded As NamedTypeSymbol In peModuleSymbol.GetForwardedTypes()
                        If forwardedTypes Is Nothing Then
                            If wellKnownData Is Nothing Then
                                wellKnownData = New CommonAssemblyWellKnownAttributeData()
                            End If

                            forwardedTypes = DirectCast(wellKnownData, CommonAssemblyWellKnownAttributeData).ForwardedTypes
                            If forwardedTypes Is Nothing Then
                                forwardedTypes = New HashSet(Of NamedTypeSymbol)()
                                DirectCast(wellKnownData, CommonAssemblyWellKnownAttributeData).ForwardedTypes = forwardedTypes
                            End If
                        End If

                        If forwardedTypes.Add(forwarded) Then
                            If forwarded.IsErrorType() Then
                                Dim info As DiagnosticInfo = If(forwarded.GetUseSiteInfo().DiagnosticInfo, DirectCast(forwarded, ErrorTypeSymbol).ErrorInfo)

                                If info IsNot Nothing Then
                                    diagnostics.Add(info, NoLocation.Singleton)
                                End If
                            End If
                        End If
                    Next
                Next

                Dim netModuleAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

                If wellKnownData IsNot Nothing OrElse attributesFromNetModules.Any() Then
                    netModuleAttributesBag = New CustomAttributesBag(Of VisualBasicAttributeData)()
                    netModuleAttributesBag.SetEarlyDecodedWellKnownAttributeData(Nothing)
                    netModuleAttributesBag.SetDecodedWellKnownAttributeData(wellKnownData)
                    netModuleAttributesBag.SetAttributes(attributesFromNetModules)
                Else
                    netModuleAttributesBag = CustomAttributesBag(Of VisualBasicAttributeData).Empty
                End If

                ' Check if we have any duplicate assembly attribute that must not be emitted,
                ' unless we are emitting a net module.
                If attributeIndicesToSkip IsNot Nothing Then
                    Debug.Assert(attributeIndicesToSkip.Any())
                    Interlocked.CompareExchange(_lazyDuplicateAttributeIndices, attributeIndicesToSkip, Nothing)
                End If

                SourceModule.AtomicStoreReferenceAndDiagnostics(lazyNetModuleAttributesBag, netModuleAttributesBag, diagnostics)
                diagnostics.Free()

                Debug.Assert(attributeIndicesToSkip Is Nothing OrElse
                             Not attributeIndicesToSkip.Any(Function(index) index < 0 OrElse index >= Me.GetAttributes().Length))
            End If

            Debug.Assert(lazyNetModuleAttributesBag.IsSealed)
        End Sub

        Friend Overrides Function GetAllTopLevelForwardedTypes() As IEnumerable(Of NamedTypeSymbol)
            Return Emit.PEModuleBuilder.GetForwardedTypes(Me, builderOpt:=Nothing)
        End Function

        Private Sub EnsureNetModuleAttributesAreBound()
            If _lazyNetModuleAttributesBag Is Nothing Then
                LoadAndValidateNetModuleAttributes(_lazyNetModuleAttributesBag)
            End If
        End Sub

        ''' <summary>
        ''' Returns true if the assembly attribute at the given index is a duplicate assembly attribute that must not be emitted.
        ''' Duplicate assembly attributes are attributes that bind to the same constructor and have identical arguments.
        ''' </summary>
        ''' <remarks>
        ''' This method must be invoked only after all the assembly attributes have been bound.
        ''' </remarks>
        Friend Function IsIndexOfDuplicateAssemblyAttribute(index As Integer) As Boolean
            Debug.Assert(Me._lazySourceAttributesBag.IsSealed)
            Debug.Assert(Me._lazyNetModuleAttributesBag.IsSealed)
            Debug.Assert(index >= 0)
            Debug.Assert(index < Me.GetAttributes().Length)
            Debug.Assert(Me._lazyDuplicateAttributeIndices Is Nothing OrElse Not IsNetModule)

            Return Me._lazyDuplicateAttributeIndices IsNot Nothing AndAlso Me._lazyDuplicateAttributeIndices.Contains(index)
        End Function

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Assembly
            End Get
        End Property

        Friend Function GetAttributeDeclarations() As ImmutableArray(Of SyntaxList(Of AttributeListSyntax))
            Dim attributeBlocks = ArrayBuilder(Of SyntaxList(Of AttributeListSyntax)).GetInstance()
            Dim declarations = DeclaringCompilation.MergedRootDeclaration.Declarations

            For Each rootNs As RootSingleNamespaceDeclaration In declarations
                If rootNs.HasAssemblyAttributes Then
                    Dim compilationUnitSyntax = DirectCast(rootNs.Location.SourceTree.GetRoot(), CompilationUnitSyntax)
                    Dim attributeStatements = compilationUnitSyntax.Attributes

                    If attributeStatements.Any Then
                        For Each statement In attributeStatements
                            'For Each block In statement.AttributeLists
                            'attributeBlocks.Add(block)
                            'Next
                            attributeBlocks.Add(statement.AttributeLists)
                        Next
                    End If
                End If
            Next

            Return attributeBlocks.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Returns a bag of netmodule assembly attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' </remarks>
        Friend Function GetNetModuleAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            EnsureNetModuleAttributesAreBound()
            Return _lazyNetModuleAttributesBag
        End Function

        Private Function GetNetModuleAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Dim attributesBag = Me.GetNetModuleAttributesBag()
            Debug.Assert(attributesBag.IsSealed)
            Return attributesBag.Attributes
        End Function

        Friend Function GetNetModuleDecodedWellKnownAttributeData() As CommonAssemblyWellKnownAttributeData
            Dim attributesBag = Me.GetNetModuleAttributesBag()
            Debug.Assert(attributesBag.IsSealed)
            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData)
        End Function

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: This method should always be kept as a NotOverridable method.
        ''' If you want to override attribute binding logic for a sub-class, then override <see cref="GetSourceAttributesBag"/> method.
        ''' </remarks>
        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Dim attributes = Me.GetSourceAttributesBag().Attributes
            Dim netmoduleAttributes = Me.GetNetModuleAttributesBag().Attributes
            Debug.Assert(Not attributes.IsDefault)
            Debug.Assert(Not netmoduleAttributes.IsDefault)

            If attributes.Length > 0 Then
                If netmoduleAttributes.Length > 0 Then
                    attributes = attributes.Concat(netmoduleAttributes)
                End If
            Else
                attributes = netmoduleAttributes
            End If

            Debug.Assert(Not attributes.IsDefault)
            Return attributes
        End Function

        Friend Function GetSourceAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            EnsureAttributesAreBound()
            Return Me._lazySourceAttributesBag
        End Function

        ''' <summary>
        ''' Returns data decoded from source assembly attributes or null if there are none.
        ''' </summary>
        ''' <returns>
        ''' Forces binding and decoding of attributes.
        ''' TODO: We should replace methods GetSourceDecodedWellKnownAttributeData and GetNetModuleDecodedWellKnownAttributeData with
        ''' a single method GetDecodedWellKnownAttributeData, which merges DecodedWellKnownAttributeData from source and netmodule attributes.
        ''' </returns>
        ''' <remarks></remarks>
        Friend Function GetSourceDecodedWellKnownAttributeData() As CommonAssemblyWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazySourceAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetSourceAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData)
        End Function

        Private Function GetWellKnownAttributeDataStringField(fieldGetter As Func(Of CommonAssemblyWellKnownAttributeData, String), Optional missingValue As String = Nothing) As String
            Dim defaultValue As String = missingValue
            Dim fieldValue = defaultValue

            Dim data = GetSourceDecodedWellKnownAttributeData()
            If data IsNot Nothing Then
                fieldValue = fieldGetter(data)
            End If

            If fieldValue Is missingValue Then
                data = GetNetModuleDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    fieldValue = fieldGetter(data)
                End If
            End If

            Return fieldValue
        End Function

        Friend Function GetSecurityAttributes() As IEnumerable(Of Cci.SecurityAttribute)
            Dim sourceSecurityAttributes As IEnumerable(Of Cci.SecurityAttribute) = Nothing

            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.GetSourceAttributesBag()
            Dim wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol))
            If wellKnownAttributeData IsNot Nothing Then
                Dim securityData As SecurityWellKnownAttributeData = wellKnownAttributeData.SecurityInformation
                If securityData IsNot Nothing Then
                    sourceSecurityAttributes = securityData.GetSecurityAttributes(attributesBag.Attributes)
                End If
            End If

            Dim netmoduleSecurityAttributes As IEnumerable(Of Cci.SecurityAttribute) = Nothing
            attributesBag = Me.GetNetModuleAttributesBag()
            wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol))
            If wellKnownAttributeData IsNot Nothing Then
                Dim securityData As SecurityWellKnownAttributeData = wellKnownAttributeData.SecurityInformation
                If securityData IsNot Nothing Then
                    netmoduleSecurityAttributes = securityData.GetSecurityAttributes(attributesBag.Attributes)
                End If
            End If

            Dim securityAttributes As IEnumerable(Of Cci.SecurityAttribute) = Nothing
            If sourceSecurityAttributes IsNot Nothing Then
                If netmoduleSecurityAttributes IsNot Nothing Then
                    securityAttributes = sourceSecurityAttributes.Concat(netmoduleSecurityAttributes)
                Else
                    securityAttributes = sourceSecurityAttributes
                End If
            Else
                If netmoduleSecurityAttributes IsNot Nothing Then
                    securityAttributes = netmoduleSecurityAttributes
                Else
                    securityAttributes = SpecializedCollections.EmptyEnumerable(Of Cci.SecurityAttribute)()
                End If
            End If

            Debug.Assert(securityAttributes IsNot Nothing)
            Return securityAttributes
        End Function

        Friend ReadOnly Property FileVersion As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyFileVersionAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property Title As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyTitleAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property Description As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyDescriptionAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property Company As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyCompanyAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property Product As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyProductAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property InformationalVersion As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyInformationalVersionAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property Copyright As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyCopyrightAttributeSetting)
            End Get
        End Property

        Friend ReadOnly Property Trademark As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyTrademarkAttributeSetting)
            End Get
        End Property

        ''' <summary>
        ''' This represents what the user claimed in source through the AssemblyFlagsAttribute.
        ''' It may be modified as emitted due to presence or absence of the public key.
        ''' </summary>
        Public ReadOnly Property AssemblyFlags As AssemblyFlags Implements ISourceAssemblySymbolInternal.AssemblyFlags
            Get
                Dim fieldValue As AssemblyFlags = Nothing

                Dim data = GetSourceDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    fieldValue = data.AssemblyFlagsAttributeSetting
                End If

                data = GetNetModuleDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    fieldValue = fieldValue Or data.AssemblyFlagsAttributeSetting
                End If

                Return fieldValue
            End Get
        End Property

        Private ReadOnly Property DelaySignAttributeSetting As Boolean
            Get
                Dim defaultValue = False
                Dim fieldValue = defaultValue

                Dim data = GetSourceDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    fieldValue = (data.AssemblyDelaySignAttributeSetting = ThreeState.True)
                End If

                If fieldValue = defaultValue Then
                    data = GetNetModuleDecodedWellKnownAttributeData()
                    If data IsNot Nothing Then
                        fieldValue = (data.AssemblyDelaySignAttributeSetting = ThreeState.True)
                    End If
                End If

                Return fieldValue
            End Get
        End Property

        Public ReadOnly Property SignatureKey As String Implements ISourceAssemblySymbolInternal.SignatureKey
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblySignatureKeyAttributeSetting)
            End Get
        End Property

        Private ReadOnly Property AssemblyKeyContainerAttributeSetting As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyKeyContainerAttributeSetting, CommonAssemblyWellKnownAttributeData.StringMissingValue)
            End Get
        End Property

        Private ReadOnly Property AssemblyKeyFileAttributeSetting As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyKeyFileAttributeSetting, CommonAssemblyWellKnownAttributeData.StringMissingValue)
            End Get
        End Property

        Private ReadOnly Property AssemblyCultureAttributeSetting As String
            Get
                Return GetWellKnownAttributeDataStringField(Function(data) data.AssemblyCultureAttributeSetting)
            End Get
        End Property

        Private ReadOnly Property AssemblyVersionAttributeSetting As Version
            Get
                Dim defaultValue As Version = Nothing
                Dim fieldValue = defaultValue

                Dim data = GetSourceDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    fieldValue = data.AssemblyVersionAttributeSetting
                End If

                If fieldValue Is defaultValue Then
                    data = GetNetModuleDecodedWellKnownAttributeData()
                    If data IsNot Nothing Then
                        fieldValue = data.AssemblyVersionAttributeSetting
                    End If
                End If

                Return fieldValue
            End Get
        End Property

        Public Overrides ReadOnly Property AssemblyVersionPattern As Version Implements ISourceAssemblySymbolInternal.AssemblyVersionPattern
            Get
                Dim attributeValue = AssemblyVersionAttributeSetting
                Return If(attributeValue Is Nothing OrElse (attributeValue.Build <> UShort.MaxValue AndAlso attributeValue.Revision <> UShort.MaxValue), Nothing, attributeValue)
            End Get
        End Property

        Public ReadOnly Property HashAlgorithm As AssemblyHashAlgorithm Implements ISourceAssemblySymbolInternal.HashAlgorithm
            Get
                Return If(AssemblyAlgorithmIdAttributeSetting, AssemblyHashAlgorithm.Sha1)
            End Get
        End Property

        Friend ReadOnly Property AssemblyAlgorithmIdAttributeSetting As AssemblyHashAlgorithm?
            Get
                Dim fieldValue As AssemblyHashAlgorithm? = Nothing

                Dim data = GetSourceDecodedWellKnownAttributeData()
                If data IsNot Nothing Then
                    fieldValue = data.AssemblyAlgorithmIdAttributeSetting
                End If

                If fieldValue Is Nothing Then
                    data = GetNetModuleDecodedWellKnownAttributeData()
                    If data IsNot Nothing Then
                        fieldValue = data.AssemblyAlgorithmIdAttributeSetting
                    End If
                End If

                Return fieldValue
            End Get
        End Property

        Private Sub EnsureAttributesAreBound()
            If _lazySourceAttributesBag Is Nothing OrElse Not _lazySourceAttributesBag.IsSealed Then
                LoadAndValidateAttributes(OneOrMany.Create(GetAttributeDeclarations()), _lazySourceAttributesBag)
            End If
        End Sub

        ' TODO: cache
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me.Modules.SelectMany(Function(m) m.Locations).AsImmutable()
            End Get
        End Property

        Public Overrides ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)
            Get
                Return _modules
            End Get
        End Property

        Friend Overrides Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return SourceModule.GetReferencedAssemblySymbols()
        End Function

        Friend Overrides Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
            ' SourceAssemblySymbol is never used directly as a reference
            ' when it is or any of its references is linked.
            Return Nothing
        End Function

        Friend Overrides Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            ' SourceAssemblySymbol is never used directly as a reference
            ' when it is or any of its references is linked.
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Overrides ReadOnly Property IsLinked As Boolean
            Get
                Return False
            End Get
        End Property

        ' Computing the identity requires computing the public key. Computing the public key 
        ' can require binding attributes that contain version or strong name information. 
        ' Attribute binding will check type visibility which will possibly 
        ' check IVT relationships. To correctly determine the IVT relationship requires the public key. 
        ' To avoid infinite recursion, this type notes, per thread, the assembly for which the thread 
        ' is actively computing the public key (s_AssemblyForWhichCurrentThreadIsComputingKeys). Should a request to determine IVT
        ' relationship occur on the thread that is computing the public key, access is optimistically
        ' granted provided the simple assembly names match. When such access is granted
        ' the assembly to which we have been granted access is noted (m_optimisticallyGrantedInternalsAccess).
        ' After the public key has been computed, the set of optimistic grants is reexamined 
        ' to ensure that full identities match. This may produce diagnostics.

        ' EDMAURER please don't use thread local storage widely. This is hoped to be a one-off usage.
        <ThreadStatic()>
        Private Shared s_AssemblyForWhichCurrentThreadIsComputingKeys As AssemblySymbol

        ' A collection of assemblies to which we were granted internals access by only checking matches for assembly name
        ' and ignoring public key. This just acts as a set. The Boolean is ignored.
        Private _optimisticallyGrantedInternalsAccess As ConcurrentDictionary(Of AssemblySymbol, Boolean)

        ' Once the computation of the AssemblyIdentity is complete, check whether
        ' any of the IVT access grants that were optimistically made during AssemblyIdentity computation
        ' are in fact invalid now that the full identity is known.
        Private Sub CheckOptimisticIVTAccessGrants(bag As BindingDiagnosticBag)
            Dim haveGrantedAssemblies As ConcurrentDictionary(Of AssemblySymbol, Boolean) = _optimisticallyGrantedInternalsAccess

            If haveGrantedAssemblies IsNot Nothing Then
                For Each otherAssembly In haveGrantedAssemblies.Keys
                    Dim conclusion As IVTConclusion = MakeFinalIVTDetermination(otherAssembly)

                    Debug.Assert(conclusion <> IVTConclusion.NoRelationshipClaimed)

                    If conclusion = IVTConclusion.PublicKeyDoesntMatch Then
                        bag.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_FriendRefNotEqualToThis,
                                                                      otherAssembly.Identity,
                                                                        Me.Identity),
                                                                  NoLocation.Singleton))
                    ElseIf conclusion = IVTConclusion.OneSignedOneNot Then
                        bag.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_FriendRefSigningMismatch,
                                                                      otherAssembly.Identity,
                                                                        Me.Identity),
                                                                  NoLocation.Singleton))
                    End If
                Next
            End If
        End Sub

        Friend Overrides Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))

            'EDMAURER assume that if EnsureAttributesAreBound() returns, then the internals visible to map has been populated.
            'Do not optimize by checking if m_lazyInternalsVisibleToMap is Nothing. It may be non-null yet still
            'incomplete because another thread is in the process of building it.
            EnsureAttributesAreBound()

            If _lazyInternalsVisibleToMap Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of ImmutableArray(Of Byte))()
            End If

            Dim result As ConcurrentDictionary(Of ImmutableArray(Of Byte), Tuple(Of Location, String)) = Nothing

            _lazyInternalsVisibleToMap.TryGetValue(simpleName, result)

            Return If(result IsNot Nothing, result.Keys, SpecializedCollections.EmptyEnumerable(Of ImmutableArray(Of Byte))())
        End Function

        Friend Overrides Function GetInternalsVisibleToAssemblyNames() As IEnumerable(Of String)
            EnsureAttributesAreBound()

            If _lazyInternalsVisibleToMap Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of String)()
            End If

            Return _lazyInternalsVisibleToMap.Keys
        End Function

        Friend ReadOnly Property DeclaresTheObjectClass As Boolean
            Get
                If Me.CorLibrary IsNot Me Then
                    Return False
                End If

                Dim obj = GetSpecialType(SpecialType.System_Object)

                Return Not obj.IsErrorType() AndAlso obj.DeclaredAccessibility = Accessibility.Public
            End Get
        End Property

        Friend ReadOnly Property SourceModule As SourceModuleSymbol
            Get
                Return DirectCast(_modules(0), SourceModuleSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                ' Only primary module of an assembly marked with an Extension attribute
                ' can contain extension methods recognized by the language (Dev10 behavior).
                Return SourceModule.MightContainExtensionMethods
            End Get
        End Property

        Private Function ProcessOneInternalsVisibleToAttribute(nodeOpt As AttributeSyntax, attrData As VisualBasicAttributeData, diagnostics As BindingDiagnosticBag) As Boolean
            'assume that this code won't be called unless we bound a well-formed, semantically
            'correct ctor call.
            Dim displayName As String = TryCast(attrData.CommonConstructorArguments(0).ValueInternal, String)

            If displayName Is Nothing Then
                diagnostics.Add(ERRID.ERR_FriendAssemblyNameInvalid, If(nodeOpt IsNot Nothing, nodeOpt.GetLocation(), NoLocation.Singleton), displayName)
                Return False
            End If

            Dim identity As AssemblyIdentity = Nothing
            Dim parts As AssemblyIdentityParts = Nothing
            If Not AssemblyIdentity.TryParseDisplayName(displayName, identity, parts) Then
                diagnostics.Add(ERRID.ERR_FriendAssemblyNameInvalid, If(nodeOpt IsNot Nothing, nodeOpt.GetLocation(), NoLocation.Singleton), displayName)
                Return False
            End If

            ' Allow public key token due to compatibility reasons, but we are not going to use its value.
            Const allowedParts = AssemblyIdentityParts.Name Or AssemblyIdentityParts.PublicKey Or AssemblyIdentityParts.PublicKeyToken

            If (parts And Not allowedParts) <> 0 Then
                diagnostics.Add(ERRID.ERR_FriendAssemblyBadArguments, If(nodeOpt IsNot Nothing, nodeOpt.GetLocation(), NoLocation.Singleton), displayName)
                Return False
            End If

            If _lazyInternalsVisibleToMap Is Nothing Then
                Interlocked.CompareExchange(_lazyInternalsVisibleToMap,
                                            New ConcurrentDictionary(Of String, ConcurrentDictionary(Of ImmutableArray(Of Byte), Tuple(Of Location, String)))(StringComparer.OrdinalIgnoreCase), Nothing)
            End If

            'later, once the identity is established we confirm that if the assembly being 
            'compiled is signed all of the IVT attributes specify a key. Stash the location for that
            ' in the event that a diagnostic needs to be produced.

            Dim locationAndValue As Tuple(Of Location, String) = Nothing

            ' only need to store anything when there is no public key. The only reason to store
            ' this stuff is for production of errors when the assembly is signed but the IVT attrib
            ' doesn't contain a public key.
            If identity.PublicKey.IsEmpty Then
                locationAndValue = New Tuple(Of Location, String)(If(nodeOpt IsNot Nothing, nodeOpt.GetLocation(), NoLocation.Singleton), displayName)
            End If

            'when two threads are attempting to update the internalsVisibleToMap one of these TryAdd()
            'calls can fail. We assume that the 'other' thread in that case will successfully add the same
            'contents eventually.
            Dim keys As ConcurrentDictionary(Of ImmutableArray(Of Byte), Tuple(Of Location, String)) = Nothing
            If _lazyInternalsVisibleToMap.TryGetValue(identity.Name, keys) Then
                keys.TryAdd(identity.PublicKey, locationAndValue)
            Else
                keys = New ConcurrentDictionary(Of ImmutableArray(Of Byte), Tuple(Of Location, String))
                keys.TryAdd(identity.PublicKey, locationAndValue)
                _lazyInternalsVisibleToMap.TryAdd(identity.Name, keys)
            End If

            Return True
        End Function

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim attrData = arguments.Attribute
            Debug.Assert(Not attrData.HasErrors)
            Debug.Assert(arguments.SymbolPart = AttributeLocation.None)
            Dim diagnostics = DirectCast(arguments.Diagnostics, BindingDiagnosticBag)

            If attrData.IsTargetAttribute(Me, AttributeDescription.CaseInsensitiveExtensionAttribute) Then
                ' Already have an attribute, no need to add another one.
                Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.True)
                _lazyEmitExtensionAttribute = ThreeState.False
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.InternalsVisibleToAttribute) Then
                ProcessOneInternalsVisibleToAttribute(arguments.AttributeSyntaxOpt, attrData, diagnostics)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblySignatureKeyAttribute) Then
                Dim signatureKey = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblySignatureKeyAttributeSetting = signatureKey

                If Not StrongNameKeys.IsValidPublicKeyString(signatureKey) Then
                    diagnostics.Add(ERRID.ERR_InvalidSignaturePublicKey, GetAssemblyAttributeFirstArgumentLocation(arguments.AttributeSyntaxOpt))
                End If

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyKeyFileAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyKeyFileAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyKeyNameAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyKeyContainerAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyDelaySignAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyDelaySignAttributeSetting = If(DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, Boolean), ThreeState.True, ThreeState.False)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyVersionAttribute) Then
                Dim verString = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
                Dim version As Version = Nothing
                If Not VersionHelper.TryParseAssemblyVersion(verString, allowWildcard:=Not _compilation.IsEmitDeterministic, version:=version) Then
                    diagnostics.Add(ERRID.ERR_InvalidVersionFormat, GetAssemblyAttributeFirstArgumentLocation(arguments.AttributeSyntaxOpt))
                End If
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyVersionAttributeSetting = version
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyFileVersionAttribute) Then
                Dim dummy As Version = Nothing
                Dim verString = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
                If Not VersionHelper.TryParse(verString, version:=dummy) Then
                    diagnostics.Add(ERRID.WRN_InvalidVersionFormat, GetAssemblyAttributeFirstArgumentLocation(arguments.AttributeSyntaxOpt))
                End If

                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyFileVersionAttributeSetting = verString
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyInformationalVersionAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyInformationalVersionAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyTitleAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyTitleAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyDescriptionAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyDescriptionAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyCultureAttribute) Then
                Dim cultureString = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
                If Not String.IsNullOrEmpty(cultureString) Then
                    If Me.DeclaringCompilation.Options.OutputKind.IsApplication() Then
                        diagnostics.Add(ERRID.ERR_InvalidAssemblyCultureForExe, GetAssemblyAttributeFirstArgumentLocation(arguments.AttributeSyntaxOpt))
                    ElseIf Not AssemblyIdentity.IsValidCultureName(cultureString) Then
                        diagnostics.Add(ERRID.ERR_InvalidAssemblyCulture, GetAssemblyAttributeFirstArgumentLocation(arguments.AttributeSyntaxOpt))
                        cultureString = Nothing
                    End If
                End If

                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyCultureAttributeSetting = cultureString
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyCompanyAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyCompanyAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyProductAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyProductAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyInformationalVersionAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyInformationalVersionAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SatelliteContractVersionAttribute) Then
                'just check the format of this one, don't do anything else with it.
                Dim dummy As Version = Nothing
                Dim verString = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
                If Not VersionHelper.TryParseAssemblyVersion(verString, allowWildcard:=False, version:=dummy) Then
                    diagnostics.Add(ERRID.ERR_InvalidVersionFormat2, GetAssemblyAttributeFirstArgumentLocation(arguments.AttributeSyntaxOpt))
                End If
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyCopyrightAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyCopyrightAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.AssemblyTrademarkAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyTrademarkAttributeSetting = DirectCast(attrData.CommonConstructorArguments(0).ValueInternal, String)
            ElseIf attrData.IsSecurityAttribute(Me.DeclaringCompilation) Then
                attrData.DecodeSecurityAttribute(Of CommonAssemblyWellKnownAttributeData)(Me, Me.DeclaringCompilation, arguments)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ClassInterfaceAttribute) Then
                attrData.DecodeClassInterfaceAttribute(arguments.AttributeSyntaxOpt, diagnostics)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.TypeLibVersionAttribute) Then
                ValidateIntegralAttributeNonNegativeArguments(attrData, arguments.AttributeSyntaxOpt, diagnostics)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ComCompatibleVersionAttribute) Then
                ValidateIntegralAttributeNonNegativeArguments(attrData, arguments.AttributeSyntaxOpt, diagnostics)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.GuidAttribute) Then
                attrData.DecodeGuidAttribute(arguments.AttributeSyntaxOpt, diagnostics)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.CompilationRelaxationsAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().HasCompilationRelaxationsAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ReferenceAssemblyAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().HasReferenceAssemblyAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.RuntimeCompatibilityAttribute) Then
                ' VB doesn't need to decode argument values
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().RuntimeCompatibilityWrapNonExceptionThrows = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.DebuggableAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().HasDebuggableAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ExperimentalAttribute) Then
                arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().ExperimentalAttributeData = attrData.DecodeExperimentalAttribute()
            Else
                Dim signature As Integer = attrData.GetTargetAttributeSignatureIndex(Me, AttributeDescription.AssemblyAlgorithmIdAttribute)

                If signature <> -1 Then
                    Dim value As Object = attrData.CommonConstructorArguments(0).ValueInternal
                    Dim algorithmId As AssemblyHashAlgorithm

                    If signature = 0 Then
                        algorithmId = CType(value, AssemblyHashAlgorithm)
                    Else
                        algorithmId = CType(CUInt(value), AssemblyHashAlgorithm)
                    End If

                    arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyAlgorithmIdAttributeSetting = algorithmId
                Else
                    signature = attrData.GetTargetAttributeSignatureIndex(Me, AttributeDescription.AssemblyFlagsAttribute)

                    If signature <> -1 Then
                        Dim value As Object = attrData.CommonConstructorArguments(0).ValueInternal
                        Dim nameFlags As AssemblyFlags

                        If signature = 0 OrElse signature = 1 Then
                            nameFlags = CType(CType(value, AssemblyNameFlags), AssemblyFlags)
                        Else
                            nameFlags = CType(CUInt(value), AssemblyFlags)
                        End If

                        arguments.GetOrCreateData(Of CommonAssemblyWellKnownAttributeData)().AssemblyFlagsAttributeSetting = nameFlags
                    End If
                End If
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Private Shared Function GetAssemblyAttributeFirstArgumentLocation(attributeSyntaxOpt As AttributeSyntax) As Location
            If attributeSyntaxOpt Is Nothing Then
                Return NoLocation.Singleton
            End If

            Return attributeSyntaxOpt.ArgumentList.Arguments.First().GetLocation()
        End Function

        ' Checks that the integral arguments for the given well-known attribute are non-negative.
        Private Sub ValidateIntegralAttributeNonNegativeArguments(attrData As VisualBasicAttributeData, nodeOpt As AttributeSyntax, diagnostics As BindingDiagnosticBag)
            Debug.Assert(Not attrData.HasErrors)

            Dim argCount As Integer = attrData.CommonConstructorArguments.Length
            For i = 0 To argCount - 1
                Dim arg As Integer = attrData.GetConstructorArgument(Of Integer)(i, SpecialType.System_Int32)
                If arg < 0 Then
                    diagnostics.Add(ERRID.ERR_BadAttribute1, VisualBasicAttributeData.GetArgumentLocation(nodeOpt, i), attrData.AttributeClass)
                End If
            Next
        End Sub

        Friend Sub AnErrorHasBeenReportedAboutExtensionAttribute()
            ' Note, we are storing false because, even though we might be required to emit the attribute,
            ' we can't do that due to the error that we just reported.
            Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.True)
            _lazyEmitExtensionAttribute = ThreeState.False
        End Sub

        Friend Sub GetAllDeclarationErrors(diagnostics As BindingDiagnosticBag, Optional cancellationToken As CancellationToken = Nothing)
            Dim hasExtensionMethods As Boolean = False
            SourceModule.GetAllDeclarationErrors(diagnostics, cancellationToken, hasExtensionMethods)
            diagnostics.AddRange(GetAssemblyLevelDeclarationErrors(hasExtensionMethods), allowMismatchInDependencyAccumulation:=True)
        End Sub

        ''' <summary>
        ''' Get assembly level declaration errors.
        ''' </summary>
        Private Function GetAssemblyLevelDeclarationErrors(
            haveExtensionMethodsInSource As Boolean) As ImmutableBindingDiagnostic(Of AssemblySymbol)

            If _lazyAssemblyLevelDeclarationErrors.IsDefault OrElse _lazyAssemblyLevelDeclarationDependencies.IsDefault Then

                Dim diagnostics = BindingDiagnosticBag.GetInstance()

                Dim emitExtensionAttribute As ThreeState = CType(_lazyEmitExtensionAttribute, ThreeState)
                If emitExtensionAttribute = ThreeState.Unknown Then

                    Dim needAttribute As Boolean = haveExtensionMethodsInSource

                    If Not needAttribute Then
                        emitExtensionAttribute = ThreeState.False
                    Else
                        ' We need to emit an Extension attribute on the assembly. 
                        ' Can we locate it?
                        Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing
                        _compilation.GetExtensionAttributeConstructor(useSiteInfo:=useSiteInfo)

                        If useSiteInfo.DiagnosticInfo IsNot Nothing Then
                            ' Note, we are storing false because, even though we should emit the attribute,
                            ' we can't do that due to the use site error.
                            ' The diagnostic itself was already reported at the location where the attribute was applied to.
                            ' Reporting it also on a place where it's implicitly used would not be expected by developers.
                            emitExtensionAttribute = ThreeState.False
                        Else
                            emitExtensionAttribute = ThreeState.True
                        End If
                    End If
                End If

                Debug.Assert(_lazyEmitExtensionAttribute = ThreeState.Unknown OrElse
                             _lazyEmitExtensionAttribute = emitExtensionAttribute)

                _lazyEmitExtensionAttribute = emitExtensionAttribute

                'strong name key settings are not validated when building netmodules.
                'They are validated when the netmodule is added to an assembly.
                If StrongNameKeys.DiagnosticOpt IsNot Nothing AndAlso Not IsNetModule Then
                    diagnostics.Add(StrongNameKeys.DiagnosticOpt)
                End If

                ValidateIVTPublicKeys(diagnostics)
                CheckOptimisticIVTAccessGrants(diagnostics)

                DetectAttributeAndOptionConflicts(diagnostics)

                If IsDelaySigned AndAlso Not Identity.HasPublicKey Then
                    diagnostics.Add(ERRID.WRN_DelaySignButNoKey, NoLocation.Singleton)
                End If

                If DeclaringCompilation.Options.PublicSign Then
                    If IsNetModule Then
                        diagnostics.Add(ERRID.ERR_PublicSignNetModule, NoLocation.Singleton)
                    ElseIf Not Identity.HasPublicKey Then
                        diagnostics.Add(ERRID.ERR_PublicSignNoKey, NoLocation.Singleton)
                    End If
                End If

                ' If the options and attributes applied on the compilation imply real signing,
                ' but we have no private key to sign it with report an error.
                ' Note that if public key is set and delay sign is off we do OSS signing, which doesn't require private key.
                ' Consider: should we allow to OSS sign if the key file only contains public key?

                If DeclaringCompilation.Options.OutputKind <> OutputKind.NetModule AndAlso
                   DeclaringCompilation.Options.CryptoPublicKey.IsEmpty AndAlso
                   Identity.HasPublicKey AndAlso
                   Not IsDelaySigned AndAlso
                   Not DeclaringCompilation.Options.PublicSign AndAlso
                   Not StrongNameKeys.CanSign Then

                    ' Since the container always contains both keys, the problem is that the key file didn't contain private key.
                    diagnostics.Add(ERRID.ERR_SignButNoPrivateKey, NoLocation.Singleton, StrongNameKeys.KeyFilePath)
                End If

                ReportDiagnosticsForSynthesizedAttributes(DeclaringCompilation, diagnostics)
                ReportDiagnosticsForAddedModules(diagnostics)

                Dim immutableBindingDiagnostic As ImmutableBindingDiagnostic(Of AssemblySymbol) = diagnostics.ToReadOnlyAndFree()
                ImmutableInterlocked.InterlockedInitialize(_lazyAssemblyLevelDeclarationDependencies, immutableBindingDiagnostic.Dependencies)
                ImmutableInterlocked.InterlockedInitialize(_lazyAssemblyLevelDeclarationErrors, immutableBindingDiagnostic.Diagnostics)
            End If

            Debug.Assert(Not _lazyAssemblyLevelDeclarationErrors.IsDefault)
            Debug.Assert(Not _lazyAssemblyLevelDeclarationDependencies.IsDefault)

            Return New ImmutableBindingDiagnostic(Of AssemblySymbol)(_lazyAssemblyLevelDeclarationErrors, _lazyAssemblyLevelDeclarationDependencies)
        End Function

        Private Sub DetectAttributeAndOptionConflicts(diagnostics As BindingDiagnosticBag)
            EnsureAttributesAreBound()

            If _compilation.Options.PublicSign AndAlso DelaySignAttributeSetting Then
                diagnostics.Add(ERRID.ERR_CmdOptionConflictsSource, NoLocation.Singleton,
                                AttributeDescription.AssemblyDelaySignAttribute.FullName,
                                NameOf(_compilation.Options.PublicSign))
            End If

            If _compilation.Options.OutputKind = OutputKind.NetModule Then
                If Not String.IsNullOrEmpty(_compilation.Options.CryptoKeyContainer) Then
                    Dim assemblyKeyContainerAttributeSetting As String = Me.AssemblyKeyContainerAttributeSetting

                    If assemblyKeyContainerAttributeSetting Is CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                        ' We need to synthesize this attribute for .NET module,
                        ' touch the constructor in order to generate proper use-site diagnostics
                        Binder.ReportUseSiteInfoForSynthesizedAttribute(
                            WellKnownMember.System_Reflection_AssemblyKeyNameAttribute__ctor,
                            _compilation,
                            NoLocation.Singleton,
                            diagnostics)

                    ElseIf String.Compare(_compilation.Options.CryptoKeyContainer, assemblyKeyContainerAttributeSetting, StringComparison.OrdinalIgnoreCase) <> 0 Then
                        ' If we are building a .NET module, things get more complicated. In particular, we don't sign the module, we emit an attribute with the key 
                        ' information, which will be used to sign an assembly once the module is linked into it. If there is already an attribute like that in source,
                        ' native compiler emits both of them, synthetic attribute is emitted after the one from source. Incidentally, ALink picks the last attribute
                        ' for signing and things seem to work out. However, relying on the order of attributes feels fragile, especially given that Roslyn emits
                        ' synthetic attributes before attributes from source. The behavior we settled on for .NET modules is that, if the attribute in source has the
                        ' same value as the one in compilation options, we won't emit the synthetic attribute. If the value doesn't match, we report an error, which 
                        ' is a breaking change. Bottom line, we will never produce a module or an assembly with two attributes, regardless whether values are the same
                        ' or not.
                        diagnostics.Add(ERRID.ERR_CmdOptionConflictsSource, NoLocation.Singleton, AttributeDescription.AssemblyKeyNameAttribute.FullName, "CryptoKeyContainer")
                    End If
                End If

                If Not String.IsNullOrEmpty(_compilation.Options.CryptoKeyFile) Then
                    Dim assemblyKeyFileAttributeSetting As String = Me.AssemblyKeyFileAttributeSetting

                    If assemblyKeyFileAttributeSetting Is CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                        ' We need to synthesize this attribute for .NET module,
                        ' touch the constructor in order to generate proper use-site diagnostics
                        Binder.ReportUseSiteInfoForSynthesizedAttribute(
                            WellKnownMember.System_Reflection_AssemblyKeyFileAttribute__ctor,
                            _compilation,
                            NoLocation.Singleton,
                            diagnostics)

                    ElseIf String.Compare(_compilation.Options.CryptoKeyFile, assemblyKeyFileAttributeSetting, StringComparison.OrdinalIgnoreCase) <> 0 Then
                        ' Comment in similar section for CryptoKeyContainer is applicable here as well.
                        diagnostics.Add(ERRID.ERR_CmdOptionConflictsSource, NoLocation.Singleton, AttributeDescription.AssemblyKeyFileAttribute.FullName, "CryptoKeyFile")
                    End If
                End If
            ElseIf _compilation.Options.PublicSign Then
                If Me.AssemblyKeyContainerAttributeSetting IsNot CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                    diagnostics.Add(ERRID.WRN_AttributeIgnoredWhenPublicSigning, NoLocation.Singleton, AttributeDescription.AssemblyKeyNameAttribute.FullName)
                End If

                If Me.AssemblyKeyFileAttributeSetting IsNot CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                    diagnostics.Add(ERRID.WRN_AttributeIgnoredWhenPublicSigning, NoLocation.Singleton, AttributeDescription.AssemblyKeyFileAttribute.FullName)
                End If
            End If
        End Sub

        Private Sub ReportDiagnosticsForAddedModules(diagnostics As BindingDiagnosticBag)
            For Each pair In _compilation.GetBoundReferenceManager().ReferencedModuleIndexMap
                Dim fileRef = TryCast(pair.Key, PortableExecutableReference)

                If fileRef IsNot Nothing AndAlso fileRef.FilePath IsNot Nothing Then
                    Dim fileName As String = FileNameUtilities.GetFileName(fileRef.FilePath)
                    Dim moduleName As String = _modules(pair.Value).Name

                    If Not String.Equals(fileName, moduleName, StringComparison.OrdinalIgnoreCase) Then
                        ' Used to be ERR_UnableToEmitAssembly
                        diagnostics.Add(ERRID.ERR_NetModuleNameMismatch, NoLocation.Singleton, moduleName, fileName)
                    End If
                End If
            Next

            ' Alink performed these checks only when emitting an assembly.
            If _modules.Length > 1 AndAlso Not _compilation.Options.OutputKind.IsNetModule() Then
                Dim assemblyMachine = Me.Machine
                Dim isPlatformAgnostic As Boolean = (assemblyMachine = PortableExecutable.Machine.I386 AndAlso Not Me.Bit32Required)
                Dim knownModuleNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                For i As Integer = 1 To Modules.Length - 1
                    Dim m As ModuleSymbol = Modules(i)
                    If Not knownModuleNames.Add(m.Name) Then
                        diagnostics.Add(ERRID.ERR_NetModuleNameMustBeUnique, NoLocation.Singleton, m.Name)
                    End If

                    If Not DirectCast(m, PEModuleSymbol).Module.IsCOFFOnly Then
                        Dim moduleMachine = m.Machine

                        If moduleMachine = PortableExecutable.Machine.I386 AndAlso Not m.Bit32Required Then
                            ' Other module is agnostic, this is always safe

                        ElseIf isPlatformAgnostic Then
                            diagnostics.Add(ERRID.ERR_AgnosticToMachineModule, NoLocation.Singleton, m)
                        ElseIf assemblyMachine <> moduleMachine Then
                            ' Different machine types, and neither is agnostic
                            ' So it is a conflict
                            diagnostics.Add(ERRID.ERR_ConflictingMachineModule, NoLocation.Singleton, m)
                        End If
                    End If
                Next

                ' Assembly main module must explicitly reference all the modules referenced by other assembly 
                ' modules, i.e. all modules from transitive closure must be referenced explicitly here
                For i As Integer = 1 To Modules.Length - 1
                    Dim m = DirectCast(Modules(i), PEModuleSymbol)

                    Try
                        For Each referencedModuleName In m.Module.GetReferencedManagedModulesOrThrow()
                            ' Do not report error for this module twice
                            If knownModuleNames.Add(referencedModuleName) Then
                                diagnostics.Add(ERRID.ERR_MissingNetModuleReference, NoLocation.Singleton, referencedModuleName)
                            End If
                        Next
                    Catch mrEx As BadImageFormatException
                        diagnostics.Add(ERRID.ERR_UnsupportedModule1, NoLocation.Singleton, m)
                    End Try
                Next
            End If
        End Sub

        Friend ReadOnly Property IsDelaySigned As Boolean
            Get
                EnsureAttributesAreBound()
                'TODO need to figure out the right behavior when command line and 
                'attribute value conflict. Does command line setting need to be three-valued?
                If (DeclaringCompilation.Options.DelaySign.HasValue) Then
                    Return DeclaringCompilation.Options.DelaySign.Value
                End If

                Return DelaySignAttributeSetting
            End Get
        End Property

        Protected Sub ValidateIVTPublicKeys(diagnostics As BindingDiagnosticBag)
            EnsureAttributesAreBound()

            If Not Me.Identity.IsStrongName Then
                Return
            End If

            If _lazyInternalsVisibleToMap IsNot Nothing Then
                For Each keys In _lazyInternalsVisibleToMap.Values
                    For Each oneKey In keys
                        If oneKey.Key.IsDefaultOrEmpty Then
                            diagnostics.Add(ERRID.ERR_FriendAssemblyStrongNameRequired, oneKey.Value.Item1, oneKey.Value.Item2)
                        End If
                    Next
                Next
            End If
        End Sub

        ''' <summary>
        ''' True if internals are exposed at all.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' This property shouldn't be accessed during binding as it can lead to attribute binding cycle.
        ''' </remarks>
        Public ReadOnly Property InternalsAreVisible As Boolean Implements ISourceAssemblySymbolInternal.InternalsAreVisible
            Get
                EnsureAttributesAreBound()
                Return _lazyInternalsVisibleToMap IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' We may synthesize some well-known attributes for this assembly symbol.  However, at synthesis time, it is
        ''' too late to report diagnostics or cancel the emit.  Instead, we check for use site errors on the types and members
        ''' we know we'll need at synthesis time.
        ''' </summary>
        Private Shared Sub ReportDiagnosticsForSynthesizedAttributes(compilation As VisualBasicCompilation, diagnostics As BindingDiagnosticBag)
            ' May need to synthesize CompilationRelaxationsAttribute and/or RuntimeCompatibilityAttribute if we are not building a net-module.
            ' NOTE: Native compiler skips synthesizing these attributes if the respective well-known attribute types aren't available, we do the same.

            Dim compilationOptions As VisualBasicCompilationOptions = compilation.Options
            If Not compilationOptions.OutputKind.IsNetModule() Then
                Dim compilationRelaxationsAttributeType = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute)
                If TryCast(compilationRelaxationsAttributeType, MissingMetadataTypeSymbol) Is Nothing Then
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32, compilation, NoLocation.Singleton, diagnostics)
                End If

                Dim runtimeCompatibilityAttributeType = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute)
                If TryCast(runtimeCompatibilityAttributeType, MissingMetadataTypeSymbol) Is Nothing Then
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor, compilation, NoLocation.Singleton, diagnostics)
                    Binder.ReportUseSiteInfoForSynthesizedAttribute(WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows, compilation, NoLocation.Singleton, diagnostics)
                End If
            End If
        End Sub

        Private ReadOnly Property HasAssemblyOrModuleDebuggableAttribute As Boolean
            Get
                Dim assemblyData As CommonAssemblyWellKnownAttributeData = Me.GetSourceDecodedWellKnownAttributeData()
                If assemblyData IsNot Nothing AndAlso assemblyData.HasDebuggableAttribute Then
                    Return True
                End If

                Dim moduleData As CommonModuleWellKnownAttributeData = Me.SourceModule.GetDecodedWellKnownAttributeData()
                If moduleData IsNot Nothing AndAlso moduleData.HasDebuggableAttribute Then
                    Return True
                End If

                Return False
            End Get
        End Property

        Private ReadOnly Property HasReferenceAssemblyAttribute As Boolean
            Get
                Dim assemblyData As CommonAssemblyWellKnownAttributeData = Me.GetSourceDecodedWellKnownAttributeData()
                Return assemblyData IsNot Nothing AndAlso assemblyData.HasReferenceAssemblyAttribute
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.Unknown)
            Debug.Assert(_lazySourceAttributesBag.IsSealed)

            Dim options As VisualBasicCompilationOptions = Me.DeclaringCompilation.Options
            Dim isBuildingNetModule As Boolean = options.OutputKind.IsNetModule()

            Dim emitExtensionAttribute As Boolean = _lazyEmitExtensionAttribute = ThreeState.True

            If emitExtensionAttribute Then
                AddSynthesizedAttribute(attributes, _compilation.SynthesizeExtensionAttribute())
            End If

            ' Note that manager's collection of referenced symbols may not be sealed 
            ' yet in case this and previous emits didn't emit IL, but only metadata
            Dim emitEmbeddedAttribute As Boolean = Me.DeclaringCompilation.EmbeddedSymbolManager.IsAnySymbolReferenced

            If emitEmbeddedAttribute Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.Microsoft_VisualBasic_Embedded__ctor))
            End If

            ' Synthesize CompilationRelaxationsAttribute only if all the following requirements are met:
            ' (a) We are not building a netmodule.
            ' (b) There is no applied CompilationRelaxationsAttribute assembly attribute in source.
            ' (c) There is no applied CompilationRelaxationsAttribute assembly attribute for any of the added PE modules.
            ' Above requirements also hold for synthesizing RuntimeCompatibilityAttribute attribute.

            Dim emitCompilationRelaxationsAttribute As Boolean = Not isBuildingNetModule AndAlso Not Me.Modules.Any(Function(m) m.HasAssemblyCompilationRelaxationsAttribute)

            If emitCompilationRelaxationsAttribute Then
                ' Synthesize attribute: <CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)>
                ' NOTE: Native compiler skips synthesizing these attributes if the respective well-known attribute types aren't available, we do the same.

                Dim compilationRelaxationsAttributeType = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CompilationRelaxationsAttribute)
                If TryCast(compilationRelaxationsAttributeType, MissingMetadataTypeSymbol) Is Nothing Then
                    Dim int32Type = Me.DeclaringCompilation.GetSpecialType(SpecialType.System_Int32)
                    Debug.Assert(int32Type.GetUseSiteInfo().DiagnosticInfo Is Nothing, "Use site errors should have been checked ahead of time (type int).")
                    Dim typedConstantNoStringInterning = New TypedConstant(int32Type, TypedConstantKind.Primitive, Cci.Constants.CompilationRelaxations_NoStringInterning)

                    AddSynthesizedAttribute(attributes, DeclaringCompilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_CompilerServices_CompilationRelaxationsAttribute__ctorInt32,
                        ImmutableArray.Create(typedConstantNoStringInterning)))
                End If
            End If

            Dim emitRuntimeCompatibilityAttribute As Boolean = Not isBuildingNetModule AndAlso Not Me.Modules.Any(Function(m) m.HasAssemblyRuntimeCompatibilityAttribute)

            If emitRuntimeCompatibilityAttribute Then
                ' Synthesize attribute: <RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)>
                ' NOTE: Native compiler skips synthesizing these attributes if the respective well-known attribute types aren't available, we do the same.

                Dim runtimeCompatibilityAttributeType = DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute)
                If TryCast(runtimeCompatibilityAttributeType, MissingMetadataTypeSymbol) Is Nothing Then
                    Dim boolType = Me.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean)
                    Debug.Assert(boolType.GetUseSiteInfo().DiagnosticInfo Is Nothing, "Use site errors should have been checked ahead of time (type bool).")
                    Dim typedConstantTrue = New TypedConstant(boolType, TypedConstantKind.Primitive, True)

                    AddSynthesizedAttribute(attributes, DeclaringCompilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__ctor,
                        ImmutableArray(Of TypedConstant).Empty,
                        ImmutableArray.Create(New KeyValuePair(Of WellKnownMember, TypedConstant)(
                            WellKnownMember.System_Runtime_CompilerServices_RuntimeCompatibilityAttribute__WrapNonExceptionThrows, typedConstantTrue))))
                End If
            End If

            ' Synthesize DebuggableAttribute only if all the following requirements are met:
            ' (a) We are not building a netmodule.
            ' (b) We are emitting debug information (full or pdbonly).
            ' (c) There is no applied DebuggableAttribute assembly attribute in source.
            ' (d) There is no applied DebuggableAttribute module attribute in source (NOTE: Native C# compiler and Roslyn C# compiler doesn't check this).

            If Not isBuildingNetModule AndAlso Not Me.HasAssemblyOrModuleDebuggableAttribute Then
                ' Synthesize attribute: <DebuggableAttribute(DebuggableAttribute.DebuggingMode.<Value>)>

                Dim int32Type = Me.DeclaringCompilation.GetSpecialType(SpecialType.System_Int32)
                If int32Type.GetUseSiteInfo().DiagnosticInfo Is Nothing Then
                    Dim debuggingMode = DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints

                    ' Since .NET 2.0 the combinations of None, Default And DisableOptimizations have the following effect
                    ' 
                    ' None                                         JIT optimizations enabled
                    ' Default                                      JIT optimizations enabled
                    ' DisableOptimizations                         JIT optimizations enabled
                    ' Default | DisableOptimizations               JIT optimizations disabled
                    If options.OptimizationLevel = OptimizationLevel.Debug Then
                        debuggingMode = debuggingMode Or DebuggableAttribute.DebuggingModes.Default Or
                                                         DebuggableAttribute.DebuggingModes.DisableOptimizations
                    End If

                    If options.EnableEditAndContinue Then
                        debuggingMode = debuggingMode Or DebuggableAttribute.DebuggingModes.EnableEditAndContinue
                    End If

                    Dim typedConstantDebugMode = New TypedConstant(int32Type, TypedConstantKind.Enum, CInt(debuggingMode))

                    AddSynthesizedAttribute(attributes, DeclaringCompilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Diagnostics_DebuggableAttribute__ctorDebuggingModes,
                        ImmutableArray.Create(typedConstantDebugMode)))
                End If
            End If

            If _compilation.Options.OutputKind = OutputKind.NetModule Then
                ' If the attribute is applied in source, do not add synthetic one.
                ' If its value is different from the supplied through options, an error should have been reported by now.

                If Not String.IsNullOrEmpty(_compilation.Options.CryptoKeyContainer) AndAlso
                   AssemblyKeyContainerAttributeSetting Is CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                    Dim stringType = _compilation.GetSpecialType(SpecialType.System_String)
                    Debug.Assert(stringType.GetUseSiteInfo.DiagnosticInfo Is Nothing, "Use site errors should have been checked ahead of time (type string).")

                    Dim typedConstant = New TypedConstant(stringType, TypedConstantKind.Primitive, _compilation.Options.CryptoKeyContainer)
                    AddSynthesizedAttribute(attributes, _compilation.TrySynthesizeAttribute(WellKnownMember.System_Reflection_AssemblyKeyNameAttribute__ctor, ImmutableArray.Create(typedConstant)))
                End If

                If Not String.IsNullOrEmpty(_compilation.Options.CryptoKeyFile) AndAlso
                   AssemblyKeyFileAttributeSetting Is CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                    Dim stringType = _compilation.GetSpecialType(SpecialType.System_String)
                    Debug.Assert(stringType.GetUseSiteInfo.DiagnosticInfo Is Nothing, "Use site errors should have been checked ahead of time (type string).")

                    Dim typedConstant = New TypedConstant(stringType, TypedConstantKind.Primitive, _compilation.Options.CryptoKeyFile)
                    AddSynthesizedAttribute(attributes, _compilation.TrySynthesizeAttribute(WellKnownMember.System_Reflection_AssemblyKeyFileAttribute__ctor, ImmutableArray.Create(typedConstant)))
                End If
            End If
        End Sub

        Friend Overrides ReadOnly Property PublicKey As ImmutableArray(Of Byte)
            Get
                Return StrongNameKeys.PublicKey
            End Get
        End Property

        Friend Overrides Function AreInternalsVisibleToThisAssembly(potentialGiverOfAccess As AssemblySymbol) As Boolean
            ' Ensure that optimistic IVT access is only granted to requests that originated on the thread
            ' that is trying to compute the assembly identity. This gives us deterministic behavior when
            ' two threads are checking IVT access but only one of them is in the process of computing identity.

            'as an optimization confirm that the identity has not yet been computed to avoid testing TLS
            If _lazyStrongNameKeys Is Nothing Then
                Dim assemblyWhoseKeysAreBeingComputed = s_AssemblyForWhichCurrentThreadIsComputingKeys
                If assemblyWhoseKeysAreBeingComputed IsNot Nothing Then
                    Debug.Assert(assemblyWhoseKeysAreBeingComputed Is Me)
                    If Not potentialGiverOfAccess.GetInternalsVisibleToPublicKeys(Me.Name).IsEmpty() Then
                        If _optimisticallyGrantedInternalsAccess Is Nothing Then
                            Interlocked.CompareExchange(_optimisticallyGrantedInternalsAccess, New ConcurrentDictionary(Of AssemblySymbol, Boolean), Nothing)
                        End If

                        _optimisticallyGrantedInternalsAccess.TryAdd(potentialGiverOfAccess, True)
                        Return True
                    Else
                        Return False
                    End If
                End If
            End If

            Dim conclusion As IVTConclusion = MakeFinalIVTDetermination(potentialGiverOfAccess)
            Return conclusion = IVTConclusion.Match
            ' Note that C#, for error recovery, includes OrElse conclusion = IVTConclusion.OneSignedOneNot
        End Function

        Friend ReadOnly Property StrongNameKeys As StrongNameKeys
            Get
                If _lazyStrongNameKeys Is Nothing Then
                    Try
                        Debug.Assert(s_AssemblyForWhichCurrentThreadIsComputingKeys Is Nothing)
                        s_AssemblyForWhichCurrentThreadIsComputingKeys = Me

                        ComputeAndSetStrongNameKeys()
                    Finally
                        s_AssemblyForWhichCurrentThreadIsComputingKeys = Nothing
                    End Try
                End If

                Return _lazyStrongNameKeys
            End Get
        End Property

        Private Sub ComputeAndSetStrongNameKeys()
            ' TODO
            ' In order to allow users to escape problems that we create with our provisional granting of IVT access,
            ' consider not binding the attributes if the command line options were specified, then later bind them
            ' and report warnings if both were used.

            ' make sure keycontainer and keyfile attribute contents fields will be set
            EnsureAttributesAreBound()

            ' Creating strong names is a potentially expensive operation, so we will check
            ' if keys could have been created and published already.
            If _lazyStrongNameKeys IsNot Nothing Then
                Return
            End If

            Dim keys As StrongNameKeys
            Dim keyFile As String = _compilation.Options.CryptoKeyFile

            ' Public sign requires a keyfile
            If DeclaringCompilation.Options.PublicSign Then
                If Not String.IsNullOrEmpty(keyFile) AndAlso Not PathUtilities.IsAbsolute(keyFile) Then
                    ' If keyFile has a relative path then there should be a diagnostic
                    ' about it
                    Debug.Assert(Not DeclaringCompilation.Options.Errors.IsEmpty)
                    keys = StrongNameKeys.None
                Else
                    keys = StrongNameKeys.Create(keyFile, MessageProvider.Instance)
                End If

                ' Public signing doesn't require a strong name provider to be used. 
                Interlocked.CompareExchange(_lazyStrongNameKeys, keys, Nothing)
                Return
            End If

            ' when both attributes and command-line options specified, cmd line wins.
            If String.IsNullOrEmpty(keyFile) Then
                keyFile = Me.AssemblyKeyFileAttributeSetting

                If keyFile Is CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                    keyFile = Nothing
                End If
            End If

            Dim keyContainer As String = _compilation.Options.CryptoKeyContainer

            If String.IsNullOrEmpty(keyContainer) Then
                keyContainer = Me.AssemblyKeyContainerAttributeSetting

                If keyContainer Is CommonAssemblyWellKnownAttributeData.StringMissingValue Then
                    keyContainer = Nothing
                End If
            End If

            Dim hasCounterSignature = Not String.IsNullOrEmpty(SignatureKey)
            keys = StrongNameKeys.Create(DeclaringCompilation.Options.StrongNameProvider, keyFile, keyContainer, hasCounterSignature, MessageProvider.Instance)
            Interlocked.CompareExchange(_lazyStrongNameKeys, keys, Nothing)
        End Sub

        Private Function ComputeIdentity() As AssemblyIdentity

            EnsureAttributesAreBound()

            Return New AssemblyIdentity(_assemblySimpleName,
                                        VersionHelper.GenerateVersionFromPatternAndCurrentTime(_compilation.Options.CurrentLocalTime, AssemblyVersionAttributeSetting),
                                        Me.AssemblyCultureAttributeSetting,
                                        StrongNameKeys.PublicKey,
                                        hasPublicKey:=Not StrongNameKeys.PublicKey.IsDefault,
                                        isRetargetable:=(AssemblyFlags And AssemblyFlags.Retargetable) = AssemblyFlags.Retargetable)

        End Function

        Friend ReadOnly Property IsVbRuntime As Boolean
            Get
                If Me._lazyIsVbRuntime = ThreeState.Unknown Then
                    Me._lazyIsVbRuntime = CheckForRuntime().ToThreeState
                End If

                Return Me._lazyIsVbRuntime = ThreeState.True
            End Get
        End Property

        Private Function CheckForRuntime() As Boolean
            Dim stdmodule = Me.DeclaringCompilation.GetWellKnownType(WellKnownType.Microsoft_VisualBasic_CompilerServices_StandardModuleAttribute)
            Return Not stdmodule.IsErrorType AndAlso
                   Not stdmodule.IsEmbedded AndAlso
                   stdmodule.ContainingAssembly Is Me
        End Function

        Friend Overrides Function TryLookupForwardedMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), ignoreCase As Boolean) As NamedTypeSymbol
            If Not _compilation.Options.OutputKind.IsNetModule() Then
                ' See if any of added modules forward the type.
                Dim matchedName As String = Nothing

                ' Similar to attributes, type forwarders from the second added module should override type forwarders from the first added module, etc. 
                For i As Integer = _modules.Length - 1 To 1 Step -1
                    Dim peModuleSymbol = DirectCast(_modules(i), PEModuleSymbol)
                    Dim forwardedToAssemblies = peModuleSymbol.GetAssembliesForForwardedType(emittedName, ignoreCase, matchedName)

                    If forwardedToAssemblies.FirstSymbol IsNot Nothing Then
                        If forwardedToAssemblies.SecondSymbol IsNot Nothing Then
                            Return CreateMultipleForwardingErrorTypeSymbol(emittedName, peModuleSymbol, forwardedToAssemblies.FirstSymbol, forwardedToAssemblies.SecondSymbol)
                        End If

                        ' Don't bother to check the forwarded-to assembly if we've already seen it.
                        If visitedAssemblies IsNot Nothing AndAlso visitedAssemblies.Contains(forwardedToAssemblies.FirstSymbol) Then
                            Return CreateCycleInTypeForwarderErrorTypeSymbol(emittedName)
                        Else
                            visitedAssemblies = New ConsList(Of AssemblySymbol)(Me, If(visitedAssemblies, ConsList(Of AssemblySymbol).Empty))

                            If ignoreCase AndAlso Not String.Equals(emittedName.FullName, matchedName, StringComparison.Ordinal) Then
                                emittedName = MetadataTypeName.FromFullName(matchedName, emittedName.UseCLSCompliantNameArityEncoding, emittedName.ForcedArity)
                            End If

                            Return forwardedToAssemblies.FirstSymbol.LookupDeclaredOrForwardedTopLevelMetadataType(emittedName, visitedAssemblies)
                        End If
                    End If
                Next
            End If

            Return Nothing
        End Function

        Public Overrides Function GetMetadata() As AssemblyMetadata
            Return Nothing
        End Function

        Private ReadOnly Property ISourceAssemblySymbol_Compilation As Compilation Implements ISourceAssemblySymbol.Compilation
            Get
                Return _compilation
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' <assembly: Experimental> may have been specified in the assembly or one of the modules
                Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazySourceAttributesBag
                If attributesBag IsNot Nothing AndAlso attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                    Dim experimentalData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData)?.ExperimentalAttributeData
                    If experimentalData IsNot Nothing Then
                        Return experimentalData
                    End If
                End If

                attributesBag = Me._lazyNetModuleAttributesBag
                If attributesBag IsNot Nothing AndAlso attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                    Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData)?.ExperimentalAttributeData
                End If

                If GetAttributeDeclarations().IsEmpty Then
                    Return Nothing
                End If

                Return ObsoleteAttributeData.Uninitialized
            End Get
        End Property

    End Class
End Namespace

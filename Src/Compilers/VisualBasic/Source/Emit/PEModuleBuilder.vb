' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Partial Friend MustInherit Class PEModuleBuilder
        Inherits PEModuleBuilder(Of VisualBasicCompilation, Symbol, SourceModuleSymbol, ModuleSymbol, AssemblySymbol, NamespaceSymbol, TypeSymbol, NamedTypeSymbol, MethodSymbol, VisualBasicSyntaxNode, NoPia.EmbeddedTypesManager)

        ' Not many methods should end up here.
        Private ReadOnly m_DisableJITOptimization As ConcurrentDictionary(Of MethodSymbol, Boolean) = New ConcurrentDictionary(Of MethodSymbol, Boolean)(ReferenceEqualityComparer.Instance)

        ' Gives the name of this module (may not reflect the name of the underlying symbol).
        ' See Assembly.MetadataName.
        Private ReadOnly m_MetadataName As String

        Private m_LazyExportedTypes As ImmutableArray(Of TypeExport(Of NamedTypeSymbol))

        ' These fields will only be set when running tests.  They allow realized IL for a given method to be looked up by method display name.
        Private m_TestData As ConcurrentDictionary(Of String, CompilationTestData.MethodData)
        Private m_TestDataKeyFormat As SymbolDisplayFormat
        Private m_TestDataOperatorKeyFormat As SymbolDisplayFormat

        Friend Sub New(sourceModule As SourceModuleSymbol,
                       outputName As String,
                       outputKind As OutputKind,
                       serializationProperties As ModulePropertiesForSerialization,
                       manifestResources As IEnumerable(Of ResourceDescription),
                       assemblySymbolMapper As Func(Of AssemblySymbol, AssemblyIdentity))

            MyBase.New(sourceModule.ContainingSourceAssembly.DeclaringCompilation, sourceModule, serializationProperties, manifestResources, outputKind, assemblySymbolMapper)

            m_MetadataName = If(outputName, sourceModule.MetadataName)
            m_AssemblyOrModuleSymbolToModuleRefMap.Add(sourceModule, Me)

            If sourceModule.AnyReferencedAssembliesAreLinked Then
                m_EmbeddedTypesManagerOpt = New NoPia.EmbeddedTypesManager(Me)
            End If
        End Sub

        Friend Overrides ReadOnly Property Name As String
            Get
                Return m_MetadataName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ModuleName As String
            Get
                Return m_MetadataName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property CorLibrary As AssemblySymbol
            Get
                Return SourceModule.ContainingSourceAssembly.CorLibrary
            End Get
        End Property

        Protected Overrides Iterator Function GetAssemblyReferencesFromAddedModules(diagnostics As DiagnosticBag) As IEnumerable(Of IAssemblyReference)
            Dim modules As ImmutableArray(Of ModuleSymbol) = SourceModule.ContainingAssembly.Modules

            For i As Integer = 1 To modules.Length - 1
                For Each aRef As AssemblySymbol In modules(i).GetReferencedAssemblySymbols()
                    Yield Translate(aRef, diagnostics)
                Next
            Next
        End Function

        Private Sub ValidateReferencedAssembly(assembly As AssemblySymbol, asmRef As AssemblyReference, diagnostics As DiagnosticBag)
            Dim asmIdentity As AssemblyIdentity = SourceModule.ContainingAssembly.Identity
            Dim refIdentity As AssemblyIdentity = asmRef.MetadataIdentity

            If asmIdentity.IsStrongName AndAlso Not refIdentity.IsStrongName AndAlso
               DirectCast(asmRef, Microsoft.Cci.IAssemblyReference).ContentType <> Reflection.AssemblyContentType.WindowsRuntime Then
                diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ReferencedAssemblyDoesNotHaveStrongName, assembly), NoLocation.Singleton)
            End If

            If OutputKind <> CodeAnalysis.OutputKind.NetModule AndAlso
               Not String.IsNullOrEmpty(refIdentity.CultureName) AndAlso
               Not String.Equals(refIdentity.CultureName, asmIdentity.CultureName, StringComparison.OrdinalIgnoreCase) Then
                diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.WRN_RefCultureMismatch, assembly, refIdentity.CultureName), NoLocation.Singleton)
            End If

            Dim refMachine = assembly.Machine
            ' If other assembly is agnostic, this is always safe
            ' Also, if no mscorlib was specified for back compat we add a reference to mscorlib
            ' that resolves to the current framework directory. If the compiler Is 64-bit
            ' this Is a 64-bit mscorlib, which will produce a warning if /platform:x86 Is
            ' specified.A reference to the default mscorlib should always succeed without
            ' warning so we ignore it here.
            If assembly IsNot assembly.CorLibrary AndAlso
               Not (refMachine = System.Reflection.PortableExecutable.Machine.I386 AndAlso Not assembly.Bit32Required) Then
                Dim machine = SourceModule.Machine

                If Not (machine = System.Reflection.PortableExecutable.Machine.I386 AndAlso Not SourceModule.Bit32Required) AndAlso
                    machine <> refMachine Then
                    ' Different machine types, and neither is agnostic
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.WRN_ConflictingMachineAssembly, assembly), NoLocation.Singleton)
                End If
            End If

            If m_EmbeddedTypesManagerOpt IsNot Nothing AndAlso m_EmbeddedTypesManagerOpt.IsFrozen Then
                m_EmbeddedTypesManagerOpt.ReportIndirectReferencesToLinkedAssemblies(assembly, diagnostics)
            End If
        End Sub

        Friend NotOverridable Overrides Function SynthesizeAttribute(attributeConstructor As WellKnownMember) As ICustomAttribute
            Return Me.Compilation.SynthesizeAttribute(attributeConstructor)
        End Function

        Friend NotOverridable Overrides Function GetSourceAssemblyAttributes() As IEnumerable(Of ICustomAttribute)
            Return SourceModule.ContainingSourceAssembly.GetCustomAttributesToEmit(emittingAssemblyAttributesInNetModule:=OutputKind.IsNetModule())
        End Function

        Friend NotOverridable Overrides Function GetSourceAssemblySecurityAttributes() As IEnumerable(Of SecurityAttribute)
            Dim sourceSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = Nothing
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = SourceModule.ContainingSourceAssembly.GetSourceAttributesBag()
            Dim wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol))
            If wellKnownAttributeData IsNot Nothing Then
                Dim securityData As SecurityWellKnownAttributeData = wellKnownAttributeData.SecurityInformation
                If securityData IsNot Nothing Then
                    sourceSecurityAttributes = securityData.GetSecurityAttributes(attributesBag.Attributes)
                End If
            End If

            Dim netmoduleSecurityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = Nothing
            attributesBag = SourceModule.ContainingSourceAssembly.GetNetModuleAttributesBag()
            wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol))
            If wellKnownAttributeData IsNot Nothing Then
                Dim securityData As SecurityWellKnownAttributeData = wellKnownAttributeData.SecurityInformation
                If securityData IsNot Nothing Then
                    netmoduleSecurityAttributes = securityData.GetSecurityAttributes(attributesBag.Attributes)
                End If
            End If

            Dim securityAttributes As IEnumerable(Of Microsoft.Cci.SecurityAttribute) = Nothing
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
                    securityAttributes = SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.SecurityAttribute)()
                End If
            End If

            Debug.Assert(securityAttributes IsNot Nothing)
            Return securityAttributes
        End Function

        Friend NotOverridable Overrides Function GetSourceModuleAttributes() As IEnumerable(Of ICustomAttribute)
            Return SourceModule.GetCustomAttributesToEmit()
        End Function

        Protected Overrides Function GetSymbolToLocationMap() As MultiDictionary(Of DebugSourceDocument, DefinitionWithLocation)
            Dim result As New MultiDictionary(Of Cci.DebugSourceDocument, Cci.DefinitionWithLocation)()

            Dim namespacesAndTypesToProcess As New Stack(Of NamespaceOrTypeSymbol)()
            namespacesAndTypesToProcess.Push(SourceModule.GlobalNamespace)

            Dim location As Location = Nothing

            While namespacesAndTypesToProcess.Count > 0
                Dim symbol As NamespaceOrTypeSymbol = namespacesAndTypesToProcess.Pop()
                Select Case symbol.Kind

                    Case SymbolKind.Namespace
                        location = GetSmallestSourceLocationOrNull(symbol)

                        ' filtering out synthesized symbols not having real source 
                        ' locations such as anonymous types, my types, etc...
                        If location IsNot Nothing Then
                            For Each member In symbol.GetMembers()
                                Select Case member.Kind
                                    Case SymbolKind.Namespace, SymbolKind.NamedType
                                        namespacesAndTypesToProcess.Push(DirectCast(member, NamespaceOrTypeSymbol))
                                    Case Else
                                        Throw ExceptionUtilities.UnexpectedValue(member.Kind)
                                End Select
                            Next
                        End If

                    Case SymbolKind.NamedType
                        location = GetSmallestSourceLocationOrNull(symbol)
                        If location IsNot Nothing Then
                            ' add this named type location
                            AddSymbolLocation(result, location, DirectCast(symbol, Cci.IDefinition))

                            For Each member In symbol.GetMembers()
                                Select Case member.Kind
                                    Case SymbolKind.NamedType
                                        namespacesAndTypesToProcess.Push(DirectCast(member, NamespaceOrTypeSymbol))

                                    Case SymbolKind.Method
                                        Dim method = DirectCast(member, MethodSymbol)
                                        If Not method.IsParameterlessStructConstructor(True) Then
                                            AddSymbolLocation(result, member)
                                        End If

                                    Case SymbolKind.Property,
                                         SymbolKind.Field
                                        AddSymbolLocation(result, member)

                                    Case SymbolKind.Event
                                        AddSymbolLocation(result, member)
                                        ' event backing fields do not show up in GetMembers
                                        AddSymbolLocation(result, (DirectCast(member, EventSymbol)).AssociatedField)

                                    Case Else
                                        Throw ExceptionUtilities.UnexpectedValue(member.Kind)
                                End Select
                            Next
                        End If

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
                End Select
            End While

            Return result
        End Function

        Private Sub AddSymbolLocation(result As MultiDictionary(Of Cci.DebugSourceDocument, Cci.DefinitionWithLocation), symbol As Symbol)
            Dim location As Location = GetSmallestSourceLocationOrNull(symbol)
            If location IsNot Nothing Then
                AddSymbolLocation(result, location, DirectCast(symbol, Cci.IDefinition))
            End If
        End Sub

        Private Sub AddSymbolLocation(result As MultiDictionary(Of Cci.DebugSourceDocument, Cci.DefinitionWithLocation), location As Location, definition As Cci.IDefinition)
            Dim span As FileLinePositionSpan = location.GetLineSpan()

            Dim doc As Cci.DebugSourceDocument = Me.TryGetDebugDocument(span.Path, basePath:=location.SourceTree.FilePath)
            Debug.Assert(doc IsNot Nothing)

            result.Add(doc,
                       New Cci.DefinitionWithLocation(
                           definition,
                           span.StartLinePosition.Line,
                           span.StartLinePosition.Character,
                           span.EndLinePosition.Line,
                           span.EndLinePosition.Character))
        End Sub

        Private Function GetSmallestSourceLocationOrNull(symbol As Symbol) As Location
            Dim compilation As VisualBasicCompilation = symbol.DeclaringCompilation
            Debug.Assert(Me.Compilation Is compilation, "How did we get symbol from different compilation?")

            Dim result As Location = Nothing

            For Each loc In symbol.Locations
                If loc.IsInSource AndAlso (result Is Nothing OrElse compilation.CompareSourceLocations(result, loc) > 0) Then
                    result = loc
                End If
            Next

            Return result
        End Function

        Friend Overridable Function CreateLocalSlotManager(method As MethodSymbol) As LocalSlotManager
            Return New FullLocalSlotManager()
        End Function

        Friend Overridable Function GetPreviousAnonymousTypes() As ImmutableArray(Of Microsoft.CodeAnalysis.Emit.AnonymousTypeKey)
            Return ImmutableArray(Of Microsoft.CodeAnalysis.Emit.AnonymousTypeKey).Empty
        End Function

        Friend Overridable Function GetNextAnonymousTypeIndex(fromDelegates As Boolean) As Integer
            Return 0
        End Function

        Friend Overridable Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out()> ByRef name As String, <Out()> ByRef index As Integer) As Boolean
            Debug.Assert(Compilation Is template.DeclaringCompilation)
            name = Nothing
            index = -1
            Return False
        End Function

        Friend Overrides Function GetAnonymousTypes() As IEnumerable(Of INamespaceTypeDefinition)
            Return SourceModule.ContainingSourceAssembly.DeclaringCompilation.AnonymousTypeManager.AllCreatedTemplates
        End Function

        Friend Overrides Iterator Function GetTopLevelTypesCore(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of INamespaceTypeDefinition)
            Dim embeddedSymbolManager As EmbeddedSymbolManager = SourceModule.ContainingSourceAssembly.DeclaringCompilation.EmbeddedSymbolManager
            Dim stack As New Stack(Of NamespaceOrTypeSymbol)()

            stack.Push(SourceModule.GlobalNamespace)

            Do
                Dim sym As NamespaceOrTypeSymbol = stack.Pop()

                If sym.Kind = SymbolKind.NamedType Then
                    Debug.Assert(sym Is sym.OriginalDefinition)
                    Debug.Assert(sym.ContainingType Is Nothing)

                    ' Skip unreferenced embedded types.
                    If Not sym.IsEmbedded OrElse embeddedSymbolManager.IsSymbolReferenced(sym) Then
                        Dim type = DirectCast(sym, NamedTypeSymbol)
                        Yield type
                    End If
                Else
                    Debug.Assert(sym.Kind = SymbolKind.Namespace)
                    Dim members As ImmutableArray(Of Symbol) = sym.GetMembers()
                    For i As Integer = members.Length - 1 To 0 Step -1
                        Dim nortsym As NamespaceOrTypeSymbol = TryCast(members(i), NamespaceOrTypeSymbol)
                        If nortsym IsNot Nothing Then
                            stack.Push(nortsym)
                        End If
                    Next
                End If
            Loop While stack.Count > 0

        End Function

        Public Overrides Function GetExportedTypes(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Cci.ITypeExport)
            Debug.Assert(HaveDeterminedTopLevelTypes)

            If m_LazyExportedTypes.IsDefault Then
                Dim builder = ArrayBuilder(Of TypeExport(Of NamedTypeSymbol)).GetInstance()
                Dim sourceAssembly As SourceAssemblySymbol = SourceModule.ContainingSourceAssembly

                If Not OutputKind.IsNetModule() Then
                    Dim modules = sourceAssembly.Modules

                    For i As Integer = 1 To modules.Length - 1 'NOTE: skipping modules(0)
                        GetExportedTypes(modules(i).GlobalNamespace, builder)
                    Next
                End If

                Dim seenTopLevelForwardedTypes = New HashSet(Of NamedTypeSymbol)()
                GetForwardedTypes(seenTopLevelForwardedTypes, sourceAssembly.GetSourceDecodedWellKnownAttributeData(), builder)

                If Not OutputKind.IsNetModule() Then
                    GetForwardedTypes(seenTopLevelForwardedTypes, sourceAssembly.GetNetModuleDecodedWellKnownAttributeData(), builder)
                End If

                Debug.Assert(m_LazyExportedTypes.IsDefault)

                m_LazyExportedTypes = builder.ToImmutableAndFree()

                If m_LazyExportedTypes.Length > 0 Then
                    ' Report name collisions.
                    Dim exportedNamesMap = New Dictionary(Of String, NamedTypeSymbol)()

                    For Each [alias] In m_LazyExportedTypes
                        Dim aliasedType As NamedTypeSymbol = [alias].AliasedType
                        Debug.Assert(aliasedType.IsDefinition)

                        If aliasedType.ContainingType Is Nothing Then
                            Dim fullEmittedName As String = MetadataHelpers.BuildQualifiedName((DirectCast(aliasedType, Microsoft.Cci.INamespaceTypeReference)).NamespaceName,
                                                                                        Cci.PeWriter.GetMangledName(aliasedType))

                            ' First check against types declared in the primary module
                            If ContainsTopLevelType(fullEmittedName) Then
                                If aliasedType.ContainingAssembly Is sourceAssembly Then
                                    context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_ExportedTypeConflictsWithDeclaration, aliasedType, aliasedType.ContainingModule),
                                            NoLocation.Singleton))
                                Else
                                    context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_ForwardedTypeConflictsWithDeclaration, aliasedType),
                                            NoLocation.Singleton))
                                End If

                                Continue For
                            End If

                            Dim contender As NamedTypeSymbol = Nothing

                            ' Now check against other exported types
                            If exportedNamesMap.TryGetValue(fullEmittedName, contender) Then

                                If aliasedType.ContainingAssembly Is sourceAssembly Then
                                    ' all exported types precede forwarded types, therefore contender cannot be a forwarded type.
                                    Debug.Assert(contender.ContainingAssembly Is sourceAssembly)

                                    context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_ExportedTypesConflict,
                                                                                                aliasedType, aliasedType.ContainingModule,
                                                                                                contender, contender.ContainingModule),
                                        NoLocation.Singleton))
                                Else
                                    If contender.ContainingAssembly Is sourceAssembly Then
                                        ' Forwarded type conflicts with exported type
                                        context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_ForwardedTypeConflictsWithExportedType,
                                                                                                    aliasedType, aliasedType.ContainingAssembly,
                                                                                                    contender, contender.ContainingModule),
                                            NoLocation.Singleton))
                                    Else
                                        ' Forwarded type conflicts with another forwarded type
                                        context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_ForwardedTypesConflict,
                                                                                                    aliasedType, aliasedType.ContainingAssembly,
                                                                                                    contender, contender.ContainingAssembly),
                                            NoLocation.Singleton))
                                    End If
                                End If

                                Continue For
                            End If

                            exportedNamesMap.Add(fullEmittedName, aliasedType)
                        End If
                    Next
                End If
            End If

            Return m_LazyExportedTypes
        End Function

        Private Overloads Sub GetExportedTypes(sym As NamespaceOrTypeSymbol, builder As ArrayBuilder(Of TypeExport(Of NamedTypeSymbol)))
            If sym.Kind = SymbolKind.NamedType Then
                If sym.DeclaredAccessibility = Accessibility.Public Then
                    Debug.Assert(sym.IsDefinition)
                    builder.Add(New TypeExport(Of NamedTypeSymbol)(DirectCast(sym, NamedTypeSymbol)))
                Else
                    Return
                End If
            End If

            For Each t In sym.GetMembers()
                Dim nortsym = TryCast(t, NamespaceOrTypeSymbol)

                If nortsym IsNot Nothing Then
                    GetExportedTypes(nortsym, builder)
                End If
            Next
        End Sub

        Private Shared Sub GetForwardedTypes(
            seenTopLevelTypes As HashSet(Of NamedTypeSymbol),
            wellKnownAttributeData As CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol),
            builder As ArrayBuilder(Of TypeExport(Of NamedTypeSymbol))
        )
            If wellKnownAttributeData IsNot Nothing AndAlso wellKnownAttributeData.ForwardedTypes IsNot Nothing Then
                For Each forwardedType As NamedTypeSymbol In wellKnownAttributeData.ForwardedTypes
                    Dim originalDefinition As NamedTypeSymbol = forwardedType.OriginalDefinition
                    Debug.Assert(originalDefinition.ContainingType Is Nothing, "How did a nested type get forwarded?")

                    ' De-dup the original definitions before emitting.
                    If Not seenTopLevelTypes.Add(originalDefinition) Then
                        Continue For
                    End If

                    ' Return all nested types.
                    ' Note the order: depth first, children in reverse order (to match dev10, not a requirement).
                    Dim stack = New Stack(Of NamedTypeSymbol)()
                    stack.Push(originalDefinition)

                    While stack.Count > 0
                        Dim curr As NamedTypeSymbol = stack.Pop()

                        ' In general, we don't want private types to appear in the ExportedTypes table.
                        If curr.DeclaredAccessibility = Accessibility.Private Then
                            ' NOTE: this will also exclude nested types of curr.
                            Continue While
                        End If

                        ' NOTE: not bothering to put nested types in seenTypes - the top-level type is adequate protection.

                        builder.Add(New TypeExport(Of NamedTypeSymbol)(curr))

                        ' Iterate backwards so they get popped in forward order.
                        Dim nested As ImmutableArray(Of NamedTypeSymbol) = curr.GetTypeMembers() ' Ordered.
                        For i As Integer = nested.Length - 1 To 0 Step -1
                            stack.Push(nested(i))
                        Next
                    End While
                Next
            End If
        End Sub

        Friend NotOverridable Overrides ReadOnly Property LinkerMajorVersion As Byte
            Get
                'EDMAURER the Windows loader team says that this value is not used in loading but is used by the appcompat infrastructure.
                'It is useful for us to have
                'a mechanism to identify the compiler that produced the binary. This is the appropriate
                'value to use for that. That is what it was invented for. We don't want to have the high
                'bit set for this in case some users perform a signed comparision to determine if the value
                'is less than some version. The C++ linker is at 0x0B. Roslyn C# will start at &H30. We'll start our numbering at &H50.
                Return &H50
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property LinkerMinorVersion As Byte
            Get
                Return 0
            End Get
        End Property

        Friend Iterator Function GetReferencedAssembliesUsedSoFar() As IEnumerable(Of AssemblySymbol)
            For Each assembly In SourceModule.GetReferencedAssemblySymbols()
                If Not assembly.IsLinked AndAlso
                    Not assembly.IsMissing AndAlso
                    m_AssemblyOrModuleSymbolToModuleRefMap.ContainsKey(assembly) Then
                    Yield assembly
                End If
            Next
        End Function

        Friend NotOverridable Overrides Function GetSystemType(syntaxOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As INamedTypeReference
            Dim systemTypeSymbol As NamedTypeSymbol = SourceModule.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type)

            Dim useSiteError = Binder.GetUseSiteErrorForWellKnownType(systemTypeSymbol)
            If useSiteError IsNot Nothing Then
                Binder.ReportDiagnostic(diagnostics,
                                        If(syntaxOpt IsNot Nothing, syntaxOpt.GetLocation(), NoLocation.Singleton),
                                        useSiteError)
            End If

            Return Translate(systemTypeSymbol, syntaxOpt, diagnostics, needDeclaration:=True)
        End Function

        Friend NotOverridable Overrides Function GetSpecialType(specialType As SpecialType, syntaxNodeOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As INamedTypeReference
            Dim typeSymbol = SourceModule.ContainingAssembly.GetSpecialType(specialType)

            Dim info = Binder.GetUseSiteErrorForSpecialType(typeSymbol)
            If info IsNot Nothing Then
                Binder.ReportDiagnostic(diagnostics, If(syntaxNodeOpt IsNot Nothing, syntaxNodeOpt.GetLocation(), NoLocation.Singleton), info)
            End If

            Return Translate(typeSymbol,
                             needDeclaration:=True,
                             syntaxNodeOpt:=syntaxNodeOpt,
                             diagnostics:=diagnostics)
        End Function

        Public Overrides Function GetInitArrayHelper() As Cci.IMethodReference
            Return DirectCast(Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__InitializeArrayArrayRuntimeFieldHandle), MethodSymbol)
        End Function

        Protected Overrides Function IsPlatformType(typeRef As Cci.ITypeReference, platformType As Cci.PlatformType) As Boolean
            Dim namedType = TryCast(typeRef, NamedTypeSymbol)

            If namedType IsNot Nothing Then
                If platformType = Cci.PlatformType.SystemType Then
                    Return namedType Is Compilation.GetWellKnownType(WellKnownType.System_Type)
                End If

                Return namedType.SpecialType = CType(platformType, SpecialType)
            End If

            Return False
        End Function

        Protected Overrides Function GetCorLibraryReferenceToEmit(context As Microsoft.CodeAnalysis.Emit.Context) As Cci.IAssemblyReference
            Dim corLib = CorLibrary

            If Not corLib.IsMissing AndAlso
                Not corLib.IsLinked AndAlso
                corLib IsNot SourceModule.ContainingAssembly Then
                Return Translate(corLib, context.Diagnostics)
            End If

            Return Nothing
        End Function

        Friend NotOverridable Overrides Function GetSynthesizedNestedTypes(container As NamedTypeSymbol) As IEnumerable(Of Cci.INestedTypeDefinition)
            Return container.GetSynthesizedNestedTypes()
        End Function

        Public Sub SetDisableJITOptimization(methodSymbol As MethodSymbol)
            Debug.Assert(methodSymbol.ContainingModule Is Me.SourceModule AndAlso methodSymbol Is methodSymbol.OriginalDefinition)

            m_DisableJITOptimization.TryAdd(methodSymbol, True)
        End Sub

        Public Function JITOptimizationIsDisabled(methodSymbol As MethodSymbol) As Boolean
            Debug.Assert(methodSymbol.ContainingModule Is Me.SourceModule AndAlso methodSymbol Is methodSymbol.OriginalDefinition)
            Return m_DisableJITOptimization.ContainsKey(methodSymbol)
        End Function

#Region "Test Hooks"

        Friend ReadOnly Property SaveTestData() As Boolean
            Get
                Return m_TestData IsNot Nothing
            End Get
        End Property

        Friend Sub SetMethodTestData(methodSymbol As MethodSymbol, builder As ILBuilder)
            If m_TestData Is Nothing Then
                Throw New InvalidOperationException("Must call SetILBuilderMap before calling SetILBuilder")
            End If

            ' If this ever throws "ArgumentException: An item with the same key has already been added.", then
            ' the ilBuilderMapKeyFormat will need to be updated to provide a unique key (see SetILBuilderMap).
            m_TestData.Add(
                methodSymbol.ToDisplayString(If(methodSymbol.IsUserDefinedOperator(), m_TestDataOperatorKeyFormat, m_TestDataKeyFormat)),
                New CompilationTestData.MethodData(builder, methodSymbol))
        End Sub

        Friend Sub SetMethodTestData(methods As ConcurrentDictionary(Of String, CompilationTestData.MethodData))
            Me.m_TestData = methods
            Me.m_TestDataKeyFormat = New SymbolDisplayFormat(
                compilerInternalOptions:=SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames Or SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers,
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:=
                    SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                    SymbolDisplayParameterOptions.IncludeExtensionThis Or
                    SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes Or
                    SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays Or
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName)
            ' most methods don't need return type to disambiguate signatures, however,
            ' it is necessary to disambiguate user defined operators:
            '   Operator op_Implicit(Type) As Integer
            '   Operator op_Implicit(Type) As Single
            '   ... etc ...
            Me.m_TestDataOperatorKeyFormat = New SymbolDisplayFormat(
                m_TestDataKeyFormat.CompilerInternalOptions,
                m_TestDataKeyFormat.GlobalNamespaceStyle,
                m_TestDataKeyFormat.TypeQualificationStyle,
                m_TestDataKeyFormat.GenericsOptions,
                m_TestDataKeyFormat.MemberOptions Or SymbolDisplayMemberOptions.IncludeType,
                m_TestDataKeyFormat.ParameterOptions,
                m_TestDataKeyFormat.DelegateStyle,
                m_TestDataKeyFormat.ExtensionMethodStyle,
                m_TestDataKeyFormat.PropertyStyle,
                m_TestDataKeyFormat.LocalOptions,
                m_TestDataKeyFormat.KindOptions,
                m_TestDataKeyFormat.MiscellaneousOptions)
        End Sub

#End Region

    End Class
End Namespace
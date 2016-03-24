' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Reflection.PortableExecutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Partial Friend MustInherit Class PEModuleBuilder
        Inherits PEModuleBuilder(Of VisualBasicCompilation, SourceModuleSymbol, AssemblySymbol, TypeSymbol, NamedTypeSymbol, MethodSymbol, VisualBasicSyntaxNode, NoPia.EmbeddedTypesManager, ModuleCompilationState)

        ' Not many methods should end up here.
        Private ReadOnly _disableJITOptimization As ConcurrentDictionary(Of MethodSymbol, Boolean) = New ConcurrentDictionary(Of MethodSymbol, Boolean)(ReferenceEqualityComparer.Instance)

        ' Gives the name of this module (may not reflect the name of the underlying symbol).
        ' See Assembly.MetadataName.
        Private ReadOnly _metadataName As String

        Private _lazyExportedTypes As ImmutableArray(Of NamedTypeSymbol)
        Private _lazyTranslatedImports As ImmutableArray(Of Cci.UsedNamespaceOrType)
        Private _lazyDefaultNamespace As String

        ' These fields will only be set when running tests.  They allow realized IL for a given method to be looked up by method display name.
        Private _testData As ConcurrentDictionary(Of String, CompilationTestData.MethodData)
        Private _testDataKeyFormat As SymbolDisplayFormat
        Private _testDataOperatorKeyFormat As SymbolDisplayFormat

        Friend Sub New(sourceModule As SourceModuleSymbol,
                       emitOptions As EmitOptions,
                       outputKind As OutputKind,
                       serializationProperties As Cci.ModulePropertiesForSerialization,
                       manifestResources As IEnumerable(Of ResourceDescription))

            MyBase.New(sourceModule.ContainingSourceAssembly.DeclaringCompilation,
                       sourceModule,
                       serializationProperties,
                       manifestResources,
                       outputKind,
                       emitOptions,
                       New ModuleCompilationState())

            Dim specifiedName = sourceModule.MetadataName

            _metadataName = If(specifiedName <> Microsoft.CodeAnalysis.Compilation.UnspecifiedModuleAssemblyName,
                                specifiedName,
                                If(emitOptions.OutputNameOverride, specifiedName))

            m_AssemblyOrModuleSymbolToModuleRefMap.Add(sourceModule, Me)

            If sourceModule.AnyReferencedAssembliesAreLinked Then
                _embeddedTypesManagerOpt = New NoPia.EmbeddedTypesManager(Me)
            End If
        End Sub

        ''' <summary>
        ''' True if conditional calls may be omitted when the required preprocessor symbols are not defined.
        ''' </summary>
        ''' <remarks>
        ''' Only false in debugger scenarios (where calls should never be omitted).
        ''' </remarks>
        Friend MustOverride ReadOnly Property AllowOmissionOfConditionalCalls As Boolean

        Friend Overrides ReadOnly Property Name As String
            Get
                Return _metadataName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ModuleName As String
            Get
                Return _metadataName
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property CorLibrary As AssemblySymbol
            Get
                Return SourceModule.ContainingSourceAssembly.CorLibrary
            End Get
        End Property

        Protected Overrides ReadOnly Property GenerateVisualBasicStylePdb As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overrides ReadOnly Property LinkedAssembliesDebugInfo As IEnumerable(Of String)
            Get
                ' NOTE: Dev12 does not seem to emit anything but the name (i.e. no version, token, etc).
                ' See Builder::WriteNoPiaPdbList
                Return SourceModule.ReferencedAssemblySymbols.Where(Function(a) a.IsLinked).Select(Function(a) a.Name)
            End Get
        End Property

        Protected NotOverridable Overrides Function GetImports() As ImmutableArray(Of Cci.UsedNamespaceOrType)
            ' Imports should have been translated in code gen phase.
            Debug.Assert(Not _lazyTranslatedImports.IsDefault)
            Return _lazyTranslatedImports
        End Function

        Public Sub TranslateImports(diagnostics As DiagnosticBag)
            If _lazyTranslatedImports.IsDefault Then
                ImmutableInterlocked.InterlockedInitialize(
                    _lazyTranslatedImports,
                    NamespaceScopeBuilder.BuildNamespaceScope(Me, SourceModule.XmlNamespaces, SourceModule.AliasImports, SourceModule.MemberImports, diagnostics))
            End If
        End Sub

        Protected NotOverridable Overrides ReadOnly Property DefaultNamespace As String
            Get
                If _lazyDefaultNamespace IsNot Nothing Then
                    Return _lazyDefaultNamespace
                End If

                Dim rootNamespace = SourceModule.RootNamespace
                If rootNamespace.IsGlobalNamespace Then
                    Return String.Empty
                End If

                _lazyDefaultNamespace = rootNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)
                Return _lazyDefaultNamespace
            End Get
        End Property

        Protected Overrides Iterator Function GetAssemblyReferencesFromAddedModules(diagnostics As DiagnosticBag) As IEnumerable(Of Cci.IAssemblyReference)
            Dim modules As ImmutableArray(Of ModuleSymbol) = SourceModule.ContainingAssembly.Modules

            For i As Integer = 1 To modules.Length - 1
                For Each aRef As AssemblySymbol In modules(i).GetReferencedAssemblySymbols()
                    Yield Translate(aRef, diagnostics)
                Next
            Next
        End Function

        Private Sub ValidateReferencedAssembly(assembly As AssemblySymbol, asmRef As AssemblyReference, diagnostics As DiagnosticBag)
            Dim asmIdentity As AssemblyIdentity = SourceModule.ContainingAssembly.Identity
            Dim refIdentity As AssemblyIdentity = asmRef.Identity

            If asmIdentity.IsStrongName AndAlso Not refIdentity.IsStrongName AndAlso
               asmRef.Identity.ContentType <> Reflection.AssemblyContentType.WindowsRuntime Then
                ' Dev12 reported error, we have changed it to a warning to allow referencing libraries 
                ' built for platforms that don't support strong names.
                diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.WRN_ReferencedAssemblyDoesNotHaveStrongName, assembly), NoLocation.Singleton)
            End If

            If OutputKind <> OutputKind.NetModule AndAlso
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
               Not (refMachine = Machine.I386 AndAlso Not assembly.Bit32Required) Then
                Dim machine = SourceModule.Machine

                If Not (machine = Machine.I386 AndAlso Not SourceModule.Bit32Required) AndAlso
                    machine <> refMachine Then
                    ' Different machine types, and neither is agnostic
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.WRN_ConflictingMachineAssembly, assembly), NoLocation.Singleton)
                End If
            End If

            If _embeddedTypesManagerOpt IsNot Nothing AndAlso _embeddedTypesManagerOpt.IsFrozen Then
                _embeddedTypesManagerOpt.ReportIndirectReferencesToLinkedAssemblies(assembly, diagnostics)
            End If
        End Sub

        Friend NotOverridable Overrides Function SynthesizeAttribute(attributeConstructor As WellKnownMember) As Cci.ICustomAttribute
            Return Me.Compilation.TrySynthesizeAttribute(attributeConstructor)
        End Function

        Friend NotOverridable Overrides Function GetSourceAssemblyAttributes() As IEnumerable(Of Cci.ICustomAttribute)
            Return SourceModule.ContainingSourceAssembly.GetCustomAttributesToEmit(Me.CompilationState, emittingAssemblyAttributesInNetModule:=OutputKind.IsNetModule())
        End Function

        Friend NotOverridable Overrides Function GetSourceAssemblySecurityAttributes() As IEnumerable(Of Cci.SecurityAttribute)
            Return SourceModule.ContainingSourceAssembly.GetSecurityAttributes()
        End Function

        Friend NotOverridable Overrides Function GetSourceModuleAttributes() As IEnumerable(Of Cci.ICustomAttribute)
            Return SourceModule.GetCustomAttributesToEmit(Me.CompilationState)
        End Function

        Protected Overrides Function GetSymbolToLocationMap() As MultiDictionary(Of Cci.DebugSourceDocument, Cci.DefinitionWithLocation)
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
                                        If method.IsDefaultValueTypeConstructor() OrElse
                                           method.IsPartialWithoutImplementation Then
                                            Exit Select
                                        End If

                                        AddSymbolLocation(result, member)

                                    Case SymbolKind.Property,
                                         SymbolKind.Field
                                        AddSymbolLocation(result, member)

                                    Case SymbolKind.Event
                                        AddSymbolLocation(result, member)
                                        Dim AssociatedField = (DirectCast(member, EventSymbol)).AssociatedField

                                        If AssociatedField IsNot Nothing Then
                                            ' event backing fields do not show up in GetMembers
                                            AddSymbolLocation(result, AssociatedField)
                                        End If

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
            If (doc IsNot Nothing) Then
                result.Add(doc,
                       New Cci.DefinitionWithLocation(
                           definition,
                           span.StartLinePosition.Line,
                           span.StartLinePosition.Character,
                           span.EndLinePosition.Line,
                           span.EndLinePosition.Character))
            End If
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

        ''' <summary>
        ''' Ignore accessibility when resolving well-known type
        ''' members, in particular for generic type arguments
        ''' (e.g.: binding to internal types in the EE).
        ''' </summary>
        Friend Overridable ReadOnly Property IgnoreAccessibility As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overridable Function TryCreateVariableSlotAllocator(method As MethodSymbol, topLevelMethod As MethodSymbol) As VariableSlotAllocator
            Return Nothing
        End Function

        Friend Overridable Function GetPreviousAnonymousTypes() As ImmutableArray(Of AnonymousTypeKey)
            Return ImmutableArray(Of AnonymousTypeKey).Empty
        End Function

        Friend Overridable Function GetNextAnonymousTypeIndex(fromDelegates As Boolean) As Integer
            Return 0
        End Function

        Friend Overridable Function TryGetAnonymousTypeName(template As NamedTypeSymbol, <Out> ByRef name As String, <Out> ByRef index As Integer) As Boolean
            Debug.Assert(Compilation Is template.DeclaringCompilation)
            name = Nothing
            index = -1
            Return False
        End Function

        Friend Overrides Function GetAnonymousTypes() As ImmutableArray(Of Cci.INamespaceTypeDefinition)
            If EmitOptions.EmitMetadataOnly Then
                Return ImmutableArray(Of Cci.INamespaceTypeDefinition).Empty
            End If

            Return StaticCast(Of Cci.INamespaceTypeDefinition).
                From(SourceModule.ContainingSourceAssembly.DeclaringCompilation.AnonymousTypeManager.AllCreatedTemplates)
        End Function

        Friend Overrides Iterator Function GetTopLevelTypesCore(context As EmitContext) As IEnumerable(Of Cci.INamespaceTypeDefinition)
            For Each topLevel In GetAdditionalTopLevelTypes()
                Yield topLevel
            Next

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

        Friend Overridable Function GetAdditionalTopLevelTypes() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetExportedTypes(context As EmitContext) As IEnumerable(Of Cci.ITypeReference)
            Debug.Assert(HaveDeterminedTopLevelTypes)

            If _lazyExportedTypes.IsDefault Then
                Dim builder = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
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

                Debug.Assert(_lazyExportedTypes.IsDefault)

                _lazyExportedTypes = builder.ToImmutableAndFree()

                If _lazyExportedTypes.Length > 0 Then
                    ' Report name collisions.
                    Dim exportedNamesMap = New Dictionary(Of String, NamedTypeSymbol)()

                    For Each exportedType In _lazyExportedTypes
                        Debug.Assert(exportedType.IsDefinition)

                        If exportedType.ContainingType Is Nothing Then
                            Dim fullEmittedName As String = MetadataHelpers.BuildQualifiedName((DirectCast(exportedType, Cci.INamespaceTypeReference)).NamespaceName,
                                                                                               Cci.MetadataWriter.GetMangledName(exportedType))

                            ' First check against types declared in the primary module
                            If ContainsTopLevelType(fullEmittedName) Then
                                If exportedType.ContainingAssembly Is sourceAssembly Then
                                    context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_ExportedTypeConflictsWithDeclaration, exportedType, exportedType.ContainingModule),
                                            NoLocation.Singleton))
                                Else
                                    context.Diagnostics.Add(New VBDiagnostic(
                                        ErrorFactory.ErrorInfo(ERRID.ERR_ForwardedTypeConflictsWithDeclaration,
                                                               CustomSymbolDisplayFormatter.DefaultErrorFormat(exportedType)), NoLocation.Singleton))
                                End If

                                Continue For
                            End If

                            Dim contender As NamedTypeSymbol = Nothing

                            ' Now check against other exported types
                            If exportedNamesMap.TryGetValue(fullEmittedName, contender) Then

                                If exportedType.ContainingAssembly Is sourceAssembly Then
                                    ' all exported types precede forwarded types, therefore contender cannot be a forwarded type.
                                    Debug.Assert(contender.ContainingAssembly Is sourceAssembly)

                                    context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(
                                                                                ERRID.ERR_ExportedTypesConflict,
                                                                                CustomSymbolDisplayFormatter.DefaultErrorFormat(exportedType),
                                                                                CustomSymbolDisplayFormatter.DefaultErrorFormat(exportedType.ContainingModule),
                                                                                CustomSymbolDisplayFormatter.DefaultErrorFormat(contender),
                                                                                CustomSymbolDisplayFormatter.DefaultErrorFormat(contender.ContainingModule)),
                                                                             NoLocation.Singleton))
                                Else
                                    If contender.ContainingAssembly Is sourceAssembly Then
                                        ' Forwarded type conflicts with exported type
                                        context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(
                                                                                    ERRID.ERR_ForwardedTypeConflictsWithExportedType,
                                                                                    CustomSymbolDisplayFormatter.DefaultErrorFormat(exportedType),
                                                                                    exportedType.ContainingAssembly,
                                                                                    CustomSymbolDisplayFormatter.DefaultErrorFormat(contender),
                                                                                    CustomSymbolDisplayFormatter.DefaultErrorFormat(contender.ContainingModule)),
                                                                                 NoLocation.Singleton))
                                    Else
                                        ' Forwarded type conflicts with another forwarded type
                                        context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(
                                                                                    ERRID.ERR_ForwardedTypesConflict,
                                                                                    CustomSymbolDisplayFormatter.DefaultErrorFormat(exportedType),
                                                                                    exportedType.ContainingAssembly,
                                                                                    CustomSymbolDisplayFormatter.DefaultErrorFormat(contender),
                                                                                    contender.ContainingAssembly),
                                                                                 NoLocation.Singleton))
                                    End If
                                End If

                                Continue For
                            End If

                            exportedNamesMap.Add(fullEmittedName, exportedType)
                        End If
                    Next
                End If
            End If

            Return _lazyExportedTypes
        End Function

        Private Overloads Sub GetExportedTypes(symbol As NamespaceOrTypeSymbol, builder As ArrayBuilder(Of NamedTypeSymbol))
            If symbol.Kind = SymbolKind.NamedType Then
                If symbol.DeclaredAccessibility = Accessibility.Public Then
                    Debug.Assert(symbol.IsDefinition)
                    builder.Add(DirectCast(symbol, NamedTypeSymbol))
                Else
                    Return
                End If
            End If

            For Each member In symbol.GetMembers()
                Dim namespaceOrType = TryCast(member, NamespaceOrTypeSymbol)

                If namespaceOrType IsNot Nothing Then
                    GetExportedTypes(namespaceOrType, builder)
                End If
            Next
        End Sub

        Private Shared Sub GetForwardedTypes(
            seenTopLevelTypes As HashSet(Of NamedTypeSymbol),
            wellKnownAttributeData As CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol),
            builder As ArrayBuilder(Of NamedTypeSymbol)
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
                        Dim current As NamedTypeSymbol = stack.Pop()

                        ' In general, we don't want private types to appear in the ExportedTypes table.
                        If current.DeclaredAccessibility = Accessibility.Private Then
                            ' NOTE: this will also exclude nested types of curr.
                            Continue While
                        End If

                        ' NOTE: not bothering to put nested types in seenTypes - the top-level type is adequate protection.

                        builder.Add(current)

                        ' Iterate backwards so they get popped in forward order.
                        Dim nested As ImmutableArray(Of NamedTypeSymbol) = current.GetTypeMembers() ' Ordered.
                        For i As Integer = nested.Length - 1 To 0 Step -1
                            stack.Push(nested(i))
                        Next
                    End While
                Next
            End If
        End Sub

        Friend Iterator Function GetReferencedAssembliesUsedSoFar() As IEnumerable(Of AssemblySymbol)
            For Each assembly In SourceModule.GetReferencedAssemblySymbols()
                If Not assembly.IsLinked AndAlso
                    Not assembly.IsMissing AndAlso
                    m_AssemblyOrModuleSymbolToModuleRefMap.ContainsKey(assembly) Then
                    Yield assembly
                End If
            Next
        End Function

        Private Function GetWellKnownType(wellKnownType As WellKnownType, syntaxOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As Cci.INamedTypeReference
            Dim typeSymbol As NamedTypeSymbol = SourceModule.DeclaringCompilation.GetWellKnownType(wellKnownType)

            Dim useSiteError = Binder.GetUseSiteErrorForWellKnownType(typeSymbol)
            If useSiteError IsNot Nothing Then
                Binder.ReportDiagnostic(diagnostics,
                                        If(syntaxOpt IsNot Nothing, syntaxOpt.GetLocation(), NoLocation.Singleton),
                                        useSiteError)
            End If

            Return Translate(typeSymbol, syntaxOpt, diagnostics, needDeclaration:=True)
        End Function

        Friend NotOverridable Overrides Function GetSystemType(syntaxOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As Cci.INamedTypeReference
            Return GetWellKnownType(WellKnownType.System_Type, syntaxOpt, diagnostics)
        End Function

        Friend NotOverridable Overrides Function GetGuidType(syntaxOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As Cci.INamedTypeReference
            Return GetWellKnownType(WellKnownType.System_Guid, syntaxOpt, diagnostics)
        End Function

        Friend NotOverridable Overrides Function GetSpecialType(specialType As SpecialType, syntaxNodeOpt As VisualBasicSyntaxNode, diagnostics As DiagnosticBag) As Cci.INamedTypeReference
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

        Protected Overrides Function GetCorLibraryReferenceToEmit(context As EmitContext) As Cci.IAssemblyReference
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

            _disableJITOptimization.TryAdd(methodSymbol, True)
        End Sub

        Public Function JITOptimizationIsDisabled(methodSymbol As MethodSymbol) As Boolean
            Debug.Assert(methodSymbol.ContainingModule Is Me.SourceModule AndAlso methodSymbol Is methodSymbol.OriginalDefinition)
            Return _disableJITOptimization.ContainsKey(methodSymbol)
        End Function

#Region "Test Hooks"

        Friend ReadOnly Property SaveTestData() As Boolean
            Get
                Return _testData IsNot Nothing
            End Get
        End Property

        Friend Sub SetMethodTestData(methodSymbol As MethodSymbol, builder As ILBuilder)
            If _testData Is Nothing Then
                Throw New InvalidOperationException("Must call SetILBuilderMap before calling SetILBuilder")
            End If

            ' If this ever throws "ArgumentException: An item with the same key has already been added.", then
            ' the ilBuilderMapKeyFormat will need to be updated to provide a unique key (see SetILBuilderMap).
            _testData.Add(
                methodSymbol.ToDisplayString(If(methodSymbol.IsUserDefinedOperator(), _testDataOperatorKeyFormat, _testDataKeyFormat)),
                New CompilationTestData.MethodData(builder, methodSymbol))
        End Sub

        Friend Sub SetMethodTestData(methods As ConcurrentDictionary(Of String, CompilationTestData.MethodData))
            Me._testData = methods
            Me._testDataKeyFormat = New SymbolDisplayFormat(
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
            Me._testDataOperatorKeyFormat = New SymbolDisplayFormat(
                _testDataKeyFormat.CompilerInternalOptions,
                _testDataKeyFormat.GlobalNamespaceStyle,
                _testDataKeyFormat.TypeQualificationStyle,
                _testDataKeyFormat.GenericsOptions,
                _testDataKeyFormat.MemberOptions Or SymbolDisplayMemberOptions.IncludeType,
                _testDataKeyFormat.ParameterOptions,
                _testDataKeyFormat.DelegateStyle,
                _testDataKeyFormat.ExtensionMethodStyle,
                _testDataKeyFormat.PropertyStyle,
                _testDataKeyFormat.LocalOptions,
                _testDataKeyFormat.KindOptions,
                _testDataKeyFormat.MiscellaneousOptions)
        End Sub

#End Region

    End Class
End Namespace

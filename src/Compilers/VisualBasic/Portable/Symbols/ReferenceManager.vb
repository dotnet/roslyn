' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports MetadataOrDiagnostic = System.Object

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public NotInheritable Class VisualBasicCompilation
        ''' <summary>
        ''' ReferenceManager encapsulates functionality to create an underlying SourceAssemblySymbol 
        ''' (with underlying ModuleSymbols) for Compilation and AssemblySymbols for referenced assemblies 
        ''' (with underlying ModuleSymbols) all properly linked together based on reference resolution 
        ''' between them.
        ''' 
        ''' ReferenceManager is also responsible for reuse of metadata readers for imported modules and 
        ''' assemblies as well as existing AssemblySymbols for referenced assemblies. In order to do that, 
        ''' it maintains global cache for metadata readers and AssemblySymbols associated with them. 
        ''' The cache uses WeakReferences to refer to the metadata readers and AssemblySymbols to allow 
        ''' memory and resources being reclaimed once they are no longer used. The tricky part about reusing 
        ''' existing AssemblySymbols is to find a set of AssemblySymbols that are created for the referenced 
        ''' assemblies, which (the AssemblySymbols from the set) are linked in a way, consistent with the 
        ''' reference resolution between the referenced assemblies.
        ''' 
        ''' When existing Compilation is used as a metadata reference, there are scenarios when its underlying 
        ''' SourceAssemblySymbol cannot be used to provide symbols in context of the new Compilation. Consider 
        ''' classic multi-targeting scenario: compilation C1 references v1 of Lib.dll and compilation C2 
        ''' references C1 and v2 of Lib.dll. In this case, SourceAssemblySymbol for C1 is linked to AssemblySymbol 
        ''' for v1 of Lib.dll. However, given the set of references for C2, the same reference for C1 should be 
        ''' resolved against v2 of Lib.dll. In other words, in context of C2, all types from v1 of Lib.dll 
        ''' leaking through C1 (through method signatures, etc.) must be retargeted to the types from v2 of Lib.dll.
        ''' In this case, ReferenceManager creates a special RetargetingAssemblySymbol for C1, which is responsible 
        ''' for the type retargeting. The RetargetingAssemblySymbols could also be reused for different 
        ''' Compilations, ReferenceManager maintains a cache of RetargetingAssemblySymbols (WeakReferences) for each 
        ''' Compilation.
        ''' 
        ''' The only public entry point of this class is CreateSourceAssembly() method.
        ''' </summary>
        Friend NotInheritable Class ReferenceManager
            Inherits CommonReferenceManager(Of VisualBasicCompilation, AssemblySymbol)

            Public Sub New(simpleAssemblyName As String, identityComparer As AssemblyIdentityComparer, observedMetadata As Dictionary(Of MetadataReference, MetadataOrDiagnostic))
                MyBase.New(simpleAssemblyName, identityComparer, observedMetadata)
            End Sub

            Protected Overrides Function GetActualBoundReferencesUsedBy(assemblySymbol As AssemblySymbol) As AssemblySymbol()
                Dim refs As New List(Of AssemblySymbol)

                For Each [module] In assemblySymbol.Modules
                    refs.AddRange([module].GetReferencedAssemblySymbols())
                Next

                For i As Integer = 0 To refs.Count - 1 Step 1
                    If refs(i).IsMissing Then
                        refs(i) = Nothing ' Do not expose missing assembly symbols to ReferenceManager.Binder
                    End If
                Next

                Return refs.ToArray()
            End Function

            Protected Overrides Function GetNoPiaResolutionAssemblies(candidateAssembly As AssemblySymbol) As ImmutableArray(Of AssemblySymbol)
                If TypeOf candidateAssembly Is SourceAssemblySymbol Then
                    ' This is an optimization, if candidateAssembly links something or explicitly declares local type, 
                    ' common reference binder shouldn't reuse this symbol because candidateAssembly won't be in the 
                    ' set returned by GetNoPiaResolutionAssemblies(). This also makes things clearer.
                    Return ImmutableArray(Of AssemblySymbol).Empty
                End If

                Return candidateAssembly.GetNoPiaResolutionAssemblies()
            End Function

            Protected Overrides Function IsLinked(candidateAssembly As AssemblySymbol) As Boolean
                Return candidateAssembly.IsLinked
            End Function

            Protected Overrides Function GetCorLibrary(candidateAssembly As AssemblySymbol) As AssemblySymbol
                Dim corLibrary As AssemblySymbol = candidateAssembly.CorLibrary

                ' Do not expose missing assembly symbols to ReferenceManager.Binder
                Return If(corLibrary.IsMissing, Nothing, corLibrary)
            End Function

            Protected Overrides ReadOnly Property MessageProvider As CommonMessageProvider
                Get
                    Return VisualBasic.MessageProvider.Instance
                End Get
            End Property

            Protected Overrides Function CreateAssemblyDataForFile(assembly As PEAssembly,
                                                                   cachedSymbols As WeakList(Of IAssemblySymbol),
                                                                   documentationProvider As DocumentationProvider,
                                                                   sourceAssemblySimpleName As String,
                                                                   importOptions As MetadataImportOptions,
                                                                   embedInteropTypes As Boolean) As AssemblyData
                Return New AssemblyDataForFile(assembly,
                                               cachedSymbols,
                                               embedInteropTypes,
                                               documentationProvider,
                                               sourceAssemblySimpleName,
                                               importOptions)
            End Function

            Protected Overrides Function CreateAssemblyDataForCompilation(compilationReference As CompilationReference) As AssemblyData
                Dim vbReference = TryCast(compilationReference, VisualBasicCompilationReference)
                If vbReference Is Nothing Then
                    Throw New NotSupportedException(String.Format(VBResources.CantReferenceCompilationFromTypes, compilationReference.GetType(), "Visual Basic"))
                End If

                Dim result As New AssemblyDataForCompilation(vbReference.Compilation, vbReference.Properties.EmbedInteropTypes)
                Debug.Assert(vbReference.Compilation._lazyAssemblySymbol IsNot Nothing)
                Return result
            End Function

            Protected Overrides Function CheckPropertiesConsistency(primaryReference As MetadataReference, duplicateReference As MetadataReference, diagnostics As DiagnosticBag) As Boolean
                Return True
            End Function

            ''' <summary>
            ''' VB allows two weak assembly references of the same simple name be passed to a compilation 
            ''' as long as their versions are different. It ignores culture.
            ''' </summary>
            Protected Overrides Function WeakIdentityPropertiesEquivalent(identity1 As AssemblyIdentity, identity2 As AssemblyIdentity) As Boolean
                Return identity1.Version = identity2.Version
            End Function

            Public Sub CreateSourceAssemblyForCompilation(compilation As VisualBasicCompilation)
                ' We are reading the Reference Manager state outside of a lock by accessing 
                ' IsBound and HasCircularReference properties.
                ' Once isBound flag is flipped the state of the manager is available and doesn't change.
                ' 
                ' If two threads are building SourceAssemblySymbol and the first just updated 
                ' set isBound flag to 1 but not yet set lazySourceAssemblySymbol,
                ' the second thread may end up reusing the Reference Manager data the first thread calculated. 
                ' That's ok since 
                ' 1) the second thread would produce the same data,
                ' 2) all results calculated by the second thread will be thrown away since the first thread 
                '    already acquired SymbolCacheAndReferenceManagerStateGuard that is needed to publish the data.

                ' Given compilation is the first compilation that shares this manager and its symbols are requested.
                ' Perform full reference resolution and binding.
                If Not IsBound AndAlso CreateAndSetSourceAssemblyFullBind(compilation) Then

                    ' we have successfully bound the references for the compilation

                ElseIf Not HasCircularReference Then
                    ' Another compilation that shares the manager with the given compilation
                    ' already bound its references and produced tables that we can use to construct 
                    ' source assembly symbol faster.

                    CreateAndSetSourceAssemblyReuseData(compilation)
                Else
                    ' We encountered a circular reference while binding the previous compilation.
                    ' This compilation can't share bound references with other compilations. Create a new manager.

                    ' NOTE: The CreateSourceAssemblyFullBind is going to replace compilation's reference manager with newManager.

                    Dim newManager = New ReferenceManager(Me.SimpleAssemblyName, Me.IdentityComparer, Me.ObservedMetadata)
                    Dim successful = newManager.CreateAndSetSourceAssemblyFullBind(compilation)

                    ' The new manager isn't shared with any other compilation so there is no other 
                    ' thread but the current one could have initialized it.
                    Debug.Assert(successful)

                    newManager.AssertBound()
                End If

                AssertBound()
                Debug.Assert(compilation._lazyAssemblySymbol IsNot Nothing)
            End Sub

            ''' <summary>
            ''' Creates a <see cref="PEAssemblySymbol"/> from specified metadata. 
            ''' </summary>
            ''' <remarks>
            ''' Used by EnC to create symbols for emit baseline. The PE symbols are used by <see cref="VisualBasicSymbolMatcher"/>.
            ''' 
            ''' The assembly references listed in the metadata AssemblyRef table are matched to the resolved references 
            ''' stored on this <see cref="ReferenceManager"/>. Each AssemblyRef is matched against the assembly identities
            ''' using an exact equality comparison. No unification Or further resolution is performed.
            ''' </remarks>
            Friend Function CreatePEAssemblyForAssemblyMetadata(metadata As AssemblyMetadata, importOptions As MetadataImportOptions) As PEAssemblySymbol
                AssertBound()

                ' If the compilation has a reference from metadata to source assembly we can't share the referenced PE symbols.
                Debug.Assert(Not HasCircularReference)

                Dim referencedAssembliesByIdentity = New Dictionary(Of AssemblyIdentity, AssemblySymbol)()
                For Each symbol In Me.ReferencedAssemblies
                    referencedAssembliesByIdentity.Add(symbol.Identity, symbol)
                Next

                Dim assembly = metadata.GetAssembly
                Dim peReferences = assembly.AssemblyReferences.SelectAsArray(AddressOf MapAssemblyIdentityToResolvedSymbol, referencedAssembliesByIdentity)
                Dim assemblySymbol = New PEAssemblySymbol(assembly, DocumentationProvider.Default, isLinked:=False, importOptions:=importOptions)

                Dim unifiedAssemblies = Me.UnifiedAssemblies.WhereAsArray(Function(unified) referencedAssembliesByIdentity.ContainsKey(unified.OriginalReference))
                InitializeAssemblyReuseData(assemblySymbol, peReferences, unifiedAssemblies)

                If assembly.ContainsNoPiaLocalTypes() Then
                    assemblySymbol.SetNoPiaResolutionAssemblies(Me.ReferencedAssemblies)
                End If

                Return assemblySymbol
            End Function

            Private Shared Function MapAssemblyIdentityToResolvedSymbol(identity As AssemblyIdentity, map As Dictionary(Of AssemblyIdentity, AssemblySymbol)) As AssemblySymbol
                Dim symbol As AssemblySymbol = Nothing
                If map.TryGetValue(identity, symbol) Then
                    Return symbol
                End If
                Return New MissingAssemblySymbol(identity)
            End Function

            Private Sub CreateAndSetSourceAssemblyReuseData(compilation As VisualBasicCompilation)
                AssertBound()

                ' If the compilation has a reference from metadata to source assembly we can't share the referenced PE symbols.
                Debug.Assert(Not HasCircularReference)

                Dim moduleName As String = compilation.MakeSourceModuleName()
                Dim assemblySymbol = New SourceAssemblySymbol(compilation, Me.SimpleAssemblyName, moduleName, Me.ReferencedModules)

                InitializeAssemblyReuseData(assemblySymbol, Me.ReferencedAssemblies, Me.UnifiedAssemblies)

                If compilation._lazyAssemblySymbol Is Nothing Then
                    SyncLock SymbolCacheAndReferenceManagerStateGuard
                        If compilation._lazyAssemblySymbol Is Nothing Then
                            compilation._lazyAssemblySymbol = assemblySymbol
                            Debug.Assert(compilation._referenceManager Is Me)
                        End If
                    End SyncLock
                End If
            End Sub

            Private Sub InitializeAssemblyReuseData(assemblySymbol As AssemblySymbol, referencedAssemblies As ImmutableArray(Of AssemblySymbol), unifiedAssemblies As ImmutableArray(Of UnifiedAssembly(Of AssemblySymbol)))
                AssertBound()

                assemblySymbol.SetCorLibrary(If(Me.CorLibraryOpt, assemblySymbol))

                Dim sourceModuleReferences = New ModuleReferences(Of AssemblySymbol)(referencedAssemblies.SelectAsArray(Function(a) a.Identity), referencedAssemblies, unifiedAssemblies)
                assemblySymbol.Modules(0).SetReferences(sourceModuleReferences)

                Dim assemblyModules = assemblySymbol.Modules
                Dim referencedModulesReferences = Me.ReferencedModulesReferences
                Debug.Assert(assemblyModules.Length = referencedModulesReferences.Length + 1)

                For i = 1 To assemblyModules.Length - 1
                    assemblyModules(i).SetReferences(referencedModulesReferences(i - 1))
                Next
            End Sub

            ' Returns false if another compilation sharing this manager finished binding earlier and we should reuse its results.
            Friend Function CreateAndSetSourceAssemblyFullBind(compilation As VisualBasicCompilation) As Boolean

                Dim resolutionDiagnostics = DiagnosticBag.GetInstance()
                Dim supersedeLowerVersions = compilation.IsSubmission
                Dim assemblyReferencesBySimpleName = PooledDictionary(Of String, List(Of ReferencedAssemblyIdentity)).GetInstance()

                Try
                    Dim boundReferenceDirectiveMap As IDictionary(Of String, MetadataReference) = Nothing
                    Dim boundReferenceDirectives As ImmutableArray(Of MetadataReference) = Nothing
                    Dim referencedAssemblies As ImmutableArray(Of AssemblyData) = Nothing
                    Dim modules As ImmutableArray(Of PEModule) = Nothing ' To make sure the modules are not collected ahead of time.
                    Dim references As ImmutableArray(Of MetadataReference) = Nothing

                    Dim referenceMap As ImmutableArray(Of ResolvedReference) = ResolveMetadataReferences(
                        compilation,
                        assemblyReferencesBySimpleName,
                        references,
                        boundReferenceDirectiveMap,
                        boundReferenceDirectives,
                        referencedAssemblies,
                        modules,
                        resolutionDiagnostics)

                    Dim assemblyBeingBuiltData As New AssemblyDataForAssemblyBeingBuilt(New AssemblyIdentity(name:=SimpleAssemblyName), referencedAssemblies, modules)
                    Dim explicitAssemblyData = referencedAssemblies.Insert(0, assemblyBeingBuiltData)

                    ' Let's bind all the references and resolve missing one (if resolver is available)
                    Dim hasCircularReference As Boolean
                    Dim corLibraryIndex As Integer
                    Dim implicitlyResolvedReferences As ImmutableArray(Of MetadataReference) = Nothing
                    Dim implicitlyResolvedReferenceMap As ImmutableArray(Of ResolvedReference) = Nothing
                    Dim allAssemblyData As ImmutableArray(Of AssemblyData) = Nothing

                    Dim bindingResult() As BoundInputAssembly = Bind(explicitAssemblyData,
                                                                     modules,
                                                                     references,
                                                                     referenceMap,
                                                                     compilation.Options.MetadataReferenceResolver,
                                                                     compilation.Options.MetadataImportOptions,
                                                                     supersedeLowerVersions,
                                                                     assemblyReferencesBySimpleName,
                                                                     allAssemblyData,
                                                                     implicitlyResolvedReferences,
                                                                     implicitlyResolvedReferenceMap,
                                                                     resolutionDiagnostics,
                                                                     hasCircularReference,
                                                                     corLibraryIndex)

                    Debug.Assert(bindingResult.Length = allAssemblyData.Length)

                    references = references.AddRange(implicitlyResolvedReferences)
                    referenceMap = referenceMap.AddRange(implicitlyResolvedReferenceMap)

                    Dim referencedAssembliesMap As Dictionary(Of MetadataReference, Integer) = Nothing
                    Dim referencedModulesMap As Dictionary(Of MetadataReference, Integer) = Nothing
                    Dim aliasesOfReferencedAssemblies As ImmutableArray(Of ImmutableArray(Of String)) = Nothing

                    BuildReferencedAssembliesAndModulesMaps(
                        bindingResult,
                        references,
                        referenceMap,
                        modules.Length,
                        referencedAssemblies.Length,
                        assemblyReferencesBySimpleName,
                        supersedeLowerVersions,
                        referencedAssembliesMap,
                        referencedModulesMap,
                        aliasesOfReferencedAssemblies)

                    ' Create AssemblySymbols for assemblies that can't use any existing symbols.
                    Dim newSymbols As New List(Of Integer)

                    For i As Integer = 1 To bindingResult.Length - 1 Step 1
                        If bindingResult(i).AssemblySymbol Is Nothing Then
                            ' symbol hasn't been found in the cache, create a new one
                            bindingResult(i).AssemblySymbol = DirectCast(allAssemblyData(i), AssemblyDataForMetadataOrCompilation).CreateAssemblySymbol()
                            newSymbols.Add(i)
                        End If

                        Debug.Assert(allAssemblyData(i).IsLinked = bindingResult(i).AssemblySymbol.IsLinked)
                    Next

                    Dim assemblySymbol = New SourceAssemblySymbol(compilation, SimpleAssemblyName, compilation.MakeSourceModuleName(), modules)

                    Dim corLibrary As AssemblySymbol

                    If corLibraryIndex = 0 Then
                        corLibrary = assemblySymbol
                    ElseIf corLibraryIndex > 0 Then
                        corLibrary = bindingResult(corLibraryIndex).AssemblySymbol
                    Else
                        corLibrary = MissingCorLibrarySymbol.Instance
                    End If

                    assemblySymbol.SetCorLibrary(corLibrary)

                    ' Setup bound references for newly created AssemblySymbols
                    ' This should be done after we created/found all AssemblySymbols 
                    Dim missingAssemblies As Dictionary(Of AssemblyIdentity, MissingAssemblySymbol) = Nothing

                    ' -1 for assembly being built
                    Dim totalReferencedAssemblyCount = allAssemblyData.Length - 1

                    ' Setup bound references for newly created SourceAssemblySymbol
                    Dim moduleReferences As ImmutableArray(Of ModuleReferences(Of AssemblySymbol)) = Nothing
                    SetupReferencesForSourceAssembly(assemblySymbol,
                                                     modules,
                                                     totalReferencedAssemblyCount,
                                                     bindingResult,
                                                     missingAssemblies,
                                                     moduleReferences)

                    If newSymbols.Count > 0 Then
                        ' Only if we detected that a referenced assembly refers to the assembly being built
                        ' we allow the references to get a hold of the assembly being built.
                        If hasCircularReference Then
                            bindingResult(0).AssemblySymbol = assemblySymbol
                        End If

                        InitializeNewSymbols(newSymbols, assemblySymbol, allAssemblyData, bindingResult, missingAssemblies)
                    End If

                    If compilation._lazyAssemblySymbol Is Nothing Then
                        SyncLock SymbolCacheAndReferenceManagerStateGuard
                            If compilation._lazyAssemblySymbol Is Nothing Then

                                If IsBound Then
                                    ' Another thread has finished constructing AssemblySymbol for another compilation that shares this manager.
                                    ' Drop the results and reuse the symbols that were created for the other compilation.
                                    Return False
                                End If

                                UpdateSymbolCacheNoLock(newSymbols, allAssemblyData, bindingResult)

                                InitializeNoLock(
                                    referencedAssembliesMap,
                                    referencedModulesMap,
                                    boundReferenceDirectiveMap,
                                    boundReferenceDirectives,
                                    hasCircularReference,
                                    resolutionDiagnostics.ToReadOnly(),
                                    If(corLibrary Is assemblySymbol, Nothing, corLibrary),
                                    modules,
                                    moduleReferences,
                                    assemblySymbol.SourceModule.GetReferencedAssemblySymbols(),
                                    aliasesOfReferencedAssemblies,
                                    assemblySymbol.SourceModule.GetUnifiedAssemblies())

                                ' Make sure that the given compilation holds on this instance of reference manager.
                                Debug.Assert(compilation._referenceManager Is Me OrElse hasCircularReference)
                                compilation._referenceManager = Me

                                ' Finally, publish the source symbol after all data have been written.
                                ' Once lazyAssemblySymbol is non-null other readers might start reading the data written above.
                                compilation._lazyAssemblySymbol = assemblySymbol
                            End If
                        End SyncLock
                    End If

                    Return True
                Finally
                    resolutionDiagnostics.Free()
                End Try
            End Function

            Private Shared Sub InitializeNewSymbols(newSymbols As List(Of Integer),
                                                    assemblySymbol As SourceAssemblySymbol,
                                                    assemblies As ImmutableArray(Of AssemblyData),
                                                    bindingResult As BoundInputAssembly(),
                                                    missingAssemblies As Dictionary(Of AssemblyIdentity, MissingAssemblySymbol))
                Debug.Assert(newSymbols.Count > 0)

                Dim corLibrary = assemblySymbol.CorLibrary
                Debug.Assert(corLibrary IsNot Nothing)

                For Each i As Integer In newSymbols
                    Dim compilationData = TryCast(assemblies(i), AssemblyDataForCompilation)

                    If compilationData IsNot Nothing Then
                        SetupReferencesForRetargetingAssembly(bindingResult, i, missingAssemblies, sourceAssemblyDebugOnly:=assemblySymbol)
                    Else
                        Dim fileData = DirectCast(assemblies(i), AssemblyDataForFile)
                        SetupReferencesForFileAssembly(fileData, bindingResult, i, missingAssemblies, sourceAssemblyDebugOnly:=assemblySymbol)
                    End If
                Next

                Dim linkedReferencedAssembliesBuilder = ArrayBuilder(Of AssemblySymbol).GetInstance()

                ' Setup CorLibrary and NoPia stuff for newly created assemblies

                For Each i As Integer In newSymbols
                    If assemblies(i).ContainsNoPiaLocalTypes Then
                        bindingResult(i).AssemblySymbol.SetNoPiaResolutionAssemblies(
                            assemblySymbol.Modules(0).GetReferencedAssemblySymbols())
                    End If

                    ' Setup linked referenced assemblies.
                    linkedReferencedAssembliesBuilder.Clear()

                    If assemblies(i).IsLinked Then
                        linkedReferencedAssembliesBuilder.Add(bindingResult(i).AssemblySymbol)
                    End If

                    For Each referenceBinding In bindingResult(i).ReferenceBinding
                        If referenceBinding.IsBound AndAlso
                           assemblies(referenceBinding.DefinitionIndex).IsLinked Then
                            linkedReferencedAssembliesBuilder.Add(
                                bindingResult(referenceBinding.DefinitionIndex).AssemblySymbol)
                        End If
                    Next

                    If linkedReferencedAssembliesBuilder.Count > 0 Then
                        linkedReferencedAssembliesBuilder.RemoveDuplicates()
                        bindingResult(i).AssemblySymbol.SetLinkedReferencedAssemblies(linkedReferencedAssembliesBuilder.ToImmutable())
                    End If

                    bindingResult(i).AssemblySymbol.SetCorLibrary(corLibrary)
                Next

                linkedReferencedAssembliesBuilder.Free()

                If missingAssemblies IsNot Nothing Then
                    For Each missingAssembly In missingAssemblies.Values
                        missingAssembly.SetCorLibrary(corLibrary)
                    Next
                End If
            End Sub

            Private Sub UpdateSymbolCacheNoLock(newSymbols As List(Of Integer), assemblies As ImmutableArray(Of AssemblyData), bindingResult As BoundInputAssembly())
                ' Add new assembly symbols into the cache
                For Each i As Integer In newSymbols
                    Dim compilationData = TryCast(assemblies(i), AssemblyDataForCompilation)

                    If compilationData IsNot Nothing Then
                        compilationData.Compilation.CacheRetargetingAssemblySymbolNoLock(bindingResult(i).AssemblySymbol)
                    Else
                        Dim fileData = DirectCast(assemblies(i), AssemblyDataForFile)
                        fileData.CachedSymbols.Add(bindingResult(i).AssemblySymbol)
                    End If
                Next
            End Sub

            Private Shared Sub SetupReferencesForRetargetingAssembly(
                bindingResult() As BoundInputAssembly,
                bindingIndex As Integer,
                ByRef missingAssemblies As Dictionary(Of AssemblyIdentity, MissingAssemblySymbol),
                sourceAssemblyDebugOnly As SourceAssemblySymbol
            )
                Dim retargetingAssemblySymbol = DirectCast(bindingResult(bindingIndex).AssemblySymbol, Retargeting.RetargetingAssemblySymbol)
                Dim modules As ImmutableArray(Of ModuleSymbol) = retargetingAssemblySymbol.Modules
                Dim moduleCount = modules.Length
                Dim refsUsed As Integer = 0

                For j As Integer = 0 To moduleCount - 1 Step 1
                    Dim referencedAssemblies As ImmutableArray(Of AssemblyIdentity) = retargetingAssemblySymbol.UnderlyingAssembly.Modules(j).GetReferencedAssemblies()

                    ' For source module skip underlying linked references
                    If j = 0 Then
                        Dim underlyingReferencedAssemblySymbols As ImmutableArray(Of AssemblySymbol) =
                            retargetingAssemblySymbol.UnderlyingAssembly.Modules(0).GetReferencedAssemblySymbols()

                        Dim linkedUnderlyingReferences As Integer = 0

                        For Each asm As AssemblySymbol In underlyingReferencedAssemblySymbols
                            If asm.IsLinked Then
                                linkedUnderlyingReferences += 1
                            End If
                        Next

                        If linkedUnderlyingReferences > 0 Then
                            Dim filteredReferencedAssemblies As AssemblyIdentity() =
                                New AssemblyIdentity(referencedAssemblies.Length - linkedUnderlyingReferences - 1) {}

                            Dim newIndex As Integer = 0

                            For k As Integer = 0 To underlyingReferencedAssemblySymbols.Length - 1 Step 1
                                If Not underlyingReferencedAssemblySymbols(k).IsLinked Then
                                    filteredReferencedAssemblies(newIndex) = referencedAssemblies(k)
                                    newIndex += 1
                                End If
                            Next

                            Debug.Assert(newIndex = filteredReferencedAssemblies.Length)
                            referencedAssemblies = filteredReferencedAssemblies.AsImmutableOrNull()
                        End If
                    End If

                    Dim refsCount As Integer = referencedAssemblies.Length
                    Dim symbols(refsCount - 1) As AssemblySymbol
                    Dim unifiedAssemblies As ArrayBuilder(Of UnifiedAssembly(Of AssemblySymbol)) = Nothing

                    For k As Integer = 0 To refsCount - 1 Step 1
                        Dim referenceBinding = bindingResult(bindingIndex).ReferenceBinding(refsUsed + k)
                        If referenceBinding.IsBound Then
                            symbols(k) = GetAssemblyDefinitionSymbol(bindingResult, referenceBinding, unifiedAssemblies)
                        Else
                            symbols(k) = GetOrAddMissingAssemblySymbol(referencedAssemblies(k), missingAssemblies)
                        End If
                    Next

                    Dim moduleReferences As New ModuleReferences(Of AssemblySymbol)(referencedAssemblies, symbols.AsImmutableOrNull(), unifiedAssemblies.AsImmutableOrEmpty())
                    modules(j).SetReferences(moduleReferences, sourceAssemblyDebugOnly)

                    refsUsed += refsCount
                Next
            End Sub

            Private Shared Sub SetupReferencesForFileAssembly(
                fileData As AssemblyDataForFile,
                bindingResult() As BoundInputAssembly,
                bindingIndex As Integer,
                ByRef missingAssemblies As Dictionary(Of AssemblyIdentity, MissingAssemblySymbol),
                sourceAssemblyDebugOnly As SourceAssemblySymbol
            )
                Dim peAssemblySymbol = DirectCast(bindingResult(bindingIndex).AssemblySymbol, Symbols.Metadata.PE.PEAssemblySymbol)

                Dim modules As ImmutableArray(Of ModuleSymbol) = peAssemblySymbol.Modules
                Dim moduleCount = modules.Length
                Dim refsUsed As Integer = 0

                For j As Integer = 0 To moduleCount - 1 Step 1
                    Dim refsCount As Integer = fileData.Assembly.ModuleReferenceCounts(j)
                    Dim names(refsCount - 1) As AssemblyIdentity
                    Dim symbols(refsCount - 1) As AssemblySymbol

                    fileData.AssemblyReferences.CopyTo(refsUsed, names, 0, refsCount)

                    Dim unifiedAssemblies As ArrayBuilder(Of UnifiedAssembly(Of AssemblySymbol)) = Nothing

                    For k As Integer = 0 To refsCount - 1 Step 1
                        Dim referenceBinding = bindingResult(bindingIndex).ReferenceBinding(refsUsed + k)
                        If referenceBinding.IsBound Then
                            symbols(k) = GetAssemblyDefinitionSymbol(bindingResult, referenceBinding, unifiedAssemblies)
                        Else
                            symbols(k) = GetOrAddMissingAssemblySymbol(names(k), missingAssemblies)
                        End If
                    Next

                    Dim moduleReferences = New ModuleReferences(Of AssemblySymbol)(names.AsImmutableOrNull(), symbols.AsImmutableOrNull(), unifiedAssemblies.AsImmutableOrEmpty())
                    modules(j).SetReferences(moduleReferences, sourceAssemblyDebugOnly)

                    refsUsed += refsCount
                Next
            End Sub

            Private Shared Sub SetupReferencesForSourceAssembly(
                sourceAssembly As SourceAssemblySymbol,
                modules As ImmutableArray(Of PEModule),
                totalReferencedAssemblyCount As Integer,
                bindingResult() As BoundInputAssembly,
                ByRef missingAssemblies As Dictionary(Of AssemblyIdentity, MissingAssemblySymbol),
                ByRef moduleReferences As ImmutableArray(Of ModuleReferences(Of AssemblySymbol))
            )
                Dim moduleSymbols = sourceAssembly.Modules
                Debug.Assert(moduleSymbols.Length = 1 + modules.Length)

                Dim moduleReferencesBuilder = If(moduleSymbols.Length > 1, ArrayBuilder(Of ModuleReferences(Of AssemblySymbol)).GetInstance(), Nothing)

                Dim refsUsed As Integer = 0
                For moduleIndex As Integer = 0 To moduleSymbols.Length - 1 Step 1
                    Dim refsCount As Integer = If(moduleIndex = 0, totalReferencedAssemblyCount, modules(moduleIndex - 1).ReferencedAssemblies.Length)

                    Dim identities(refsCount - 1) As AssemblyIdentity
                    Dim symbols(refsCount - 1) As AssemblySymbol

                    Dim unifiedAssemblies As ArrayBuilder(Of UnifiedAssembly(Of AssemblySymbol)) = Nothing

                    For k As Integer = 0 To refsCount - 1 Step 1
                        Dim boundReference = bindingResult(0).ReferenceBinding(refsUsed + k)
                        If boundReference.IsBound Then
                            symbols(k) = GetAssemblyDefinitionSymbol(bindingResult, boundReference, unifiedAssemblies)
                        Else
                            symbols(k) = GetOrAddMissingAssemblySymbol(boundReference.ReferenceIdentity, missingAssemblies)
                        End If

                        identities(k) = boundReference.ReferenceIdentity
                    Next

                    Dim references = New ModuleReferences(Of AssemblySymbol)(identities.AsImmutableOrNull(),
                                                                             symbols.AsImmutableOrNull(),
                                                                             unifiedAssemblies.AsImmutableOrEmpty())

                    If moduleIndex > 0 Then
                        moduleReferencesBuilder.Add(references)
                    End If

                    moduleSymbols(moduleIndex).SetReferences(references, sourceAssembly)

                    refsUsed += refsCount
                Next

                moduleReferences = moduleReferencesBuilder.ToImmutableOrEmptyAndFree()
            End Sub

            Private Shared Function GetAssemblyDefinitionSymbol(bindingResult As BoundInputAssembly(),
                                                                referenceBinding As AssemblyReferenceBinding,
                                                                ByRef unifiedAssemblies As ArrayBuilder(Of UnifiedAssembly(Of AssemblySymbol))) As AssemblySymbol
                Debug.Assert(referenceBinding.IsBound)
                Dim assembly = bindingResult(referenceBinding.DefinitionIndex).AssemblySymbol
                Debug.Assert(assembly IsNot Nothing)

                If referenceBinding.VersionDifference <> 0 Then
                    If unifiedAssemblies Is Nothing Then
                        unifiedAssemblies = New ArrayBuilder(Of UnifiedAssembly(Of AssemblySymbol))()
                    End If

                    unifiedAssemblies.Add(New UnifiedAssembly(Of AssemblySymbol)(assembly, referenceBinding.ReferenceIdentity))
                End If

                Return assembly
            End Function

            Private Shared Function GetOrAddMissingAssemblySymbol(
                identity As AssemblyIdentity,
                ByRef missingAssemblies As Dictionary(Of AssemblyIdentity, MissingAssemblySymbol)
            ) As MissingAssemblySymbol
                Dim missingAssembly As MissingAssemblySymbol = Nothing

                If missingAssemblies Is Nothing Then
                    missingAssemblies = New Dictionary(Of AssemblyIdentity, MissingAssemblySymbol)()
                ElseIf missingAssemblies.TryGetValue(identity, missingAssembly) Then
                    Return missingAssembly
                End If

                missingAssembly = New MissingAssemblySymbol(identity)
                missingAssemblies.Add(identity, missingAssembly)

                Return missingAssembly
            End Function

            Private MustInherit Class AssemblyDataForMetadataOrCompilation
                Inherits AssemblyData

                Private _assemblies As List(Of AssemblySymbol)
                Private ReadOnly m_Identity As AssemblyIdentity
                Private ReadOnly m_ReferencedAssemblies As ImmutableArray(Of AssemblyIdentity)
                Private ReadOnly m_EmbedInteropTypes As Boolean

                Protected Sub New(identity As AssemblyIdentity,
                                  referencedAssemblies As ImmutableArray(Of AssemblyIdentity),
                                  embedInteropTypes As Boolean)

                    Debug.Assert(identity IsNot Nothing)
                    Debug.Assert(Not referencedAssemblies.IsDefault)

                    m_EmbedInteropTypes = embedInteropTypes
                    m_Identity = identity
                    m_ReferencedAssemblies = referencedAssemblies
                End Sub

                Friend MustOverride Function CreateAssemblySymbol() As AssemblySymbol

                Public Overrides ReadOnly Property Identity As AssemblyIdentity
                    Get
                        Return m_Identity
                    End Get
                End Property

                Public Overrides ReadOnly Property AvailableSymbols As IEnumerable(Of AssemblySymbol)
                    Get
                        If (_assemblies Is Nothing) Then
                            _assemblies = New List(Of AssemblySymbol)()

                            ' This should be done lazy because while we creating
                            ' instances of this type, creation of new SourceAssembly symbols
                            ' might change the set of available AssemblySymbols.
                            AddAvailableSymbols(_assemblies)
                        End If

                        Return _assemblies
                    End Get
                End Property

                Protected MustOverride Sub AddAvailableSymbols(assemblies As List(Of AssemblySymbol))

                Public Overrides ReadOnly Property AssemblyReferences As ImmutableArray(Of AssemblyIdentity)
                    Get
                        Return m_ReferencedAssemblies
                    End Get
                End Property

                Public Overrides Function BindAssemblyReferences(assemblies As ImmutableArray(Of AssemblyData), assemblyIdentityComparer As AssemblyIdentityComparer) As AssemblyReferenceBinding()
                    Return ResolveReferencedAssemblies(m_ReferencedAssemblies, assemblies, definitionStartIndex:=0, assemblyIdentityComparer:=assemblyIdentityComparer)
                End Function

                Public NotOverridable Overrides ReadOnly Property IsLinked As Boolean
                    Get
                        Return m_EmbedInteropTypes
                    End Get
                End Property
            End Class

            Private NotInheritable Class AssemblyDataForFile
                Inherits AssemblyDataForMetadataOrCompilation

                Public ReadOnly Assembly As PEAssembly

                ''' <summary>
                ''' Guarded by <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/>.
                ''' </summary>
                Public ReadOnly CachedSymbols As WeakList(Of IAssemblySymbol)

                Public ReadOnly DocumentationProvider As DocumentationProvider

                ''' <summary>
                ''' Import options of the compilation being built.
                ''' </summary>
                Private ReadOnly _compilationImportOptions As MetadataImportOptions

                ' This is the name of the compilation that is being built. 
                ' This should be the assembly name w/o the extension. It is
                ' used to compute whether or not it is possible that this
                ' assembly will give friend access to the compilation.
                Private ReadOnly _sourceAssemblySimpleName As String

                Private _internalsVisibleComputed As Boolean = False
                Private _internalsPotentiallyVisibleToCompilation As Boolean = False

                Public Sub New(assembly As PEAssembly,
                               cachedSymbols As WeakList(Of IAssemblySymbol),
                               embedInteropTypes As Boolean,
                               documentationProvider As DocumentationProvider,
                               sourceAssemblySimpleName As String,
                               compilationImportOptions As MetadataImportOptions)

                    MyBase.New(assembly.Identity, assembly.AssemblyReferences, embedInteropTypes)

                    Debug.Assert(documentationProvider IsNot Nothing)
                    Debug.Assert(cachedSymbols IsNot Nothing)

                    Me.CachedSymbols = cachedSymbols
                    Me.Assembly = assembly
                    Me.DocumentationProvider = documentationProvider
                    _compilationImportOptions = compilationImportOptions
                    _sourceAssemblySimpleName = sourceAssemblySimpleName
                End Sub

                Friend Overrides Function CreateAssemblySymbol() As AssemblySymbol
                    Return New PEAssemblySymbol(Assembly, DocumentationProvider, IsLinked, EffectiveImportOptions)
                End Function

                Friend ReadOnly Property InternalsMayBeVisibleToCompilation As Boolean
                    Get
                        If Not _internalsVisibleComputed Then
                            _internalsPotentiallyVisibleToCompilation = InternalsMayBeVisibleToAssemblyBeingCompiled(_sourceAssemblySimpleName, Assembly)
                            _internalsVisibleComputed = True
                        End If

                        Return _internalsPotentiallyVisibleToCompilation
                    End Get
                End Property

                Friend ReadOnly Property EffectiveImportOptions As MetadataImportOptions
                    Get
                        If InternalsMayBeVisibleToCompilation AndAlso _compilationImportOptions = MetadataImportOptions.Public Then
                            Return MetadataImportOptions.Internal
                        End If

                        Return _compilationImportOptions
                    End Get
                End Property

                Protected Overrides Sub AddAvailableSymbols(assemblies As List(Of AssemblySymbol))
                    Dim internalsMayBeVisibleToCompilation = Me.InternalsMayBeVisibleToCompilation

                    ' accessing cached symbols requires a lock
                    SyncLock SymbolCacheAndReferenceManagerStateGuard
                        For Each assemblySymbol In CachedSymbols
                            Dim peAssembly = TryCast(assemblySymbol, PEAssemblySymbol)
                            If IsMatchingAssembly(peAssembly) Then
                                assemblies.Add(peAssembly)
                            End If
                        Next
                    End SyncLock
                End Sub

                Public Overrides Function IsMatchingAssembly(candidateAssembly As AssemblySymbol) As Boolean
                    Return IsMatchingAssembly(TryCast(candidateAssembly, PEAssemblySymbol))
                End Function

                Private Overloads Function IsMatchingAssembly(peAssembly As PEAssemblySymbol) As Boolean
                    If peAssembly Is Nothing Then
                        Return False
                    End If

                    If peAssembly.Assembly IsNot Me.Assembly Then
                        Return False
                    End If

                    If EffectiveImportOptions <> peAssembly.PrimaryModule.ImportOptions Then
                        Return False
                    End If

                    ' TODO (tomat): 
                    ' We shouldn't need to compare documentation providers. All symbols in the cachedSymbols list 
                    ' should share the same provider - as they share the same metadata.
                    ' Removing the Equals call also avoids calling user code while holding a lock.
                    If Not peAssembly.DocumentationProvider.Equals(DocumentationProvider) Then
                        Return False
                    End If

                    Return True
                End Function

                Public Overrides ReadOnly Property ContainsNoPiaLocalTypes() As Boolean
                    Get
                        Return Assembly.ContainsNoPiaLocalTypes()
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaresTheObjectClass As Boolean
                    Get
                        Return Assembly.DeclaresTheObjectClass
                    End Get
                End Property

                Public Overrides ReadOnly Property SourceCompilation As Compilation
                    Get
                        Return Nothing
                    End Get
                End Property
            End Class

            Private NotInheritable Class AssemblyDataForCompilation
                Inherits AssemblyDataForMetadataOrCompilation

                Public ReadOnly Compilation As VisualBasicCompilation

                Public Sub New(compilation As VisualBasicCompilation, embedInteropTypes As Boolean)
                    MyBase.New(compilation.Assembly.Identity, GetReferencedAssemblies(compilation), embedInteropTypes)

                    Debug.Assert(compilation IsNot Nothing)
                    Me.Compilation = compilation
                End Sub

                Private Shared Function GetReferencedAssemblies(compilation As VisualBasicCompilation) As ImmutableArray(Of AssemblyIdentity)
                    ' Collect information about references
                    Dim refs = ArrayBuilder(Of AssemblyIdentity).GetInstance()

                    Dim modules = compilation.Assembly.Modules

                    ' Filter out linked assemblies referenced by the source module.
                    Dim sourceReferencedAssemblies = modules(0).GetReferencedAssemblies()
                    Dim sourceReferencedAssemblySymbols = modules(0).GetReferencedAssemblySymbols()

                    Debug.Assert(sourceReferencedAssemblies.Length = sourceReferencedAssemblySymbols.Length)

                    For i = 0 To sourceReferencedAssemblies.Length - 1
                        If Not sourceReferencedAssemblySymbols(i).IsLinked Then
                            refs.Add(sourceReferencedAssemblies(i))
                        End If
                    Next

                    For i = 1 To modules.Length - 1
                        refs.AddRange(modules(i).GetReferencedAssemblies())
                    Next

                    Return refs.ToImmutableAndFree()
                End Function

                Friend Overrides Function CreateAssemblySymbol() As AssemblySymbol
                    Return New RetargetingAssemblySymbol(Compilation.SourceAssembly, IsLinked)
                End Function

                Protected Overrides Sub AddAvailableSymbols(assemblies As List(Of AssemblySymbol))
                    assemblies.Add(Compilation.Assembly)

                    ' accessing cached symbols requires a lock
                    SyncLock SymbolCacheAndReferenceManagerStateGuard
                        Compilation.AddRetargetingAssemblySymbolsNoLock(assemblies)
                    End SyncLock
                End Sub

                Public Overrides Function IsMatchingAssembly(candidateAssembly As AssemblySymbol) As Boolean
                    Dim retargeting = TryCast(candidateAssembly, RetargetingAssemblySymbol)
                    Dim asm As AssemblySymbol

                    If retargeting IsNot Nothing Then
                        asm = retargeting.UnderlyingAssembly
                    Else
                        asm = TryCast(candidateAssembly, SourceAssemblySymbol)
                    End If

                    Debug.Assert(TypeOf asm IsNot RetargetingAssemblySymbol)

                    Return asm Is Compilation.Assembly
                End Function

                Public Overrides ReadOnly Property ContainsNoPiaLocalTypes As Boolean
                    Get
                        Return Compilation.MightContainNoPiaLocalTypes()
                    End Get
                End Property

                Public Overrides ReadOnly Property DeclaresTheObjectClass As Boolean
                    Get
                        Return Compilation.DeclaresTheObjectClass
                    End Get
                End Property

                Public Overrides ReadOnly Property SourceCompilation As Compilation
                    Get
                        Return Compilation
                    End Get
                End Property
            End Class

            ''' <summary>
            ''' For testing purposes only.
            ''' </summary>
            Friend Shared Function IsSourceAssemblySymbolCreated(compilation As VisualBasicCompilation) As Boolean
                Return compilation._lazyAssemblySymbol IsNot Nothing
            End Function

            ''' <summary>
            ''' For testing purposes only.
            ''' </summary>
            Friend Shared Function IsReferenceManagerInitialized(compilation As VisualBasicCompilation) As Boolean
                Return compilation._referenceManager.IsBound
            End Function
        End Class
    End Class
End Namespace

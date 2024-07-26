' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend Module EmitHelpers

        Friend Function EmitDifference(
            compilation As VisualBasicCompilation,
            baseline As EmitBaseline,
            edits As IEnumerable(Of SemanticEdit),
            isAddedSymbol As Func(Of ISymbol, Boolean),
            metadataStream As Stream,
            ilStream As Stream,
            pdbStream As Stream,
            testData As CompilationTestData,
            cancellationToken As CancellationToken) As EmitDifferenceResult

            Dim pdbName = FileNameUtilities.ChangeExtension(compilation.SourceModule.Name, "pdb")
            Dim diagnostics = DiagnosticBag.GetInstance()

            Dim emitOpts = EmitOptions.Default.WithDebugInformationFormat(If(baseline.HasPortablePdb, DebugInformationFormat.PortablePdb, DebugInformationFormat.Pdb))
            Dim runtimeMDVersion = compilation.GetRuntimeMetadataVersion()
            Dim serializationProperties = compilation.ConstructModuleSerializationProperties(emitOpts, runtimeMDVersion, baseline.ModuleVersionId)
            Dim manifestResources = SpecializedCollections.EmptyEnumerable(Of ResourceDescription)()

            Dim predefinedHotReloadExceptionConstructor As MethodSymbol = Nothing
            If Not GetPredefinedHotReloadExceptionTypeConstructor(compilation, diagnostics, predefinedHotReloadExceptionConstructor) Then
                Return New EmitDifferenceResult(
                    success:=False,
                    diagnostics:=diagnostics.ToReadOnlyAndFree(),
                    baseline:=Nothing,
                    updatedMethods:=ImmutableArray(Of MethodDefinitionHandle).Empty,
                    changedTypes:=ImmutableArray(Of TypeDefinitionHandle).Empty)
            End If

            Dim changes As VisualBasicSymbolChanges
            Dim definitionMap As VisualBasicDefinitionMap
            Dim moduleBeingBuilt As PEDeltaAssemblyBuilder
            Try
                Dim sourceAssembly = compilation.SourceAssembly
                Dim initialBaseline = baseline.InitialBaseline
                Dim previousSourceAssembly = DirectCast(baseline.Compilation, VisualBasicCompilation).SourceAssembly

                ' Hydrate symbols from initial metadata. Once we do so it is important to reuse these symbols across all generations,
                ' in order for the symbol matcher to be able to use reference equality once it maps symbols to initial metadata.
                Dim metadataSymbols = PEDeltaAssemblyBuilder.GetOrCreateMetadataSymbols(initialBaseline, sourceAssembly.DeclaringCompilation)

                Dim metadataDecoder = DirectCast(metadataSymbols.MetadataDecoder, MetadataDecoder)
                Dim metadataAssembly = DirectCast(metadataDecoder.ModuleSymbol.ContainingAssembly, PEAssemblySymbol)
                Dim matchToMetadata = New VisualBasicSymbolMatcher(initialBaseline.LazyMetadataSymbols.SynthesizedTypes, sourceAssembly, metadataAssembly)

                Dim previousSourceToMetadata = New VisualBasicSymbolMatcher(
                metadataSymbols.SynthesizedTypes,
                previousSourceAssembly,
                metadataAssembly)

                Dim previousSourceToCurrentSource As VisualBasicSymbolMatcher = Nothing
                If baseline.Ordinal > 0 Then
                    Debug.Assert(baseline.PEModuleBuilder IsNot Nothing)

                    previousSourceToCurrentSource = New VisualBasicSymbolMatcher(
                        sourceAssembly:=sourceAssembly,
                        otherAssembly:=previousSourceAssembly,
                        baseline.SynthesizedTypes,
                        otherSynthesizedMembersOpt:=baseline.SynthesizedMembers,
                        otherDeletedMembersOpt:=baseline.DeletedMembers)
                End If

                definitionMap = New VisualBasicDefinitionMap(edits, metadataDecoder, previousSourceToMetadata, matchToMetadata, previousSourceToCurrentSource, baseline)
                changes = New VisualBasicSymbolChanges(definitionMap, edits, isAddedSymbol)

                moduleBeingBuilt = New PEDeltaAssemblyBuilder(
                    compilation.SourceAssembly,
                    changes,
                    emitOpts,
                    compilation.Options.OutputKind,
                    serializationProperties,
                    manifestResources,
                    predefinedHotReloadExceptionConstructor)
            Catch e As NotSupportedException
                ' TODO: https://github.com/dotnet/roslyn/issues/9004
                diagnostics.Add(ERRID.ERR_ModuleEmitFailure, NoLocation.Singleton, compilation.AssemblyName, e.Message)
                Return New EmitDifferenceResult(
                    success:=False,
                    diagnostics:=diagnostics.ToReadOnlyAndFree(),
                    baseline:=Nothing,
                    updatedMethods:=ImmutableArray(Of MethodDefinitionHandle).Empty,
                    changedTypes:=ImmutableArray(Of TypeDefinitionHandle).Empty)
            End Try

            If testData IsNot Nothing Then
                moduleBeingBuilt.SetTestData(testData)
            End If

            Dim newBaseline As EmitBaseline = Nothing
            Dim updatedMethods = ArrayBuilder(Of MethodDefinitionHandle).GetInstance()
            Dim changedTypes = ArrayBuilder(Of TypeDefinitionHandle).GetInstance()

            If compilation.Compile(moduleBeingBuilt,
                                   emittingPdb:=True,
                                   diagnostics:=diagnostics,
                                   filterOpt:=Function(s) changes.RequiresCompilation(s),
                                   cancellationToken:=cancellationToken) Then

                newBaseline = compilation.SerializeToDeltaStreams(
                    moduleBeingBuilt,
                    definitionMap,
                    changes,
                    metadataStream,
                    ilStream,
                    pdbStream,
                    updatedMethods,
                    changedTypes,
                    diagnostics,
                    testData?.SymWriterFactory,
                    emitOpts.PdbFilePath,
                    cancellationToken)
            End If

            Return New EmitDifferenceResult(
                success:=newBaseline IsNot Nothing,
                diagnostics:=diagnostics.ToReadOnlyAndFree(),
                baseline:=newBaseline,
                updatedMethods:=updatedMethods.ToImmutableAndFree(),
                changedTypes:=changedTypes.ToImmutableAndFree())
        End Function

        ''' <summary>
        ''' Returns true if the correct constructor is found or if the type Is Not defined at all, in which case it can be synthesized.
        ''' </summary>
        Private Function GetPredefinedHotReloadExceptionTypeConstructor(compilation As VisualBasicCompilation, diagnostics As DiagnosticBag, <Out> ByRef constructor As MethodSymbol) As Boolean
            constructor = TryCast(compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_HotReloadException__ctorStringInt32), MethodSymbol)
            If constructor IsNot Nothing Then
                Return True
            End If

            Dim type = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_HotReloadException)
            If type.Kind = SymbolKind.ErrorType Then
                ' type is missing And will be synthesized
                Return True
            End If

            diagnostics.Add(
                ERRID.ERR_ModuleEmitFailure,
                NoLocation.Singleton,
                compilation.AssemblyName,
                String.Format(CodeAnalysisResources.Type_0_does_not_have_expected_constructor, type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)))

            Return False
        End Function

        Friend Function MapToCompilation(
            compilation As VisualBasicCompilation,
            moduleBeingBuilt As PEDeltaAssemblyBuilder) As EmitBaseline

            Dim previousGeneration = moduleBeingBuilt.PreviousGeneration
            Debug.Assert(previousGeneration.Compilation IsNot compilation)

            If previousGeneration.Ordinal = 0 Then
                ' Initial generation, nothing to map. (Since the initial generation
                ' is always loaded from metadata in the context of the current
                ' compilation, there's no separate mapping step.)
                Return previousGeneration
            End If

            Dim synthesizedTypes = moduleBeingBuilt.GetSynthesizedTypes()
            Dim currentSynthesizedMembers = moduleBeingBuilt.GetAllSynthesizedMembers()
            Dim currentDeletedMembers = moduleBeingBuilt.EncSymbolChanges.DeletedMembers

            ' Mapping from previous compilation to the current.
            Dim previousSourceAssembly = DirectCast(previousGeneration.Compilation, VisualBasicCompilation).SourceAssembly

            Dim matcher = New VisualBasicSymbolMatcher(
                previousSourceAssembly,
                compilation.SourceAssembly,
                synthesizedTypes,
                currentSynthesizedMembers,
                currentDeletedMembers)

            Dim mappedSynthesizedMembers = matcher.MapSynthesizedOrDeletedMembers(previousGeneration.SynthesizedMembers, currentSynthesizedMembers, isDeletedMemberMapping:=False)
            Dim mappedDeletedMembers = matcher.MapSynthesizedOrDeletedMembers(previousGeneration.DeletedMembers, currentDeletedMembers, isDeletedMemberMapping:=True)

            ' TODO can we reuse some data from the previous matcher?
            Dim matcherWithAllSynthesizedMembers = New VisualBasicSymbolMatcher(
                previousSourceAssembly,
                compilation.SourceAssembly,
                synthesizedTypes,
                mappedSynthesizedMembers,
                mappedDeletedMembers)

            Return matcherWithAllSynthesizedMembers.MapBaselineToCompilation(
                previousGeneration,
                compilation,
                moduleBeingBuilt,
                mappedSynthesizedMembers,
                mappedDeletedMembers)
        End Function
    End Module
End Namespace

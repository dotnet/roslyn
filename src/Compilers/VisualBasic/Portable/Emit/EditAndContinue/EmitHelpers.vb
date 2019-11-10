' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Metadata
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit

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
            updatedMethods As ICollection(Of MethodDefinitionHandle),
            testData As CompilationTestData,
            cancellationToken As CancellationToken) As EmitDifferenceResult

            Dim pdbName = FileNameUtilities.ChangeExtension(compilation.SourceModule.Name, "pdb")
            Dim diagnostics = DiagnosticBag.GetInstance()

            Dim emitOpts = EmitOptions.Default.WithDebugInformationFormat(If(baseline.HasPortablePdb, DebugInformationFormat.PortablePdb, DebugInformationFormat.Pdb))
            Dim runtimeMDVersion = compilation.GetRuntimeMetadataVersion()
            Dim serializationProperties = compilation.ConstructModuleSerializationProperties(emitOpts, runtimeMDVersion, baseline.ModuleVersionId)
            Dim manifestResources = SpecializedCollections.EmptyEnumerable(Of ResourceDescription)()

            Dim moduleBeingBuilt As PEDeltaAssemblyBuilder
            Try
                moduleBeingBuilt = New PEDeltaAssemblyBuilder(
                    compilation.SourceAssembly,
                    emitOptions:=emitOpts,
                    outputKind:=compilation.Options.OutputKind,
                    serializationProperties:=serializationProperties,
                    manifestResources:=manifestResources,
                    previousGeneration:=baseline,
                    edits:=edits,
                    isAddedSymbol:=isAddedSymbol)
            Catch e As NotSupportedException
                ' TODO: better error code (https://github.com/dotnet/roslyn/issues/8910)
                diagnostics.Add(ERRID.ERR_ModuleEmitFailure, NoLocation.Singleton, compilation.AssemblyName)
                Return New EmitDifferenceResult(success:=False, diagnostics:=diagnostics.ToReadOnlyAndFree(), baseline:=Nothing)
            End Try

            If testData IsNot Nothing Then
                moduleBeingBuilt.SetMethodTestData(testData.Methods)
                testData.Module = moduleBeingBuilt
            End If

            Dim definitionMap = moduleBeingBuilt.PreviousDefinitions
            Dim changes = moduleBeingBuilt.Changes

            Dim newBaseline As EmitBaseline = Nothing

            If compilation.Compile(moduleBeingBuilt,
                                   emittingPdb:=True,
                                   diagnostics:=diagnostics,
                                   filterOpt:=Function(s) changes.RequiresCompilation(s.GetISymbol()),
                                   cancellationToken:=cancellationToken) Then

                ' Map the definitions from the previous compilation to the current compilation.
                ' This must be done after compiling above since synthesized definitions
                ' (generated when compiling method bodies) may be required.
                Dim mappedBaseline = MapToCompilation(compilation, moduleBeingBuilt)

                newBaseline = compilation.SerializeToDeltaStreams(
                    moduleBeingBuilt,
                    mappedBaseline,
                    definitionMap,
                    changes,
                    metadataStream,
                    ilStream,
                    pdbStream,
                    updatedMethods,
                    diagnostics,
                    testData?.SymWriterFactory,
                    emitOpts.PdbFilePath,
                    cancellationToken)
            End If

            Return New EmitDifferenceResult(
                success:=newBaseline IsNot Nothing,
                diagnostics:=diagnostics.ToReadOnlyAndFree(),
                baseline:=newBaseline)
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

            Dim currentSynthesizedMembers = moduleBeingBuilt.GetAllSynthesizedMembers()

            ' Mapping from previous compilation to the current.
            Dim anonymousTypeMap = moduleBeingBuilt.GetAnonymousTypeMap()
            Dim sourceAssembly = DirectCast(previousGeneration.Compilation, VisualBasicCompilation).SourceAssembly
            Dim sourceContext = New EmitContext(DirectCast(previousGeneration.PEModuleBuilder, PEModuleBuilder), Nothing, New DiagnosticBag(), metadataOnly:=False, includePrivateMembers:=True)
            Dim otherContext = New EmitContext(moduleBeingBuilt, Nothing, New DiagnosticBag(), metadataOnly:=False, includePrivateMembers:=True)

            Dim matcher = New VisualBasicSymbolMatcher(
                anonymousTypeMap,
                sourceAssembly,
                sourceContext,
                compilation.SourceAssembly,
                otherContext,
                currentSynthesizedMembers)

            Dim mappedSynthesizedMembers = matcher.MapSynthesizedMembers(previousGeneration.SynthesizedMembers, currentSynthesizedMembers)

            ' TODO can we reuse some data from the previous matcher?
            Dim matcherWithAllSynthesizedMembers = New VisualBasicSymbolMatcher(
                anonymousTypeMap,
                sourceAssembly,
                sourceContext,
                compilation.SourceAssembly,
                otherContext,
                mappedSynthesizedMembers)

            Return matcherWithAllSynthesizedMembers.MapBaselineToCompilation(
                previousGeneration,
                compilation,
                moduleBeingBuilt,
                mappedSynthesizedMembers)
        End Function
    End Module
End Namespace

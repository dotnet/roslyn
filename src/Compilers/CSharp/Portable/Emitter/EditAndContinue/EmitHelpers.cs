// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal static class EmitHelpers
    {
        internal static EmitDifferenceResult EmitDifference(
            CSharpCompilation compilation,
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            CompilationTestData? testData,
            CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();

            var emitOptions = EmitOptions.Default.WithDebugInformationFormat(baseline.HasPortablePdb ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb);
            var runtimeMDVersion = compilation.GetRuntimeMetadataVersion(emitOptions, diagnostics);
            var serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMDVersion, baseline.ModuleVersionId);
            var manifestResources = SpecializedCollections.EmptyEnumerable<ResourceDescription>();

            if (!GetPredefinedHotReloadExceptionTypeConstructor(compilation, diagnostics, out var predefinedHotReloadExceptionConstructor))
            {
                return new EmitDifferenceResult(
                    success: false,
                    diagnostics: diagnostics.ToReadOnlyAndFree(),
                    baseline: null,
                    updatedMethods: [],
                    changedTypes: []);
            }

            CSharpSymbolChanges changes;
            CSharpDefinitionMap definitionMap;
            PEDeltaAssemblyBuilder moduleBeingBuilt;
            try
            {
                var sourceAssembly = compilation.SourceAssembly;
                var initialBaseline = baseline.InitialBaseline;

                var previousSourceAssembly = ((CSharpCompilation)baseline.Compilation).SourceAssembly;

                // Hydrate symbols from initial metadata. Once we do so it is important to reuse these symbols across all generations,
                // in order for the symbol matcher to be able to use reference equality once it maps symbols to initial metadata.
                var metadataSymbols = PEDeltaAssemblyBuilder.GetOrCreateMetadataSymbols(initialBaseline, sourceAssembly.DeclaringCompilation);
                var metadataDecoder = (MetadataDecoder)metadataSymbols.MetadataDecoder;
                var metadataAssembly = (PEAssemblySymbol)metadataDecoder.ModuleSymbol.ContainingAssembly;

                var sourceToMetadata = new CSharpSymbolMatcher(
                    metadataSymbols.SynthesizedTypes,
                    sourceAssembly,
                    metadataAssembly);

                var previousSourceToMetadata = new CSharpSymbolMatcher(
                    metadataSymbols.SynthesizedTypes,
                    previousSourceAssembly,
                    metadataAssembly);

                CSharpSymbolMatcher? currentSourceToPreviousSource = null;
                if (baseline.Ordinal > 0)
                {
                    Debug.Assert(baseline.PEModuleBuilder != null);

                    currentSourceToPreviousSource = new CSharpSymbolMatcher(
                        sourceAssembly: sourceAssembly,
                        otherAssembly: previousSourceAssembly,
                        baseline.SynthesizedTypes,
                        otherSynthesizedMembers: baseline.SynthesizedMembers,
                        otherDeletedMembers: baseline.DeletedMembers);
                }

                definitionMap = new CSharpDefinitionMap(edits, metadataDecoder, previousSourceToMetadata, sourceToMetadata, currentSourceToPreviousSource, baseline);
                changes = new CSharpSymbolChanges(definitionMap, edits, isAddedSymbol);

                moduleBeingBuilt = new PEDeltaAssemblyBuilder(
                    compilation.SourceAssembly,
                    changes,
                    emitOptions: emitOptions,
                    outputKind: compilation.Options.OutputKind,
                    serializationProperties: serializationProperties,
                    manifestResources: manifestResources,
                    predefinedHotReloadExceptionConstructor);
            }
            catch (NotSupportedException e)
            {
                // TODO: https://github.com/dotnet/roslyn/issues/9004
                diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, compilation.AssemblyName, e.Message);
                return new EmitDifferenceResult(
                    success: false,
                    diagnostics: diagnostics.ToReadOnlyAndFree(),
                    baseline: null,
                    updatedMethods: [],
                    changedTypes: []);
            }

            if (testData != null)
            {
                moduleBeingBuilt.SetTestData(testData);
            }

            EmitBaseline? newBaseline = null;
            var updatedMethods = ArrayBuilder<MethodDefinitionHandle>.GetInstance();
            var changedTypes = ArrayBuilder<TypeDefinitionHandle>.GetInstance();

            if (compilation.Compile(
                moduleBeingBuilt,
                emittingPdb: true,
                diagnostics: diagnostics,
                filterOpt: s => changes.RequiresCompilation(s),
                cancellationToken: cancellationToken))
            {
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
                    emitOptions.PdbFilePath,
                    cancellationToken);
            }

            return new EmitDifferenceResult(
                success: newBaseline != null,
                diagnostics: diagnostics.ToReadOnlyAndFree(),
                baseline: newBaseline,
                updatedMethods: updatedMethods.ToImmutableAndFree(),
                changedTypes: changedTypes.ToImmutableAndFree());
        }

        /// <summary>
        /// Returns true if the correct constructor is found or if the type is not defined at all, in which case it can be synthesized.
        /// </summary>
        private static bool GetPredefinedHotReloadExceptionTypeConstructor(CSharpCompilation compilation, DiagnosticBag diagnostics, out MethodSymbol? constructor)
        {
            constructor = compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_HotReloadException__ctorStringInt32) as MethodSymbol;
            if (constructor is { })
            {
                return true;
            }

            var type = compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_HotReloadException);
            if (type.Kind == SymbolKind.ErrorType)
            {
                // type is missing and will be synthesized
                return true;
            }

            diagnostics.Add(
                ErrorCode.ERR_ModuleEmitFailure,
                NoLocation.Singleton,
                compilation.AssemblyName,
                string.Format(CodeAnalysisResources.Type_0_does_not_have_expected_constructor, type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

            return false;
        }

        /// <summary>
        /// Return a version of the baseline with all definitions mapped to this compilation.
        /// Definitions from the initial generation, from metadata, are not mapped since
        /// the initial generation is always included as metadata. That is, the symbols from
        /// types, methods, ... in the TypesAdded, MethodsAdded, ... collections are replaced
        /// by the corresponding symbols from the current compilation.
        /// </summary>
        internal static EmitBaseline MapToCompilation(
            CSharpCompilation compilation,
            PEDeltaAssemblyBuilder moduleBeingBuilt)
        {
            var previousGeneration = moduleBeingBuilt.PreviousGeneration;
            RoslynDebug.Assert(previousGeneration.Compilation != compilation);

            if (previousGeneration.Ordinal == 0)
            {
                // Initial generation, nothing to map. (Since the initial generation
                // is always loaded from metadata in the context of the current
                // compilation, there's no separate mapping step.)
                return previousGeneration;
            }

            RoslynDebug.AssertNotNull(previousGeneration.Compilation);
            RoslynDebug.AssertNotNull(previousGeneration.PEModuleBuilder);
            RoslynDebug.AssertNotNull(moduleBeingBuilt.EncSymbolChanges);

            var synthesizedTypes = moduleBeingBuilt.GetSynthesizedTypes();
            var currentSynthesizedMembers = moduleBeingBuilt.GetAllSynthesizedMembers();
            var currentDeletedMembers = moduleBeingBuilt.EncSymbolChanges.DeletedMembers;

            // Mapping from previous compilation to the current.
            var previousSourceAssembly = ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly;

            var matcher = new CSharpSymbolMatcher(
                previousSourceAssembly,
                compilation.SourceAssembly,
                synthesizedTypes,
                currentSynthesizedMembers,
                currentDeletedMembers);

            var mappedSynthesizedMembers = matcher.MapSynthesizedOrDeletedMembers(previousGeneration.SynthesizedMembers, currentSynthesizedMembers, isDeletedMemberMapping: false);

            // Deleted members are mapped the same way as synthesized members, so we can just call the same method.
            var mappedDeletedMembers = matcher.MapSynthesizedOrDeletedMembers(previousGeneration.DeletedMembers, currentDeletedMembers, isDeletedMemberMapping: true);

            // TODO: can we reuse some data from the previous matcher?
            var matcherWithAllSynthesizedMembers = new CSharpSymbolMatcher(
                previousSourceAssembly,
                compilation.SourceAssembly,
                synthesizedTypes,
                mappedSynthesizedMembers,
                mappedDeletedMembers);

            return matcherWithAllSynthesizedMembers.MapBaselineToCompilation(
                previousGeneration,
                compilation,
                moduleBeingBuilt,
                mappedSynthesizedMembers,
                mappedDeletedMembers);
        }
    }
}

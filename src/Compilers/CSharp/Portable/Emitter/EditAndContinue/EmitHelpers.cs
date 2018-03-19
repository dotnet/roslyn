// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
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
            ICollection<MethodDefinitionHandle> updatedMethods,
            CompilationTestData testData,
            CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();

            EmitOptions emitOptions = EmitOptions.Default.WithDebugInformationFormat(baseline.HasPortablePdb ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb);
            string runtimeMDVersion = compilation.GetRuntimeMetadataVersion(emitOptions, diagnostics);
            Cci.ModulePropertiesForSerialization serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMDVersion, baseline.ModuleVersionId);
            IEnumerable<ResourceDescription> manifestResources = SpecializedCollections.EmptyEnumerable<ResourceDescription>();

            PEDeltaAssemblyBuilder moduleBeingBuilt;
            try
            {
                moduleBeingBuilt = new PEDeltaAssemblyBuilder(
                    compilation.SourceAssembly,
                    emitOptions: emitOptions,
                    outputKind: compilation.Options.OutputKind,
                    serializationProperties: serializationProperties,
                    manifestResources: manifestResources,
                    previousGeneration: baseline,
                    edits: edits,
                    isAddedSymbol: isAddedSymbol);
            }
            catch (NotSupportedException)
            {
                // TODO: better error code (https://github.com/dotnet/roslyn/issues/8910)
                diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, compilation.AssemblyName);
                return new EmitDifferenceResult(success: false, diagnostics: diagnostics.ToReadOnlyAndFree(), baseline: null);
            }

            if (testData != null)
            {
                moduleBeingBuilt.SetMethodTestData(testData.Methods);
                testData.Module = moduleBeingBuilt;
            }

            CSharpDefinitionMap definitionMap = moduleBeingBuilt.PreviousDefinitions;
            SymbolChanges changes = moduleBeingBuilt.Changes;

            EmitBaseline newBaseline = null;

            if (compilation.Compile(
                moduleBeingBuilt,
                emittingPdb: true,
                diagnostics: diagnostics,
                filterOpt: changes.RequiresCompilation,
                cancellationToken: cancellationToken))
            {
                // Map the definitions from the previous compilation to the current compilation.
                // This must be done after compiling above since synthesized definitions
                // (generated when compiling method bodies) may be required.
                EmitBaseline mappedBaseline = MapToCompilation(compilation, moduleBeingBuilt);

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
                    emitOptions.PdbFilePath,
                    cancellationToken);
            }

            return new EmitDifferenceResult(
                success: newBaseline != null,
                diagnostics: diagnostics.ToReadOnlyAndFree(),
                baseline: newBaseline);
        }

        /// <summary>
        /// Return a version of the baseline with all definitions mapped to this compilation.
        /// Definitions from the initial generation, from metadata, are not mapped since
        /// the initial generation is always included as metadata. That is, the symbols from
        /// types, methods, ... in the TypesAdded, MethodsAdded, ... collections are replaced
        /// by the corresponding symbols from the current compilation.
        /// </summary>
        private static EmitBaseline MapToCompilation(
            CSharpCompilation compilation,
            PEDeltaAssemblyBuilder moduleBeingBuilt)
        {
            EmitBaseline previousGeneration = moduleBeingBuilt.PreviousGeneration;
            Debug.Assert(previousGeneration.Compilation != compilation);

            if (previousGeneration.Ordinal == 0)
            {
                // Initial generation, nothing to map. (Since the initial generation
                // is always loaded from metadata in the context of the current
                // compilation, there's no separate mapping step.)
                return previousGeneration;
            }

            System.Collections.Immutable.ImmutableDictionary<Cci.ITypeDefinition, System.Collections.Immutable.ImmutableArray<Cci.ITypeDefinitionMember>> currentSynthesizedMembers = moduleBeingBuilt.GetSynthesizedMembers();

            // Mapping from previous compilation to the current.
            IReadOnlyDictionary<AnonymousTypeKey, AnonymousTypeValue> anonymousTypeMap = moduleBeingBuilt.GetAnonymousTypeMap();
            Symbols.SourceAssemblySymbol sourceAssembly = ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly;
            var sourceContext = new EmitContext((PEModuleBuilder)previousGeneration.PEModuleBuilder, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);
            var otherContext = new EmitContext(moduleBeingBuilt, null, new DiagnosticBag(), metadataOnly: false, includePrivateMembers: true);

            var matcher = new CSharpSymbolMatcher(
                anonymousTypeMap,
                sourceAssembly,
                sourceContext,
                compilation.SourceAssembly,
                otherContext,
                currentSynthesizedMembers);

            System.Collections.Immutable.ImmutableDictionary<Cci.ITypeDefinition, System.Collections.Immutable.ImmutableArray<Cci.ITypeDefinitionMember>> mappedSynthesizedMembers = matcher.MapSynthesizedMembers(previousGeneration.SynthesizedMembers, currentSynthesizedMembers);

            // TODO: can we reuse some data from the previous matcher?
            var matcherWithAllSynthesizedMembers = new CSharpSymbolMatcher(
                anonymousTypeMap,
                sourceAssembly,
                sourceContext,
                compilation.SourceAssembly,
                otherContext,
                mappedSynthesizedMembers);

            return matcherWithAllSynthesizedMembers.MapBaselineToCompilation(
                previousGeneration,
                compilation,
                moduleBeingBuilt,
                mappedSynthesizedMembers);
        }
    }
}

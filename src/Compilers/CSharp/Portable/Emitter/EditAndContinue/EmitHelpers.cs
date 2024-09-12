// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
            catch (NotSupportedException e)
            {
                // TODO: https://github.com/dotnet/roslyn/issues/9004
                diagnostics.Add(ErrorCode.ERR_ModuleEmitFailure, NoLocation.Singleton, compilation.AssemblyName, e.Message);
                return new EmitDifferenceResult(
                    success: false,
                    diagnostics: diagnostics.ToReadOnlyAndFree(),
                    baseline: null,
                    updatedMethods: ImmutableArray<MethodDefinitionHandle>.Empty,
                    changedTypes: ImmutableArray<TypeDefinitionHandle>.Empty);
            }

            if (testData != null)
            {
                moduleBeingBuilt.SetTestData(testData);
            }

            var definitionMap = moduleBeingBuilt.PreviousDefinitions;
            var changes = moduleBeingBuilt.EncSymbolChanges;
            Debug.Assert(changes != null);

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
                // Map the definitions from the previous compilation to the current compilation.
                // This must be done after compiling above since synthesized definitions
                // (generated when compiling method bodies) may be required.
                var mappedBaseline = MapToCompilation(compilation, moduleBeingBuilt);

                newBaseline = compilation.SerializeToDeltaStreams(
                    moduleBeingBuilt,
                    mappedBaseline,
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
            var sourceAssembly = ((CSharpCompilation)previousGeneration.Compilation).SourceAssembly;

            var matcher = new CSharpSymbolMatcher(
                sourceAssembly,
                compilation.SourceAssembly,
                synthesizedTypes,
                currentSynthesizedMembers,
                currentDeletedMembers);

            var mappedSynthesizedMembers = matcher.MapSynthesizedOrDeletedMembers(previousGeneration.SynthesizedMembers, currentSynthesizedMembers, isDeletedMemberMapping: false);

            // Deleted members are mapped the same way as synthesized members, so we can just call the same method.
            var mappedDeletedMembers = matcher.MapSynthesizedOrDeletedMembers(previousGeneration.DeletedMembers, currentDeletedMembers, isDeletedMemberMapping: true);

            // TODO: can we reuse some data from the previous matcher?
            var matcherWithAllSynthesizedMembers = new CSharpSymbolMatcher(
                sourceAssembly,
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

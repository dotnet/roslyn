// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal static class EmitHelpers
    {
        internal static EmitDifferenceResult EmitDifference(
            CSharpCompilation compilation,
            EmitBaseline baseline,
            IEnumerable<SemanticEdit> edits,
            IEnumerable<ResourceEdit> resourceEdits,
            Func<ISymbol, bool> isAddedSymbol,
            Stream metadataStream,
            Stream ilStream,
            Stream pdbStream,
            EmitDifferenceOptions options,
            CompilationTestData? testData,
            CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticBag.GetInstance();

            var emitOptions = EmitOptions.Default.WithDebugInformationFormat(baseline.HasPortablePdb ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb);
            var runtimeMDVersion = compilation.GetRuntimeMetadataVersion(emitOptions, diagnostics);
            var serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMDVersion, baseline.ModuleVersionId);

            var manifestResources = resourceEdits.SelectAsArray(e => e.Resource);

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
                    sourceAssembly: sourceAssembly,
                    otherAssembly: metadataAssembly,
                    otherSynthesizedTypes: metadataSymbols.SynthesizedTypes);

                var previousSourceToMetadata = new CSharpSymbolMatcher(
                    sourceAssembly: previousSourceAssembly,
                    otherAssembly: metadataAssembly,
                    otherSynthesizedTypes: metadataSymbols.SynthesizedTypes);

                CSharpSymbolMatcher? currentSourceToPreviousSource = null;
                if (baseline.Ordinal > 0)
                {
                    Debug.Assert(baseline.PEModuleBuilder != null);

                    currentSourceToPreviousSource = new CSharpSymbolMatcher(
                        sourceAssembly: sourceAssembly,
                        otherAssembly: previousSourceAssembly,
                        otherSynthesizedTypes: baseline.SynthesizedTypes,
                        otherSynthesizedMembers: baseline.SynthesizedMembers,
                        otherDeletedMembers: baseline.DeletedMembers);
                }

                definitionMap = new CSharpDefinitionMap(edits, metadataDecoder, previousSourceToMetadata, sourceToMetadata, currentSourceToPreviousSource, baseline);
                changes = new CSharpSymbolChanges(definitionMap, edits, isAddedSymbol);

                moduleBeingBuilt = new PEDeltaAssemblyBuilder(
                    compilation.SourceAssembly,
                    changes,
                    emitOptions: emitOptions,
                    options: options,
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
                string.Format(CodeAnalysisResources.Type0DoesNotHaveExpectedConstructor, type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));

            return false;
        }
    }
}

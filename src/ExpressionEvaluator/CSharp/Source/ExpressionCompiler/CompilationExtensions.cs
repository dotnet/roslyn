// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class CompilationExtensions
    {
        private static PENamedTypeSymbol GetType(PEModuleSymbol module, TypeDefinitionHandle typeHandle)
        {
            var metadataDecoder = new MetadataDecoder(module);
            return (PENamedTypeSymbol)metadataDecoder.GetTypeOfToken(typeHandle);
        }

        internal static PENamedTypeSymbol GetType(this CSharpCompilation compilation, Guid moduleVersionId, int typeToken)
        {
            return GetType(compilation.GetModule(moduleVersionId), (TypeDefinitionHandle)MetadataTokens.Handle(typeToken));
        }

        internal static PEMethodSymbol GetSourceMethod(this CSharpCompilation compilation, Guid moduleVersionId, MethodDefinitionHandle methodHandle)
        {
            var method = GetMethod(compilation, moduleVersionId, methodHandle);
            var metadataDecoder = new MetadataDecoder((PEModuleSymbol)method.ContainingModule);
            var containingType = method.ContainingType;
            if (GeneratedNameParser.TryParseSourceMethodNameFromGeneratedName(containingType.Name, GeneratedNameKind.StateMachineType, out var sourceMethodName))
            {
                foreach (var member in containingType.ContainingType.GetMembers(sourceMethodName))
                {
                    if (member is PEMethodSymbol candidateMethod &&
                        metadataDecoder.Module.HasStateMachineAttribute(candidateMethod.Handle, out var stateMachineTypeName) &&
                        metadataDecoder.GetTypeSymbolForSerializedType(stateMachineTypeName).OriginalDefinition.Equals(containingType))
                    {
                        return candidateMethod;
                    }
                }
            }
            return method;
        }

        internal static PEMethodSymbol GetMethod(this CSharpCompilation compilation, Guid moduleVersionId, MethodDefinitionHandle methodHandle)
        {
            var module = compilation.GetModule(moduleVersionId);
            var reader = module.Module.MetadataReader;
            var typeHandle = reader.GetMethodDefinition(methodHandle).GetDeclaringType();
            var type = GetType(module, typeHandle);
            var method = (PEMethodSymbol)new MetadataDecoder(module, type).GetMethodSymbolForMethodDefOrMemberRef(methodHandle, type);
            return method;
        }

        internal static PEModuleSymbol GetModule(this CSharpCompilation compilation, Guid moduleVersionId)
        {
            foreach (var pair in compilation.GetBoundReferenceManager().GetReferencedAssemblies())
            {
                var assembly = (AssemblySymbol)pair.Value;
                foreach (var module in assembly.Modules)
                {
                    var m = (PEModuleSymbol)module;
                    var id = m.Module.GetModuleVersionIdOrThrow();
                    if (id == moduleVersionId)
                    {
                        return m;
                    }
                }
            }

            throw new ArgumentException($"No module found with MVID '{moduleVersionId}'", nameof(moduleVersionId));
        }

        internal static CSharpCompilation ToCompilationReferencedModulesOnly(this ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId)
        {
            return ToCompilation(metadataBlocks, moduleVersionId, kind: MakeAssemblyReferencesKind.DirectReferencesOnly);
        }

        internal static CSharpCompilation ToCompilation(this ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId, MakeAssemblyReferencesKind kind)
        {
            var references = metadataBlocks.MakeAssemblyReferences(moduleVersionId, IdentityComparer, kind, out var referencesBySimpleName);
            var options = s_compilationOptions;
            if (referencesBySimpleName != null)
            {
                Debug.Assert(kind == MakeAssemblyReferencesKind.AllReferences);
                var resolver = new EEMetadataReferenceResolver(IdentityComparer, referencesBySimpleName);
                options = options.WithMetadataReferenceResolver(resolver);
            }
            return CSharpCompilation.Create(
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                references: references,
                options: options);
        }

        internal static ReadOnlyCollection<byte>? GetCustomTypeInfoPayload(
            this CSharpCompilation compilation,
            TypeSymbol type,
            int customModifiersCount,
            RefKind refKind)
        {
            return CustomTypeInfo.Encode(
                GetDynamicTransforms(compilation, type, customModifiersCount, refKind),
                GetTupleElementNames(compilation, type));
        }

        private static ReadOnlyCollection<byte>? GetDynamicTransforms(
            this CSharpCompilation compilation,
            TypeSymbol type,
            int customModifiersCount,
            RefKind refKind)
        {
            var builder = ArrayBuilder<bool>.GetInstance();
            CSharpCompilation.DynamicTransformsEncoder.Encode(type, customModifiersCount, refKind, builder, addCustomModifierFlags: true);
            var bytes = builder.Count > 0 && compilation.HasDynamicEmitAttributes(BindingDiagnosticBag.Discarded, Location.None)
                ? DynamicFlagsCustomTypeInfo.ToBytes(builder)
                : null;
            builder.Free();
            return bytes;
        }

        private static ReadOnlyCollection<string?>? GetTupleElementNames(
            this CSharpCompilation compilation,
            TypeSymbol type)
        {
            var builder = ArrayBuilder<string?>.GetInstance();
            var names = CSharpCompilation.TupleNamesEncoder.TryGetNames(type, builder) && compilation.HasTupleNamesAttributes(BindingDiagnosticBag.Discarded, Location.None)
                ? new ReadOnlyCollection<string?>(builder.ToArray())
                : null;
            builder.Free();
            return names;
        }

        internal static readonly AssemblyIdentityComparer IdentityComparer = DesktopAssemblyIdentityComparer.Default;

        // XML file references, #r directives not supported:
        private static readonly CSharpCompilationOptions s_compilationOptions = new CSharpCompilationOptions(
            outputKind: OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            platform: Platform.AnyCpu, // Platform should match PEModule.Machine, in this case I386.
            optimizationLevel: OptimizationLevel.Release,
            assemblyIdentityComparer: IdentityComparer).
            WithMetadataImportOptions(MetadataImportOptions.All).
            WithReferencesSupersedeLowerVersions(true).
            WithTopLevelBinderFlags(
                BinderFlags.SuppressObsoleteChecks |
                BinderFlags.IgnoreAccessibility |
                BinderFlags.UnsafeRegion |
                BinderFlags.UncheckedRegion |
                BinderFlags.AllowMoveableAddressOf |
                BinderFlags.AllowAwaitInUnsafeContext |
                BinderFlags.IgnoreCorLibraryDuplicatedTypes);
    }
}

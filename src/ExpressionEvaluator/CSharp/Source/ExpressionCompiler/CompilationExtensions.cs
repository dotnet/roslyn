// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

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

        internal static PEMethodSymbol GetSourceMethod(this CSharpCompilation compilation, Guid moduleVersionId, int methodToken)
        {
            var methodHandle = (MethodDefinitionHandle)MetadataTokens.Handle(methodToken);
            var method = GetMethod(compilation, moduleVersionId, methodHandle);
            var metadataDecoder = new MetadataDecoder((PEModuleSymbol)method.ContainingModule);
            var containingType = method.ContainingType;
            string sourceMethodName;
            if (GeneratedNames.TryParseSourceMethodNameFromGeneratedName(containingType.Name, GeneratedNameKind.StateMachineType, out sourceMethodName))
            {
                foreach (var member in containingType.ContainingType.GetMembers(sourceMethodName))
                {
                    var candidateMethod = member as PEMethodSymbol;
                    if (candidateMethod != null)
                    {
                        var module = metadataDecoder.Module;
                        methodHandle = candidateMethod.Handle;
                        string stateMachineTypeName;
                        if (module.HasStringValuedAttribute(methodHandle, AttributeDescription.AsyncStateMachineAttribute, out stateMachineTypeName) ||
                            module.HasStringValuedAttribute(methodHandle, AttributeDescription.IteratorStateMachineAttribute, out stateMachineTypeName))
                        {
                            if (metadataDecoder.GetTypeSymbolForSerializedType(stateMachineTypeName).OriginalDefinition.Equals(containingType))
                            {
                                return candidateMethod;
                            }
                        }
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

        internal static CSharpCompilation ToCompilation(this ImmutableArray<MetadataBlock> metadataBlocks)
        {
            var references = metadataBlocks.MakeAssemblyReferences(default(Guid), identityComparer: null);
            return references.ToCompilation();
        }

        internal static CSharpCompilation ToCompilationReferencedModulesOnly(this ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId)
        {
            var references = metadataBlocks.MakeAssemblyReferences(moduleVersionId, IdentityComparer);
            return references.ToCompilation();
        }

        internal static CSharpCompilation ToCompilation(this ImmutableArray<MetadataReference> references)
        {
            return CSharpCompilation.Create(
                assemblyName: ExpressionCompilerUtilities.GenerateUniqueName(),
                references: references,
                options: s_compilationOptions);
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
                BinderFlags.AllowManagedAddressOf |
                BinderFlags.AllowAwaitInUnsafeContext |
                BinderFlags.IgnoreCorLibraryDuplicatedTypes);
    }
}

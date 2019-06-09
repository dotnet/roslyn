// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static partial class MetadataUtilities
    {
        /// <summary>
        /// Group module metadata into assemblies.
        /// If <paramref name="moduleVersionId"/> is set, the
        /// assemblies are limited to those referenced by that module.
        /// </summary>
        internal static ImmutableArray<MetadataReference> MakeAssemblyReferences(
            this ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            AssemblyIdentityComparer identityComparer,
            MakeAssemblyReferencesKind kind,
            out IReadOnlyDictionary<string, ImmutableArray<(AssemblyIdentity, MetadataReference)>> referencesBySimpleName)
        {
            Debug.Assert(kind == MakeAssemblyReferencesKind.AllAssemblies || moduleVersionId != default(Guid));
            Debug.Assert(moduleVersionId == default(Guid) || identityComparer != null);

            // Get metadata for each module.
            var metadataBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
            // Win8 applications contain a reference to Windows.winmd version >= 1.3
            // and perhaps multiple application winmds. At runtime, Windows.winmd
            // is replaced by multiple Windows.*.winmd version >= 1.3. In the EE, we
            // need to map compile-time assembly references to the runtime assemblies
            // supplied by the debugger. To do so, we "merge" all winmds named
            // Windows.*.winmd into a single fake Windows.winmd at runtime.
            // All other (application) winmds are left as is.
            var runtimeWinMdBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
            AssemblyIdentity corLibrary = null;
            foreach (var block in metadataBlocks)
            {
                var metadata = ModuleMetadata.CreateFromMetadata(block.Pointer, block.Size, includeEmbeddedInteropTypes: true);
                try
                {
                    var reader = metadata.MetadataReader;
                    if (corLibrary == null)
                    {
                        bool hasNoAssemblyRefs = reader.AssemblyReferences.Count == 0;
                        // .NET Native uses a corlib with references
                        // (see https://github.com/dotnet/roslyn/issues/13275).
                        if (hasNoAssemblyRefs || metadata.Name.Equals("System.Private.CoreLib.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            // If this assembly declares System.Object, assume it is the corlib.
                            // (Note, it is order dependent which assembly we treat as corlib
                            // if there are multiple assemblies that meet these requirements.
                            // That should be acceptable for evaluating expressions in the EE though.) 
                            if (reader.DeclaresTheObjectClass())
                            {
                                corLibrary = reader.ReadAssemblyIdentityOrThrow();
                                // Compiler layer requires corlib to have no AssemblyRefs.
                                if (!hasNoAssemblyRefs)
                                {
                                    metadata = ModuleMetadata.CreateFromMetadata(block.Pointer, block.Size, includeEmbeddedInteropTypes: true, ignoreAssemblyRefs: true);
                                }
                            }
                        }
                    }
                    if (IsWindowsComponent(reader, metadata.Name))
                    {
                        runtimeWinMdBuilder.Add(metadata);
                    }
                    else
                    {
                        metadataBuilder.Add(metadata);
                    }
                }
                catch (BadImageFormatException)
                {
                    // Ignore modules with "bad" metadata.
                }
            }

            // Index non-primary netmodules by name. Multiple modules may
            // have the same name but we do not have a way to differentiate
            // netmodules other than by name so if there are duplicates, the
            // dictionary value is set to null and references are dropped when
            // generating the containing assembly metadata.
            Dictionary<string, ModuleMetadata> modulesByName = null;
            foreach (var metadata in metadataBuilder)
            {
                if (IsPrimaryModule(metadata))
                {
                    // Primary module. No need to add to dictionary.
                    continue;
                }
                if (modulesByName == null)
                {
                    modulesByName = new Dictionary<string, ModuleMetadata>(); // Requires case-insensitive comparison?
                }
                var name = metadata.Name;
                modulesByName[name] = modulesByName.ContainsKey(name) ? null : metadata;
            }

            // We don't walk the references of winmd assemblies currently. (That would require walking
            // references from the current module and also the winmd assemblies.) So if there are any
            // winmd assemblies, we'll use all assemblies. See https://github.com/dotnet/roslyn/issues/26157.
            if (kind == MakeAssemblyReferencesKind.AllReferences && runtimeWinMdBuilder.Any())
            {
                kind = MakeAssemblyReferencesKind.AllAssemblies;
            }

            // Build assembly references from modules in primary module manifests.
            ImmutableArray<MetadataReference> references;

            if (kind == MakeAssemblyReferencesKind.AllReferences)
            {
                var refsBySimpleName = (kind == MakeAssemblyReferencesKind.AllReferences) ? new Dictionary<string, ImmutableArray<(AssemblyIdentity, MetadataReference)>>(StringComparer.OrdinalIgnoreCase) : null;
                MetadataReference targetReference = null;

                foreach (var metadata in metadataBuilder)
                {
                    if (!IsPrimaryModule(metadata))
                    {
                        continue;
                    }

                    var reference = MakeAssemblyReference(metadata, modulesByName);
                    var reader = metadata.MetadataReader;
                    var identity = reader.ReadAssemblyIdentityOrThrow();
                    if (!refsBySimpleName.TryGetValue(identity.Name, out ImmutableArray<(AssemblyIdentity, MetadataReference)> refs))
                    {
                        refs = ImmutableArray<(AssemblyIdentity, MetadataReference)>.Empty;
                    }
                    refsBySimpleName[identity.Name] = refs.Add((identity, reference));
                    if (targetReference == null &&
                        reader.GetModuleVersionIdOrThrow() == moduleVersionId)
                    {
                        targetReference = reference;
                    }
                }

                var referencesBuilder = ArrayBuilder<MetadataReference>.GetInstance();
                // CommonReferenceManager<TCompilation, TAssemblySymbol>.Bind()
                // expects COR library to be included in the explicit assemblies.
                Debug.Assert(corLibrary != null);
                if (corLibrary != null && refsBySimpleName.TryGetValue(corLibrary.Name, out var corLibraryReferences))
                {
                    referencesBuilder.Add(corLibraryReferences[0].Item2);
                }
                Debug.Assert(targetReference != null);
                if (targetReference != null)
                {
                    referencesBuilder.Add(targetReference);
                }

                references = referencesBuilder.ToImmutableAndFree();
                referencesBySimpleName = refsBySimpleName;
            }
            else
            {
                var referencesBuilder = ArrayBuilder<MetadataReference>.GetInstance();
                var identitiesBuilder = (kind == MakeAssemblyReferencesKind.DirectReferencesOnly) ? ArrayBuilder<AssemblyIdentity>.GetInstance() : null;
                ModuleMetadata targetModule = null;
                AssemblyIdentity intrinsicsAssembly = null;

                foreach (var metadata in metadataBuilder)
                {
                    if (!IsPrimaryModule(metadata))
                    {
                        continue;
                    }

                    var reference = MakeAssemblyReference(metadata, modulesByName);
                    referencesBuilder.Add(reference);

                    if (identitiesBuilder != null)
                    {
                        var reader = metadata.MetadataReader;
                        var identity = reader.ReadAssemblyIdentityOrThrow();
                        identitiesBuilder.Add(identity);
                        if (targetModule == null &&
                            reader.GetModuleVersionIdOrThrow() == moduleVersionId)
                        {
                            targetModule = metadata;
                        }
                        if (intrinsicsAssembly == null &&
                            reader.DeclaresType((r, t) => r.IsPublicNonInterfaceType(t, ExpressionCompilerConstants.IntrinsicAssemblyNamespace, ExpressionCompilerConstants.IntrinsicAssemblyTypeName)))
                        {
                            intrinsicsAssembly = identity;
                        }
                    }
                }

                if (identitiesBuilder != null)
                {
                    // Remove assemblies not directly referenced by the target module.
                    Debug.Assert(targetModule != null);
                    if (targetModule != null)
                    {
                        var referencedModules = ArrayBuilder<AssemblyIdentity>.GetInstance();
                        referencedModules.Add(targetModule.MetadataReader.ReadAssemblyIdentityOrThrow());
                        referencedModules.AddRange(targetModule.MetadataReader.GetReferencedAssembliesOrThrow());
                        // Ensure COR library is included, otherwise any compilation will fail.
                        // (Note, an equivalent assembly may have already been included from
                        // GetReferencedAssembliesOrThrow above but RemoveUnreferencedModules
                        // allows duplicates.)
                        Debug.Assert(corLibrary != null);
                        if (corLibrary != null)
                        {
                            referencedModules.Add(corLibrary);
                        }
                        // Ensure Debugger intrinsic methods assembly is included.
                        if (intrinsicsAssembly != null)
                        {
                            referencedModules.Add(intrinsicsAssembly);
                        }
                        RemoveUnreferencedModules(referencesBuilder, identitiesBuilder, identityComparer, referencedModules);
                        referencedModules.Free();
                    }
                    identitiesBuilder.Free();
                }

                // Any runtime winmd modules were separated out initially. Now add
                // those to a placeholder for the missing compile time module since
                // each of the runtime winmds refer to the compile time module.
                if (runtimeWinMdBuilder.Any())
                {
                    referencesBuilder.Add(MakeCompileTimeWinMdAssemblyMetadata(runtimeWinMdBuilder));
                }

                references = referencesBuilder.ToImmutableAndFree();
                referencesBySimpleName = null;
            }

            metadataBuilder.Free();
            runtimeWinMdBuilder.Free();
            return references;
        }

        /// <summary>
        /// Remove any modules that are not in the set of referenced modules.
        /// If there are duplicates of referenced modules, potentially differing by
        /// version, one instance of the highest version is kept and others dropped.
        /// </summary>
        /// <remarks>
        /// Binding against this reduced set of modules will not handle certain valid cases
        /// where binding to full set would succeed (e.g.: binding to types outside the
        /// referenced modules). And since duplicates are dropped, this will prevent resolving
        /// ambiguities between two versions of the same assembly by using aliases. Also,
        /// there is no attempt here to follow binding redirects or to use the CLR to determine
        /// which version of an assembly to prefer when there are duplicate assemblies.
        /// </remarks>
        private static void RemoveUnreferencedModules(
            ArrayBuilder<MetadataReference> modules,
            ArrayBuilder<AssemblyIdentity> identities,
            AssemblyIdentityComparer identityComparer,
            ArrayBuilder<AssemblyIdentity> referencedModules)
        {
            Debug.Assert(modules.Count == identities.Count);

            var referencedIndices = PooledHashSet<int>.GetInstance();

            // O(n*m) where n = all assemblies and m = referenced assemblies.
            // Can this be more efficient?
            int n = identities.Count;
            int index;
            foreach (var referencedModule in referencedModules)
            {
                index = -1;
                for (int i = 0; i < n; i++)
                {
                    var identity = identities[i];
                    var compareResult = identityComparer.Compare(referencedModule, identity);
                    switch (compareResult)
                    {
                        case AssemblyIdentityComparer.ComparisonResult.NotEquivalent:
                            break;
                        case AssemblyIdentityComparer.ComparisonResult.Equivalent:
                        case AssemblyIdentityComparer.ComparisonResult.EquivalentIgnoringVersion:
                            if ((index < 0) || (identity.Version > identities[index].Version))
                            {
                                index = i;
                            }
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(compareResult);
                    }
                }
                if (index >= 0)
                {
                    referencedIndices.Add(index);
                }
            }

            Debug.Assert(referencedIndices.Count <= modules.Count);
            Debug.Assert(referencedIndices.Count <= referencedModules.Count);

            index = 0;
            for (int i = 0; i < n; i++)
            {
                if (referencedIndices.Contains(i))
                {
                    modules[index] = modules[i];
                    index++;
                }
            }
            modules.Clip(index);

            referencedIndices.Free();
        }

        private static PortableExecutableReference MakeAssemblyReference(ModuleMetadata metadata, Dictionary<string, ModuleMetadata> modulesByName)
        {
            Debug.Assert(metadata.Module.IsManifestModule);

            var builder = ArrayBuilder<ModuleMetadata>.GetInstance();
            builder.Add(metadata);

            // Include any associated netmodules from the manifest.
            if (modulesByName != null)
            {
                try
                {
                    var reader = metadata.MetadataReader;
                    foreach (var handle in reader.AssemblyFiles)
                    {
                        var assemblyFile = reader.GetAssemblyFile(handle);
                        if (assemblyFile.ContainsMetadata)
                        {
                            var name = reader.GetString(assemblyFile.Name);
                            // Find the assembly file in the set of netmodules with that name.
                            // The file may be missing if the file is not a module (say a resource)
                            // or if the module has not been loaded yet. The value will be null
                            // if the name was ambiguous.
                            ModuleMetadata module;
                            if (!modulesByName.TryGetValue(name, out module))
                            {
                                // AssemblyFile names may contain file information (".dll", etc).
                                modulesByName.TryGetValue(GetFileNameWithoutExtension(name), out module);
                            }
                            if (module != null)
                            {
                                builder.Add(module);
                            }
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    // Ignore modules with "bad" metadata.
                }
            }

            var assemblyMetadata = AssemblyMetadata.Create(builder.ToImmutableAndFree());
            return assemblyMetadata.GetReference(embedInteropTypes: false, display: metadata.Name);
        }

        internal static string GetFileNameWithoutExtension(string fileName)
        {
            var lastDotIndex = fileName.LastIndexOf('.');
            var extensionStartIndex = lastDotIndex + 1;
            if ((lastDotIndex > 0) && (extensionStartIndex < fileName.Length))
            {
                var extension = fileName.Substring(extensionStartIndex);
                switch (extension)
                {
                    case "dll":
                    case "exe":
                    case "netmodule":
                    case "winmd":
                        return fileName.Substring(0, lastDotIndex);
                }
            }
            return fileName;
        }

        private static byte[] GetWindowsProxyBytes()
        {
            var assembly = typeof(ExpressionCompiler).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("Microsoft.CodeAnalysis.ExpressionEvaluator.Resources.WindowsProxy.winmd"))
            {
                var bytes = new byte[stream.Length];
                using (var memoryStream = new MemoryStream(bytes))
                {
                    stream.CopyTo(memoryStream);
                }
                return bytes;
            }
        }

        private static PortableExecutableReference MakeCompileTimeWinMdAssemblyMetadata(ArrayBuilder<ModuleMetadata> runtimeModules)
        {
            var metadata = ModuleMetadata.CreateFromImage(GetWindowsProxyBytes());
            var builder = ArrayBuilder<ModuleMetadata>.GetInstance();
            builder.Add(metadata);
            builder.AddRange(runtimeModules);
            var assemblyMetadata = AssemblyMetadata.Create(builder.ToImmutableAndFree());
            return assemblyMetadata.GetReference(embedInteropTypes: false, display: metadata.Name);
        }

        private static bool IsPrimaryModule(ModuleMetadata metadata)
        {
            return metadata.Module.IsManifestModule;
        }

        internal static bool IsWindowsComponent(MetadataReader reader, string moduleName)
        {
            if (reader.MetadataKind != MetadataKind.WindowsMetadata)
            {
                return false;
            }
            if (!IsWindowsComponentName(moduleName))
            {
                return false;
            }
            int majorVersion;
            int minorVersion;
            reader.GetWinMdVersion(out majorVersion, out minorVersion);
            return (majorVersion == 1) && (minorVersion >= 3);
        }

        private static bool IsWindowsComponentName(string moduleName)
        {
            return moduleName.StartsWith("windows.", StringComparison.OrdinalIgnoreCase) &&
                moduleName.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase) &&
                !moduleName.Equals("windows.winmd", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsWindowsAssemblyName(string assemblyName)
        {
            return assemblyName.Equals("windows", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsWindowsAssemblyIdentity(this AssemblyIdentity assemblyIdentity)
        {
            return IsWindowsAssemblyName(assemblyIdentity.Name) &&
                assemblyIdentity.ContentType == System.Reflection.AssemblyContentType.WindowsRuntime;
        }

        internal static ImmutableArray<string> GetLocalNames(this ArrayBuilder<ISymUnmanagedScope> scopes)
        {
            var builder = ArrayBuilder<string>.GetInstance();
            foreach (var scope in scopes)
            {
                foreach (var local in scope.GetLocals())
                {
                    int attributes;
                    local.GetAttributes(out attributes);
                    if (attributes == (int)LocalVariableAttributes.DebuggerHidden)
                    {
                        continue;
                    }

                    builder.SetItem(local.GetSlot(), local.GetName());
                }
            }
            return builder.ToImmutableAndFree();
        }

        internal static ImmutableArray<int> GetSynthesizedMethods(byte[] assembly, string methodName)
        {
            var builder = ArrayBuilder<int>.GetInstance();
            using (var metadata = ModuleMetadata.CreateFromStream(new MemoryStream(assembly)))
            {
                var reader = metadata.MetadataReader;
                foreach (var handle in reader.MethodDefinitions)
                {
                    var methodDef = reader.GetMethodDefinition(handle);
                    if (reader.StringComparer.Equals(methodDef.Name, methodName))
                    {
                        builder.Add(reader.GetToken(handle));
                    }
                }
            }
            return builder.ToImmutableAndFree();
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class MetadataUtilities
    {
        internal const uint COR_E_BADIMAGEFORMAT = 0x8007000b;
        internal const uint CORDBG_E_MISSING_METADATA = 0x80131c35;

        /// <summary>
        /// Group module metadata into assemblies.
        /// If <paramref name="moduleVersionId"/> is set, the
        /// assemblies are limited to those referenced by that module.
        /// </summary>
        internal static ImmutableArray<MetadataReference> MakeAssemblyReferences(
            this ImmutableArray<MetadataBlock> metadataBlocks,
            Guid moduleVersionId,
            AssemblyIdentityComparer identityComparer)
        {
            Debug.Assert((identityComparer == null) || (moduleVersionId != default(Guid)));

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
            foreach (var block in metadataBlocks)
            {
                var metadata = ModuleMetadata.CreateFromMetadata(block.Pointer, block.Size, includeEmbeddedInteropTypes: true);
                try
                {
                    if (IsWindowsComponent(metadata.MetadataReader, metadata.Name))
                    {
                        runtimeWinMdBuilder.Add(metadata);
                    }
                    else
                    {
                        metadataBuilder.Add(metadata);
                    }
                }
                catch (Exception e) when (IsBadMetadataException(e))
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

            // Build assembly references from modules in primary module manifests.
            var referencesBuilder = ArrayBuilder<MetadataReference>.GetInstance();
            var identitiesBuilder = (identityComparer == null) ? null : ArrayBuilder<AssemblyIdentity>.GetInstance();
            AssemblyIdentity corLibrary = null;
            AssemblyIdentity intrinsicsAssembly = null;

            foreach (var metadata in metadataBuilder)
            {
                if (!IsPrimaryModule(metadata))
                {
                    continue;
                }
                if (identitiesBuilder != null)
                {
                    var reader = metadata.MetadataReader;
                    var identity = reader.ReadAssemblyIdentityOrThrow();
                    identitiesBuilder.Add(identity);
                    // If this assembly has no references, and declares
                    // System.Object, assume it is the COR library.
                    if ((corLibrary == null) &&
                        (reader.AssemblyReferences.Count == 0) &&
                        reader.DeclaresTheObjectClass())
                    {
                        corLibrary = identity;
                    }
                    else if ((intrinsicsAssembly == null) &&
                        reader.DeclaresType((r, t) => r.IsPublicNonInterfaceType(t, ExpressionCompilerConstants.IntrinsicAssemblyNamespace, ExpressionCompilerConstants.IntrinsicAssemblyTypeName)))
                    {
                        intrinsicsAssembly = identity;
                    }
                }
                var reference = MakeAssemblyMetadata(metadata, modulesByName);
                referencesBuilder.Add(reference);
            }

            if (identitiesBuilder != null)
            {
                // Remove assemblies not directly referenced by the target module.
                var module = metadataBuilder.FirstOrDefault(m => m.MetadataReader.GetModuleVersionIdOrThrow() == moduleVersionId);
                Debug.Assert(module != null);
                if (module != null)
                {
                    var referencedModules = ArrayBuilder<AssemblyIdentity>.GetInstance();
                    referencedModules.Add(module.MetadataReader.ReadAssemblyIdentityOrThrow());
                    referencedModules.AddRange(module.MetadataReader.GetReferencedAssembliesOrThrow());
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

            metadataBuilder.Free();

            // Any runtime winmd modules were separated out initially. Now add
            // those to a placeholder for the missing compile time module since
            // each of the runtime winmds refer to the compile time module.
            if (runtimeWinMdBuilder.Any())
            {
                referencesBuilder.Add(MakeCompileTimeWinMdAssemblyMetadata(runtimeWinMdBuilder));
            }

            runtimeWinMdBuilder.Free();
            return referencesBuilder.ToImmutableAndFree();
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

        private static PortableExecutableReference MakeAssemblyMetadata(ModuleMetadata metadata, Dictionary<string, ModuleMetadata> modulesByName)
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
                catch (Exception e) when (IsBadMetadataException(e))
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
                var locals = scope.GetLocals();
                foreach (var local in locals)
                {
                    int attributes;
                    local.GetAttributes(out attributes);
                    if (attributes == Cci.PdbWriter.HiddenLocalAttributesValue)
                    {
                        continue;
                    }
                    var slot = local.GetSlot();
                    // Local slot may be less than the current count
                    // if the array was padded with nulls earlier.
                    while (builder.Count <= slot)
                    {
                        builder.Add(null);
                    }
                    Debug.Assert(builder[slot] == null);
                    builder[slot] = local.GetName();
                }
            }
            return builder.ToImmutableAndFree();
        }

        private static bool IsBadMetadataException(Exception e)
        {
            return GetHResult(e) == COR_E_BADIMAGEFORMAT;
        }

        internal static bool IsBadOrMissingMetadataException(Exception e, string moduleName)
        {
            Debug.Assert(moduleName != null);
            switch (GetHResult(e))
            {
                case COR_E_BADIMAGEFORMAT:
                    Debug.WriteLine($"Module '{moduleName}' contains corrupt metadata.");
                    return true;
                case CORDBG_E_MISSING_METADATA:
                    Debug.WriteLine($"Module '{moduleName}' is missing metadata.");
                    return true;
                default:
                    return false;
            }
        }

        private static uint GetHResult(Exception e)
        {
            return unchecked((uint)e.HResult);
        }
    }
}

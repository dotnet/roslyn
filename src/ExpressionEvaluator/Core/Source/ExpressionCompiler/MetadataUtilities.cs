// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.VisualStudio.SymReaderInterop;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class MetadataUtilities
    {
        internal const uint COR_E_BADIMAGEFORMAT = 0x8007000b;
        internal const uint CORDBG_E_MISSING_METADATA = 0x80131c35;

        internal static bool HaveNotChanged(this ImmutableArray<MetadataBlock> metadataBlocks, MetadataContext previous)
        {
            return ((previous != null) && metadataBlocks.SequenceEqual(previous.MetadataBlocks));
        }

        /// <summary>
        /// Group module metadata into assemblies.
        /// </summary>
        internal static ImmutableArray<MetadataReference> MakeAssemblyReferences(this ImmutableArray<MetadataBlock> metadataBlocks)
        {
            // Get metadata for each module.
            var metadataBuilder = ArrayBuilder<ModuleMetadata>.GetInstance();
            // Win8 applications contain a reference to Windows.winmd version >= 1.3
            // and perhaps multiple application winmds. At runtime, Windows.winmd
            // is replaced by multiple Windows.*.winmd version >= 1.3. In the EE, we
            // need to map compile-time assembly references to the runtime assemblies
            // supplied by the debugger. To do so, we “merge” all winmds named
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

            // Build assembly references from modules in primary module
            // manifests. There will be duplicate assemblies if the process has
            // multiple app domains and those duplicates need to be dropped.
            var referencesBuilder = ArrayBuilder<MetadataReference>.GetInstance();
            var moduleIds = PooledHashSet<Guid>.GetInstance();
            foreach (var metadata in metadataBuilder)
            {
                if (!IsPrimaryModule(metadata))
                {
                    continue;
                }
                var mvid = metadata.GetModuleVersionId();
                if (moduleIds.Contains(mvid))
                {
                    continue;
                }
                moduleIds.Add(mvid);
                referencesBuilder.Add(MakeAssemblyMetadata(metadata, modulesByName));
            }
            moduleIds.Free();

            // Any runtime winmd modules were separated out initially. Now add
            // those to a placeholder for the missing compile time module since
            // each of the runtime winmds refer to the compile time module.
            if (runtimeWinMdBuilder.Any())
            {
                referencesBuilder.Add(MakeCompileTimeWinMdAssemblyMetadata(runtimeWinMdBuilder));
            }

            runtimeWinMdBuilder.Free();
            metadataBuilder.Free();
            return referencesBuilder.ToImmutableAndFree();
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
                        var name = reader.GetString(assemblyFile.Name);
                        // Find the assembly file in the set of netmodules with that name.
                        // The file may be missing if the file is not a module (say a resource)
                        // or if the module has not been loaded yet. The value will be null
                        // if the name was ambiguous.
                        ModuleMetadata module;
                        if (modulesByName.TryGetValue(name, out module) && (module != null))
                        {
                            builder.Add(module);
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

        private static PortableExecutableReference MakeCompileTimeWinMdAssemblyMetadata(ArrayBuilder<ModuleMetadata> runtimeModules)
        {
            var metadata = ModuleMetadata.CreateFromImage(Resources.WindowsProxy_winmd);
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

        internal static LocalInfo<TTypeSymbol> GetLocalInfo<TModuleSymbol, TTypeSymbol, TMethodSymbol, TFieldSymbol, TSymbol>(
            this MetadataDecoder<TModuleSymbol, TTypeSymbol, TMethodSymbol, TFieldSymbol, TSymbol> metadataDecoder,
                byte[] signature)
            where TModuleSymbol : class
            where TTypeSymbol : class, TSymbol, ITypeSymbol
            where TMethodSymbol : class, TSymbol, IMethodSymbol
            where TFieldSymbol : class, TSymbol, IFieldSymbol
            where TSymbol : class, ISymbol
        {
            unsafe
            {
                fixed (byte* ptr = signature)
                {
                    var blobReader = new BlobReader(ptr, signature.Length);
                    return metadataDecoder.DecodeLocalVariableOrThrow(ref blobReader);
                }
            }
        }

        /// <summary>
        /// Returns the local info for all locals indexed by slot.
        /// </summary>
        internal static ImmutableArray<LocalInfo<TTypeSymbol>> GetLocalInfo<TModuleSymbol, TTypeSymbol, TMethodSymbol, TFieldSymbol, TSymbol>(
            this MetadataDecoder<TModuleSymbol, TTypeSymbol, TMethodSymbol, TFieldSymbol, TSymbol> metadataDecoder,
            int localSignatureToken)
            where TModuleSymbol : class
            where TTypeSymbol : class, TSymbol, ITypeSymbol
            where TMethodSymbol : class, TSymbol, IMethodSymbol
            where TFieldSymbol : class, TSymbol, IFieldSymbol
            where TSymbol : class, ISymbol
        {
            var handle = MetadataTokens.Handle(localSignatureToken);
            if (handle.IsNil)
            {
                return ImmutableArray<LocalInfo<TTypeSymbol>>.Empty;
            }
            var reader = metadataDecoder.Module.MetadataReader;
            var signature = reader.GetStandaloneSignature((StandaloneSignatureHandle)handle).Signature;
            var blobReader = reader.GetBlobReader(signature);
            return metadataDecoder.DecodeLocalSignatureOrThrow(ref blobReader);
        }

        /// <summary>
        /// Get the set of nested scopes containing the
        /// IL offset from outermost scope to innermost.
        /// </summary>
        internal static void GetScopes(this ISymUnmanagedReader symReader, int methodToken, int methodVersion, int ilOffset, bool isScopeEndInclusive, ArrayBuilder<ISymUnmanagedScope> scopes)
        {
            if (symReader == null)
            {
                return;
            }

            var symMethod = symReader.GetMethodByVersion(methodToken, methodVersion);
            if (symMethod == null)
            {
                return;
            }

            symMethod.GetAllScopes(scopes, ilOffset, isScopeEndInclusive);
        }

        internal static MethodScope GetMethodScope(this ArrayBuilder<ISymUnmanagedScope> scopes, int methodToken, int methodVersion)
        {
            if (scopes.Count == 0)
            {
                return null;
            }
            var scope = scopes.Last();
            return new MethodScope(methodToken, methodVersion, scope.GetStartOffset(), scope.GetEndOffset());
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

        internal static ImmutableArray<NamedLocalConstant> GetConstantSignatures(this ArrayBuilder<ISymUnmanagedScope> scopes)
        {
            var builder = ArrayBuilder<NamedLocalConstant>.GetInstance();
            foreach (var scope in scopes)
            {
                var constants = ((ISymUnmanagedScope2)scope).GetConstants();
                if (constants == null)
                {
                    continue;
                }
                foreach (var constant in constants)
                {
                    NamedLocalConstant value;
                    if (constant.TryGetConstantValue(out value))
                    {
                        builder.Add(value);
                    }
                }
            }
            return builder.ToImmutableAndFree();
        }

        private static ISymUnmanagedConstant[] GetConstants(this ISymUnmanagedScope2 scope)
        {
            int length;
            scope.GetConstants(0, out length, null);
            if (length == 0)
            {
                return null;
            }

            var constants = new ISymUnmanagedConstant[length];
            scope.GetConstants(length, out length, constants);
            return constants;
        }

        private static bool TryGetConstantValue(this ISymUnmanagedConstant constant, out NamedLocalConstant value)
        {
            value = default(NamedLocalConstant);

            int length;
            int hresult = constant.GetName(0, out length, null);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hresult);
            Debug.Assert(length > 0);
            if (length == 0)
            {
                return false;
            }

            var chars = new char[length];
            hresult = constant.GetName(length, out length, chars);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hresult);
            Debug.Assert(chars[length - 1] == 0);
            var name = new string(chars, 0, length - 1);

            constant.GetSignature(0, out length, null);
            Debug.Assert(length > 0);
            if (length == 0)
            {
                return false;
            }

            var signature = new byte[length];
            constant.GetSignature(length, out length, signature);

            object val;
            constant.GetValue(out val);

            var constantValue = GetConstantValue(signature, val);
            value = new NamedLocalConstant(name, signature, constantValue);
            return true;
        }

        private static ConstantValue GetConstantValue(byte[] signature, object value)
        {
            if (signature.Length == 1)
            {
                switch ((SignatureTypeCode)signature[0])
                {
                    case SignatureTypeCode.Object:
                        // Dev12 and Dev14 C#/VB compilers emit (int)0 for (object)null.
                        Debug.Assert(object.Equals(value, 0) || (value == null));
                        return ConstantValue.Null;
                    case SignatureTypeCode.String:
                        return ConstantValue.Create((string)value);
                }
            }

            Debug.Assert(value != null);
            return ConstantValue.Create(value, SpecialTypeExtensions.FromRuntimeTypeOfLiteralValue(value));
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

        internal static string GetUtf8String(this BlobHandle blobHandle, MetadataReader metadataReader)
        {
            return Encoding.UTF8.GetString(metadataReader.GetBlobBytes(blobHandle));
        }
    }
}

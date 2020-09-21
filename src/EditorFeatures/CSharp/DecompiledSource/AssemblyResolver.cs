// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource
{
    internal class AssemblyResolver : IAssemblyResolver
    {
        private readonly Compilation _parentCompilation;
        private readonly Dictionary<string, List<IAssemblySymbol>> _cache = new Dictionary<string, List<IAssemblySymbol>>();
        private readonly StringBuilder _logger;

        public AssemblyResolver(Compilation parentCompilation, StringBuilder logger)
        {
            _parentCompilation = parentCompilation;
            _logger = logger;
            BuildReferenceCache();
            Log(CSharpEditorResources._0_items_in_cache, _cache.Count);

            void BuildReferenceCache()
            {
                foreach (var reference in _parentCompilation.GetReferencedAssemblySymbols())
                {
                    if (!_cache.TryGetValue(reference.Identity.Name, out var list))
                    {
                        list = new List<IAssemblySymbol>();
                        _cache.Add(reference.Identity.Name, list);
                    }

                    list.Add(reference);
                }
            }
        }

        bool IAssemblyResolver.IsGacAssembly(IAssemblyReference reference)
        {
            // This method is not called by the decompiler
            throw new NotSupportedException();
        }

        public PEFile Resolve(IAssemblyReference name)
        {
            Log("------------------");
            Log(CSharpEditorResources.Resolve_0, name.FullName);

            // First, find the correct list of assemblies by name
            if (!_cache.TryGetValue(name.Name, out var assemblies))
            {
                Log(CSharpEditorResources.Could_not_find_by_name_0, name.FullName);
                return null;
            }

            // If we have only one assembly available, just use it.
            // This is necessary, because in most cases there is only one assembly,
            // but still might have a version different from what the decompiler asks for.
            if (assemblies.Count == 1)
            {
                Log(CSharpEditorResources.Found_single_assembly_0, assemblies[0]);
                if (assemblies[0].Identity.Version != name.Version)
                {
                    Log(CSharpEditorResources.WARN_Version_mismatch_Expected_0_Got_1, name.Version, assemblies[0].Identity.Version);
                }
                return MakePEFile(assemblies[0]);
            }

            // There are multiple assemblies
            Log(CSharpEditorResources.Found_0_assemblies_for_1, assemblies.Count, name.Name);

            // Get an exact match or highest version match from the list
            IAssemblySymbol highestVersion = null;
            IAssemblySymbol exactMatch = null;

            var publicKeyTokenOfName = name.PublicKeyToken ?? Array.Empty<byte>();

            foreach (var assembly in assemblies)
            {
                Log(assembly.Identity.GetDisplayName());
                var version = assembly.Identity.Version;
                var publicKeyToken = assembly.Identity.PublicKey;
                if (version == name.Version && publicKeyToken.SequenceEqual(publicKeyTokenOfName))
                {
                    exactMatch = assembly;
                    Log(CSharpEditorResources.Found_exact_match_0, assembly);
                }
                else if (highestVersion == null || highestVersion.Identity.Version < version)
                {
                    highestVersion = assembly;
                    Log(CSharpEditorResources.Found_higher_version_match_0, assembly);
                }
            }

            var chosen = exactMatch ?? highestVersion;
            Log(CSharpEditorResources.Chosen_version_0, chosen);
            return MakePEFile(chosen);

            PEFile MakePEFile(IAssemblySymbol assembly)
            {
                // reference assemblies should be fine here, we only need the metadata of references.
                var reference = _parentCompilation.GetMetadataReference(assembly);
                Log(CSharpEditorResources.Load_from_0, reference.Display);
                return new PEFile(reference.Display, PEStreamOptions.PrefetchMetadata);
            }
        }

        public PEFile ResolveModule(PEFile mainModule, string moduleName)
        {
            Log("-------------");
            Log(CSharpEditorResources.Resolve_module_0_of_1, moduleName, mainModule.FullName);

            // Primitive implementation to support multi-module assemblies
            // where all modules are located next to the main module.
            var baseDirectory = Path.GetDirectoryName(mainModule.FileName);
            var moduleFileName = Path.Combine(baseDirectory, moduleName);
            if (!File.Exists(moduleFileName))
            {
                Log(CSharpEditorResources.Module_not_found);
                return null;
            }

            Log(CSharpEditorResources.Load_from_0, moduleFileName);
            return new PEFile(moduleFileName, PEStreamOptions.PrefetchMetadata);
        }

        private void Log(string format, params object[] args)
            => _logger.AppendFormat(format + Environment.NewLine, args);
    }
}

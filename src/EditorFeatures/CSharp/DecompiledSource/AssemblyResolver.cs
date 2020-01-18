// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Log("'{0}' items in cache", _cache.Count);

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

        public PEFile Resolve(IAssemblyReference name)
        {
            Log("------------------");
            Log("Resolve: '{0}'", name.FullName);

            // First, find the correct list of assemblies by name
            if (!_cache.TryGetValue(name.Name, out var assemblies))
            {
                Log("Could not find by name: '{0}'", name.FullName);
                return null;
            }

            // If we have only one assembly available, just use it.
            // This is necessary, because in most cases there is only one assembly,
            // but still might have a version different from what the decompiler asks for.
            if (assemblies.Count == 1)
            {
                Log("Found single assembly: '{0}'", assemblies[0]);
                if (assemblies[0].Identity.Version != name.Version)
                {
                    Log("WARN: Version mismatch. Expected: '{0}', Got: '{1}'!", name.Version, assemblies[0].Identity.Version);
                }
                return MakePEFile(assemblies[0]);
            }

            // There are multiple assemblies
            Log("Found '{0}' assemblies for '{1}':", assemblies.Count, name.Name);

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
                    Log("Found exact match: '{0}'", assembly);
                }
                else if (highestVersion == null || highestVersion.Identity.Version < version)
                {
                    highestVersion = assembly;
                    Log("Found higher version match: '{0}'", assembly);
                }
            }

            var chosen = exactMatch ?? highestVersion;
            Log("Choosing version: '{0}'", chosen);
            return MakePEFile(chosen);

            PEFile MakePEFile(IAssemblySymbol assembly)
            {
                // reference assemblies should be fine here, we only need the metadata of references.
                var reference = _parentCompilation.GetMetadataReference(assembly);
                Log("Load from: '{0}'", reference.Display);
                return new PEFile(reference.Display, PEStreamOptions.PrefetchMetadata);
            }
        }

        public PEFile ResolveModule(PEFile mainModule, string moduleName)
        {
            Log("-------------");
            Log("ResolveModule: '{0}' of '{1}'", moduleName, mainModule.FullName);

            // Primitive implementation to support multi-module assemblies
            // where all modules are located next to the main module.
            var baseDirectory = Path.GetDirectoryName(mainModule.FileName);
            var moduleFileName = Path.Combine(baseDirectory, moduleName);
            if (!File.Exists(moduleFileName))
            {
                Log("Not found!");
                return null;
            }

            Log("Found '{0}'", moduleFileName);
            return new PEFile(moduleFileName, PEStreamOptions.PrefetchMetadata);
        }

        private void Log(string format, params object[] args)
        {
            _logger.AppendFormat(format + Environment.NewLine, args);
        }
    }
}

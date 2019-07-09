// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource
{
    internal class AssemblyResolver : IAssemblyResolver
    {
        private readonly Compilation parentCompilation;
        private static readonly Version zeroVersion = new Version(0, 0, 0, 0);

        public AssemblyResolver(Compilation parentCompilation)
        {
            this.parentCompilation = parentCompilation;
        }

        public PEFile Resolve(IAssemblyReference name)
        {
            foreach (var assembly in parentCompilation.GetReferencedAssemblySymbols())
            {
                // First, find the correct IAssemblySymbol by name and PublicKeyToken.
                if (assembly.Identity.Name != name.Name
                    || !assembly.Identity.PublicKeyToken.SequenceEqual(name.PublicKeyToken ?? Array.Empty<byte>()))
                {
                    continue;
                }

                // Normally we skip versions that do not match, except if the reference is "mscorlib" (see comments below)
                // or if the name.Version is '0.0.0.0'. This is because we require the metadata of all transitive references
                // and modules, to achieve best decompilation results.
                // In the case of .NET Standard projects for example, the 'netstandard' reference contains no references
                // with actual versions. All versions are '0.0.0.0', therefore we have to ignore those version numbers,
                // and can just use the references provided by Roslyn instead.
                if (assembly.Identity.Version != name.Version && name.Version != zeroVersion
                    && !string.Equals("mscorlib", assembly.Identity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // MSBuild treats mscorlib special for the purpose of assembly resolution/unification, where all
                    // versions of the assembly are considered equal. The same policy is adopted here.
                    continue;
                }

                // reference assemblies should be fine here, we only need the metadata of references.
                var reference = parentCompilation.GetMetadataReference(assembly);
                return new PEFile(reference.Display, PEStreamOptions.PrefetchMetadata);
            }

            // not found
            return null;
        }

        public PEFile ResolveModule(PEFile mainModule, string moduleName)
        {
            // Primitive implementation to support multi-module assemblies
            // where all modules are located next to the main module.
            var baseDirectory = Path.GetDirectoryName(mainModule.FileName);
            var moduleFileName = Path.Combine(baseDirectory, moduleName);
            if (!File.Exists(moduleFileName))
            {
                return null;
            }

            return new PEFile(moduleFileName, PEStreamOptions.PrefetchMetadata);
        }
    }

}

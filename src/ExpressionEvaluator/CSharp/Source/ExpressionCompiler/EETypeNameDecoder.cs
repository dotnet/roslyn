// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EETypeNameDecoder : TypeNameDecoder<PEModuleSymbol, TypeSymbol>
    {
        private readonly CSharpCompilation _compilation;

        internal EETypeNameDecoder(CSharpCompilation compilation, PEModuleSymbol moduleSymbol) :
            base(SymbolFactory.Instance, moduleSymbol)
        {
            _compilation = compilation;
        }

        protected override int GetIndexOfReferencedAssembly(AssemblyIdentity identity)
        {
            // Find assembly matching identity.
            int index = this.Module.GetReferencedAssemblies().IndexOf(identity);
            if (index >= 0)
            {
                return index;
            }
            if (identity.IsWindowsComponent())
            {
                // Find placeholder Windows.winmd assembly (created
                // in MetadataUtilities.MakeAssemblyReferences).
                var assemblies = this.Module.GetReferencedAssemblySymbols();
                index = assemblies.IndexOf((assembly, unused) => assembly.Identity.IsWindowsRuntime(), (object)null);
                if (index >= 0)
                {
                    // Find module in Windows.winmd matching identity.
                    var modules = assemblies[index].Modules;
                    var moduleIndex = modules.IndexOf((m, id) => id.Equals(GetComponentAssemblyIdentity(m)), identity);
                    if (moduleIndex >= 0)
                    {
                        return index;
                    }
                }
            }
            return -1;
        }

        protected override bool IsContainingAssembly(AssemblyIdentity identity)
        {
            return false;
        }

        protected override TypeSymbol LookupNestedTypeDefSymbol(TypeSymbol container, ref MetadataTypeName emittedName)
        {
            return container.LookupMetadataType(ref emittedName);
        }

        protected override TypeSymbol LookupTopLevelTypeDefSymbol(int referencedAssemblyIndex, ref MetadataTypeName emittedName)
        {
            var assembly = this.Module.GetReferencedAssemblySymbols()[referencedAssemblyIndex];
            return assembly.LookupTopLevelMetadataType(ref emittedName, digThroughForwardedTypes: true);
        }

        protected override TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            return this.moduleSymbol.LookupTopLevelMetadataType(ref emittedName, out isNoPiaLocalType);
        }

        private static AssemblyIdentity GetComponentAssemblyIdentity(ModuleSymbol module)
        {
            return ((PEModuleSymbol)module).Module.ReadAssemblyIdentityOrThrow();
        }

        private ModuleSymbol Module => _compilation.Assembly.Modules.Single();
    }
}

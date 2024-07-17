// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            int index = Module.GetReferencedAssemblies().IndexOf(identity);
            if (index >= 0)
            {
                return index;
            }
            if (identity.IsWindowsComponent())
            {
                // Find placeholder Windows.winmd assembly (created
                // in MetadataUtilities.MakeAssemblyReferences).
                var assemblies = Module.GetReferencedAssemblySymbols();
                index = assemblies.IndexOf(predicate: (assembly, unused) => assembly.Identity.IsWindowsRuntime(), arg: (object?)null);
                if (index >= 0)
                {
                    // Find module in Windows.winmd matching identity.
                    var modules = assemblies[index].Modules;
                    var moduleIndex = modules.IndexOf(predicate: (m, id) => id.Equals(GetComponentAssemblyIdentity(m)), arg: identity);
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
            return container.LookupMetadataType(ref emittedName) ??
                       new MissingMetadataTypeSymbol.Nested((NamedTypeSymbol)container, ref emittedName);
        }

        protected override TypeSymbol LookupTopLevelTypeDefSymbol(int referencedAssemblyIndex, ref MetadataTypeName emittedName)
        {
            var assembly = Module.GetReferencedAssemblySymbol(referencedAssemblyIndex);
            // GetReferencedAssemblySymbol should not return null since referencedAssemblyIndex
            // was obtained from GetIndexOfReferencedAssembly above.
            return assembly.LookupDeclaredOrForwardedTopLevelMetadataType(ref emittedName, visitedAssemblies: null);
        }

        protected override TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            return moduleSymbol.LookupTopLevelMetadataTypeWithNoPiaLocalTypeUnification(ref emittedName, out isNoPiaLocalType);
        }

        private static AssemblyIdentity GetComponentAssemblyIdentity(ModuleSymbol module)
        {
            return ((PEModuleSymbol)module).Module.ReadAssemblyIdentityOrThrow();
        }

        private ModuleSymbol Module => _compilation.Assembly.Modules.Single();
    }
}

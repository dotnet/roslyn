// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        protected sealed class AssemblyDataForAssemblyBeingBuilt : AssemblyData
        {
            private readonly AssemblyIdentity _assemblyIdentity;

            // assemblies referenced directly by the assembly:
            private readonly ImmutableArray<AssemblyData> _referencedAssemblyData;

            // all referenced assembly names including assemblies referenced by modules:
            private readonly ImmutableArray<AssemblyIdentity> _referencedAssemblies;

            public AssemblyDataForAssemblyBeingBuilt(
                AssemblyIdentity identity,
                ImmutableArray<AssemblyData> referencedAssemblyData,
                ImmutableArray<PEModule> modules)
            {
                Debug.Assert(identity != null);
                Debug.Assert(!referencedAssemblyData.IsDefault);

                _assemblyIdentity = identity;

                _referencedAssemblyData = referencedAssemblyData;

                var refs = ArrayBuilder<AssemblyIdentity>.GetInstance(referencedAssemblyData.Length + modules.Length); //approximate size
                foreach (AssemblyData data in referencedAssemblyData)
                {
                    refs.Add(data.Identity);
                }

                // add assembly names from modules:
                for (int i = 1; i <= modules.Length; i++)
                {
                    refs.AddRange(modules[i - 1].ReferencedAssemblies);
                }

                _referencedAssemblies = refs.ToImmutableAndFree();
            }

            public override AssemblyIdentity Identity
            {
                get
                {
                    return _assemblyIdentity;
                }
            }

            public override ImmutableArray<AssemblyIdentity> AssemblyReferences
            {
                get
                {
                    return _referencedAssemblies;
                }
            }

            public override ImmutableArray<TAssemblySymbol> AvailableSymbols
            {
                get
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            public override AssemblyReferenceBinding[] BindAssemblyReferences(
                MultiDictionary<string, (AssemblyData DefinitionData, int DefinitionIndex)> assemblies,
                AssemblyIdentityComparer assemblyIdentityComparer)
            {
                var boundReferences = new AssemblyReferenceBinding[_referencedAssemblies.Length];

                for (int i = 0; i < _referencedAssemblyData.Length; i++)
                {
                    Debug.Assert(assemblies[_referencedAssemblyData[i].Identity.Name].Contains((_referencedAssemblyData[i], i + 1)));
                    boundReferences[i] = new AssemblyReferenceBinding(_referencedAssemblyData[i].Identity, i + 1);
                }

                // resolve references coming from linked modules:
                for (int i = _referencedAssemblyData.Length; i < _referencedAssemblies.Length; i++)
                {
                    boundReferences[i] = ResolveReferencedAssembly(
                        _referencedAssemblies[i],
                        assemblies,
                        resolveAgainstAssemblyBeingBuilt: false, // references from added modules shouldn't resolve against the assembly being built (definition #0)
                        assemblyIdentityComparer);
                }

                return boundReferences;
            }

            public override bool IsMatchingAssembly(TAssemblySymbol? assembly)
            {
                throw ExceptionUtilities.Unreachable();
            }

            public override bool ContainsNoPiaLocalTypes
            {
                get
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            public override bool IsLinked
            {
                get
                {
                    return false;
                }
            }

            public override bool DeclaresTheObjectClass
            {
                get
                {
                    return false;
                }
            }

            public override Compilation? SourceCompilation => null;
        }
    }
}

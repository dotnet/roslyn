// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

            // [0] is the number of assembly names in referencedAssemblies that are direct references specified in referencedAssemblyData.
            // [i] is the number of references coming from module moduleInfo[i-1]. These names are also added to referencedAssemblies.
            private readonly int[] _moduleReferenceCounts;

            public AssemblyDataForAssemblyBeingBuilt(
                AssemblyIdentity identity,
                ImmutableArray<AssemblyData> referencedAssemblyData,
                ImmutableArray<PEModule> modules)
            {
                Debug.Assert(identity != null);
                Debug.Assert(!referencedAssemblyData.IsDefault);

                _assemblyIdentity = identity;

                _referencedAssemblyData = referencedAssemblyData;

                int count = modules.Length;

                var refs = ArrayBuilder<AssemblyIdentity>.GetInstance(referencedAssemblyData.Length + count); //approximate size
                foreach (AssemblyData data in referencedAssemblyData)
                {
                    refs.Add(data.Identity);
                }

                _moduleReferenceCounts = new int[count + 1]; // Plus one for the source module.
                _moduleReferenceCounts[0] = refs.Count;

                // add assembly names from modules:
                for (int i = 1; i <= count; i++)
                {
                    var module = modules[i - 1];

                    _moduleReferenceCounts[i] = module.ReferencedAssemblies.Length;
                    refs.AddRange(module.ReferencedAssemblies);
                }

                _referencedAssemblies = refs.ToImmutableAndFree();
            }

            public int[] ReferencesCountForModule
            {
                get
                {
                    return _moduleReferenceCounts;
                }
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

            public override IEnumerable<TAssemblySymbol> AvailableSymbols
            {
                get
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            public override AssemblyReferenceBinding[] BindAssemblyReferences(
                ImmutableArray<AssemblyData> assemblies,
                AssemblyIdentityComparer assemblyIdentityComparer)
            {
                var boundReferences = new AssemblyReferenceBinding[_referencedAssemblies.Length];

                for (int i = 0; i < _referencedAssemblyData.Length; i++)
                {
                    Debug.Assert(ReferenceEquals(_referencedAssemblyData[i], assemblies[i + 1]));
                    boundReferences[i] = new AssemblyReferenceBinding(assemblies[i + 1].Identity, i + 1);
                }

                // resolve references coming from linked modules:
                for (int i = _referencedAssemblyData.Length; i < _referencedAssemblies.Length; i++)
                {
                    boundReferences[i] = ResolveReferencedAssembly(
                        _referencedAssemblies[i],
                        assemblies,
                        assemblyIdentityComparer,
                        okToResolveAgainstCompilationBeingCreated: false); // references from added modules shouldn't resolve against the assembly we are building.
                }

                return boundReferences;
            }

            public override bool IsMatchingAssembly(TAssemblySymbol assembly)
            {
                throw ExceptionUtilities.Unreachable;
            }

            public override bool ContainsNoPiaLocalTypes
            {
                get
                {
                    throw ExceptionUtilities.Unreachable;
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

            public override Compilation SourceCompilation => null;
        }
    }
}

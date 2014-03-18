// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis
{
    partial class CommonReferenceManager<TCompilation, TAssemblySymbol>
    {
        protected sealed class AssemblyDataForAssemblyBeingBuilt : AssemblyData
        {
            private readonly AssemblyIdentity assemblyIdentity;

            // assemblies referenced directly by the assembly:
            private readonly ImmutableArray<AssemblyData> referencedAssemblyData;

            // all referenced assembly names including assemblies referenced by modules:
            private readonly ImmutableArray<AssemblyIdentity> referencedAssemblies;

            // [0] is the number of assembly names in referencedAssemblies that are direct references specified in referencedAssemblyData.
            // [i] is the number of references coming from module moduleInfo[i-1]. These names are also added to referencedAssemblies.
            private readonly int[] moduleReferenceCounts;

            public AssemblyDataForAssemblyBeingBuilt(
                AssemblyIdentity identity,
                ImmutableArray<AssemblyData> referencedAssemblyData,
                ImmutableArray<PEModule> modules)
            {
                Debug.Assert(identity != null);
                Debug.Assert(!referencedAssemblyData.IsDefault);

                assemblyIdentity = identity;

                this.referencedAssemblyData = referencedAssemblyData;

                int count = modules.Length;

                var refs = ArrayBuilder<AssemblyIdentity>.GetInstance(referencedAssemblyData.Length + count); //approximate size
                foreach (AssemblyData data in referencedAssemblyData)
                {
                    refs.Add(data.Identity);
                }

                moduleReferenceCounts = new int[count + 1]; // Plus one for the source module.
                moduleReferenceCounts[0] = refs.Count;

                // add assembly names from modules:
                for (int i = 1; i <= count; i++)
                {
                    var module = modules[i - 1];

                    moduleReferenceCounts[i] = module.ReferencedAssemblies.Length;
                    refs.AddRange(module.ReferencedAssemblies);
                }

                referencedAssemblies = refs.ToImmutableAndFree();
            }

            public int[] ReferencesCountForModule
            {
                get
                {
                    return moduleReferenceCounts;
                }
            }

            public override AssemblyIdentity Identity
            {
                get
                {
                    return assemblyIdentity;
                }
            }

            public override ImmutableArray<AssemblyIdentity> AssemblyReferences
            {
                get
                {
                    return referencedAssemblies;
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
                var boundReferences = new AssemblyReferenceBinding[referencedAssemblies.Length];

                for (int i = 0; i < referencedAssemblyData.Length; i++)
                {
                    Debug.Assert(ReferenceEquals(referencedAssemblyData[i], assemblies[i + 1]));
                    boundReferences[i] = new AssemblyReferenceBinding(assemblies[i + 1].Identity, i + 1);
                }

                // resolve references coming from linked modules:
                for (int i = referencedAssemblyData.Length; i < referencedAssemblies.Length; i++)
                {
                    boundReferences[i] = ResolveReferencedAssembly(
                        referencedAssemblies[i],
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
        }
    }
}
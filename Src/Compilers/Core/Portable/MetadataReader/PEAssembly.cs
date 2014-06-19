// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal sealed class PEAssembly
    {
        /// <summary>
        /// All assemblies this assembly references.
        /// </summary>
        /// <remarks>
        /// A concatenation of assemblies referenced by each module in the order they are listed in <see cref="modules"/>.
        /// </remarks>
        internal readonly ImmutableArray<AssemblyIdentity> AssemblyReferences;

        /// <summary>
        /// The number of assemblies referenced by each module in <see cref="modules"/>.
        /// </summary>
        internal readonly ImmutableArray<int> ModuleReferenceCounts;

        private readonly ImmutableArray<PEModule> modules;

        /// <summary>
        /// Assembly identity read from Assembly table, or null if the table is empty.
        /// </summary>
        private readonly AssemblyIdentity identity;

        /// <summary>
        /// Using <see cref="ThreeState"/> for atomicity.
        /// </summary>
        private ThreeState lazyContainsNoPiaLocalTypes;

        private ThreeState lazyDeclaresTheObjectClass;

        // We need to store reference for to keep the metadata alive while symbols have reference to PEAssembly.
        private readonly AssemblyMetadata owner;

        //Maps from simple name to list of public keys. If an IVT attribute specifies no public
        //key, the list contains one element with an empty value
        private Dictionary<string, List<ImmutableArray<byte>>> lazyInternalsVisibleToMap;

        /// <exception cref="BadImageFormatException"/>
        internal PEAssembly(AssemblyMetadata owner, ImmutableArray<PEModule> modules)
        {
            Debug.Assert(!modules.IsDefault);
            Debug.Assert(modules.Length > 0);

            this.identity = modules[0].ReadAssemblyIdentityOrThrow();

            var refs = ArrayBuilder<AssemblyIdentity>.GetInstance();
            int[] refCounts = new int[modules.Length];

            for (int i = 0; i < modules.Length; i++)
            {
                ImmutableArray<AssemblyIdentity> refsForModule = modules[i].ReferencedAssemblies;
                refCounts[i] = refsForModule.Length;
                refs.AddRange(refsForModule);
            }

            this.modules = modules;
            this.AssemblyReferences = refs.ToImmutableAndFree();
            this.ModuleReferenceCounts = refCounts.AsImmutableOrNull();
            this.owner = owner;
        }

        internal Handle Handle
        {
            get
            {
                return Handle.AssemblyDefinition;
            }
        }

        internal PEModule ManifestModule
        {
            get { return Modules[0]; }
        }

        internal ImmutableArray<PEModule> Modules
        {
            get
            {
                return modules;
            }
        }

        internal AssemblyIdentity Identity
        {
            get
            {
                return identity;
            }
        }

        internal bool ContainsNoPiaLocalTypes()
        {
            if (this.lazyContainsNoPiaLocalTypes == ThreeState.Unknown)
            {
                foreach (PEModule module in Modules)
                {
                    if (module.ContainsNoPiaLocalTypes())
                    {
                        this.lazyContainsNoPiaLocalTypes = ThreeState.True;
                        return true;
                    }
                }

                this.lazyContainsNoPiaLocalTypes = ThreeState.False;
            }

            return this.lazyContainsNoPiaLocalTypes == ThreeState.True;
        }

        private Dictionary<string, List<ImmutableArray<byte>>> BuildInternalsVisibleToMap()
        {
            var ivtMap = new Dictionary<string, List<ImmutableArray<byte>>>(StringComparer.OrdinalIgnoreCase);
            foreach (string attrVal in Modules[0].GetInternalsVisibleToAttributeValues(Handle))
            {
                AssemblyIdentity identity;
                if (AssemblyIdentity.TryParseDisplayName(attrVal, out identity))
                {
                    List<ImmutableArray<byte>> keys;
                    if (ivtMap.TryGetValue(identity.Name, out keys))
                        keys.Add(identity.PublicKey);
                    else
                    {
                        keys = new List<ImmutableArray<byte>>();
                        keys.Add(identity.PublicKey);
                        ivtMap[identity.Name] = keys;
                    }
                }
                else
                {
                    // Dev10 C# reports WRN_InvalidAssemblyName and Dev10 VB reports ERR_FriendAssemblyNameInvalid but
                    // we have no way to do that from here.  Since the absence of these diagnostics does not impact the
                    // user experience enough to justify the work required to produce them, we will simply omit them
                    // (DevDiv #15099, #14348).
                }
            }

            return ivtMap;
        }

        internal IEnumerable<ImmutableArray<byte>> GetInternalsVisibleToPublicKeys(string simpleName)
        {
            if (lazyInternalsVisibleToMap == null)
                Interlocked.CompareExchange(ref lazyInternalsVisibleToMap, BuildInternalsVisibleToMap(), null);

            List<ImmutableArray<byte>> result;

            lazyInternalsVisibleToMap.TryGetValue(simpleName, out result);

            return result ?? SpecializedCollections.EmptyEnumerable<ImmutableArray<byte>>();
        }

        internal bool DeclaresTheObjectClass
        {
            get
            {
                if (this.lazyDeclaresTheObjectClass == ThreeState.Unknown)
                {
                    if (!modules[0].FindSystemObjectTypeDef().IsNil)
                    {
                        this.lazyDeclaresTheObjectClass = ThreeState.True;
                        return true;
                    }

                    this.lazyDeclaresTheObjectClass = ThreeState.False;
                }

                return this.lazyDeclaresTheObjectClass == ThreeState.True;
            }
        }
    }
}

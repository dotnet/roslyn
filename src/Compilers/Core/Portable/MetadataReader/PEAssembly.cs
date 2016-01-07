// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// A concatenation of assemblies referenced by each module in the order they are listed in <see cref="_modules"/>.
        /// </remarks>
        internal readonly ImmutableArray<AssemblyIdentity> AssemblyReferences;

        /// <summary>
        /// The number of assemblies referenced by each module in <see cref="_modules"/>.
        /// </summary>
        internal readonly ImmutableArray<int> ModuleReferenceCounts;

        private readonly ImmutableArray<PEModule> _modules;

        /// <summary>
        /// Assembly identity read from Assembly table, or null if the table is empty.
        /// </summary>
        private readonly AssemblyIdentity _identity;

        /// <summary>
        /// Using <see cref="ThreeState"/> for atomicity.
        /// </summary>
        private ThreeState _lazyContainsNoPiaLocalTypes;

        private ThreeState _lazyDeclaresTheObjectClass;

        // We need to store reference for to keep the metadata alive while symbols have reference to PEAssembly.
        private readonly AssemblyMetadata _owner;

        //Maps from simple name to list of public keys. If an IVT attribute specifies no public
        //key, the list contains one element with an empty value
        private Dictionary<string, List<ImmutableArray<byte>>> _lazyInternalsVisibleToMap;

        /// <exception cref="BadImageFormatException"/>
        internal PEAssembly(AssemblyMetadata owner, ImmutableArray<PEModule> modules)
        {
            Debug.Assert(!modules.IsDefault);
            Debug.Assert(modules.Length > 0);

            _identity = modules[0].ReadAssemblyIdentityOrThrow();

            var refs = ArrayBuilder<AssemblyIdentity>.GetInstance();
            int[] refCounts = new int[modules.Length];

            for (int i = 0; i < modules.Length; i++)
            {
                ImmutableArray<AssemblyIdentity> refsForModule = modules[i].ReferencedAssemblies;
                refCounts[i] = refsForModule.Length;
                refs.AddRange(refsForModule);
            }

            _modules = modules;
            this.AssemblyReferences = refs.ToImmutableAndFree();
            this.ModuleReferenceCounts = refCounts.AsImmutableOrNull();
            _owner = owner;
        }

        internal EntityHandle Handle
        {
            get
            {
                return EntityHandle.AssemblyDefinition;
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
                return _modules;
            }
        }

        internal AssemblyIdentity Identity
        {
            get
            {
                return _identity;
            }
        }

        internal bool ContainsNoPiaLocalTypes()
        {
            if (_lazyContainsNoPiaLocalTypes == ThreeState.Unknown)
            {
                foreach (PEModule module in Modules)
                {
                    if (module.ContainsNoPiaLocalTypes())
                    {
                        _lazyContainsNoPiaLocalTypes = ThreeState.True;
                        return true;
                    }
                }

                _lazyContainsNoPiaLocalTypes = ThreeState.False;
            }

            return _lazyContainsNoPiaLocalTypes == ThreeState.True;
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
            if (_lazyInternalsVisibleToMap == null)
                Interlocked.CompareExchange(ref _lazyInternalsVisibleToMap, BuildInternalsVisibleToMap(), null);

            List<ImmutableArray<byte>> result;

            _lazyInternalsVisibleToMap.TryGetValue(simpleName, out result);

            return result ?? SpecializedCollections.EmptyEnumerable<ImmutableArray<byte>>();
        }

        internal bool DeclaresTheObjectClass
        {
            get
            {
                if (_lazyDeclaresTheObjectClass == ThreeState.Unknown)
                {
                    var value = _modules[0].MetadataReader.DeclaresTheObjectClass();
                    _lazyDeclaresTheObjectClass = value.ToThreeState();
                }

                return _lazyDeclaresTheObjectClass == ThreeState.True;
            }
        }

        public MetadataId MetadataId => _owner.Id;
    }
}

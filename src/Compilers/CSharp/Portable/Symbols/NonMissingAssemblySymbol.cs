// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A <see cref="NonMissingAssemblySymbol"/> is a special kind of <see cref="AssemblySymbol"/> that represents
    /// an assembly that is not missing, i.e. the "real" thing.
    /// </summary>
    internal abstract class NonMissingAssemblySymbol : AssemblySymbol
    {
        /// <summary>
        /// This is a cache similar to the one used by MetaImport::GetTypeByName
        /// in native compiler. The difference is that native compiler pre-populates 
        /// the cache when it loads types. Here we are populating the cache only
        /// with things we looked for, so that next time we are looking for the same 
        /// thing, the lookup is fast. This cache also takes care of TypeForwarders. 
        /// Gives about 8% win on subsequent lookups in some scenarios.     
        /// </summary>
        /// <remarks></remarks>
        private readonly ConcurrentDictionary<MetadataTypeName.Key, NamedTypeSymbol> _emittedNameToTypeMap =
            new ConcurrentDictionary<MetadataTypeName.Key, NamedTypeSymbol>();

        private NamespaceSymbol _globalNamespace;

        /// <summary>
        /// Does this symbol represent a missing assembly.
        /// </summary>
        internal sealed override bool IsMissing
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the merged root namespace that contains all namespaces and types defined in the modules
        /// of this assembly. If there is just one module in this assembly, this property just returns the 
        /// GlobalNamespace of that module.
        /// </summary>
        public sealed override NamespaceSymbol GlobalNamespace
        {
            get
            {
                if ((object)_globalNamespace == null)
                {
                    // Get the root namespace from each module, and merge them all together. If there is only one, 
                    // then MergedNamespaceSymbol.Create will just return that one.

                    IEnumerable<NamespaceSymbol> allGlobalNamespaces = from m in Modules select m.GlobalNamespace;
                    var result = MergedNamespaceSymbol.Create(new NamespaceExtent(this),
                                                        null,
                                                        allGlobalNamespaces.AsImmutable());
                    Interlocked.CompareExchange(ref _globalNamespace, result, null);
                }

                return _globalNamespace;
            }
        }

        /// <summary>
        /// Lookup a top level type referenced from metadata, names should be
        /// compared case-sensitively.  Detect cycles during lookup.
        /// </summary>
        /// <param name="emittedName">
        /// Full type name, possibly with generic name mangling.
        /// </param>
        /// <param name="visitedAssemblies">
        /// List of assemblies lookup has already visited (since type forwarding can introduce cycles).
        /// </param>
        /// <param name="digThroughForwardedTypes">
        /// Take forwarded types into account.
        /// </param>
        internal sealed override NamedTypeSymbol LookupTopLevelMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies, bool digThroughForwardedTypes)
        {
            NamedTypeSymbol result = null;

            // This is a cache similar to the one used by MetaImport::GetTypeByName in native
            // compiler. The difference is that native compiler pre-populates the cache when it
            // loads types. Here we are populating the cache only with things we looked for, so that
            // next time we are looking for the same thing, the lookup is fast. This cache also
            // takes care of TypeForwarders. Gives about 8% win on subsequent lookups in some
            // scenarios.     
            //    
            // CONSIDER !!!
            //
            // However, it is questionable how often subsequent lookup by name  is going to happen.
            // Currently it doesn't happen for TypeDef tokens at all, for TypeRef tokens, the
            // lookup by name is done once and the result is cached. So, multiple lookups by name
            // for the same type are going to happen only in these cases:
            // 1) Resolving GetType() in attribute application, type is encoded by name.
            // 2) TypeRef token isn't reused within the same module, i.e. multiple TypeRefs point to
            //    the same type.
            // 3) Different Module refers to the same type, lookup once per Module (with exception of #2).
            // 4) Multitargeting - retargeting the type to a different version of assembly
            result = LookupTopLevelMetadataTypeInCache(ref emittedName);

            if ((object)result != null)
            {
                // We only cache result equivalent to digging through type forwarders, which
                // might produce an forwarder specific ErrorTypeSymbol. We don't want to 
                // return that error symbol, unless digThroughForwardedTypes is true.
                if (digThroughForwardedTypes || (!result.IsErrorType() && (object)result.ContainingAssembly == (object)this))
                {
                    return result;
                }

                // According to the cache, the type wasn't found, or isn't declared in this assembly (forwarded).
                return new MissingMetadataTypeSymbol.TopLevel(this.Modules[0], ref emittedName);
            }
            else
            {
                // Now we will look for the type in each module of the assembly and pick the first type
                // we find, this is what native VB compiler does.

                var modules = this.Modules;
                var count = modules.Length;
                var i = 0;

                result = modules[i].LookupTopLevelMetadataType(ref emittedName);

                if (result is MissingMetadataTypeSymbol)
                {
                    for (i = 1; i < count; i++)
                    {
                        var newResult = modules[i].LookupTopLevelMetadataType(ref emittedName);

                        // Hold on to the first missing type result, unless we found the type.
                        if (!(newResult is MissingMetadataTypeSymbol))
                        {
                            result = newResult;
                            break;
                        }
                    }
                }

                bool foundMatchInThisAssembly = (i < count);

                Debug.Assert(!foundMatchInThisAssembly || (object)result.ContainingAssembly == (object)this);

                if (!foundMatchInThisAssembly && digThroughForwardedTypes)
                {
                    // We didn't find the type
                    System.Diagnostics.Debug.Assert(result is MissingMetadataTypeSymbol);

                    NamedTypeSymbol forwarded = TryLookupForwardedMetadataTypeWithCycleDetection(ref emittedName, visitedAssemblies);
                    if ((object)forwarded != null)
                    {
                        result = forwarded;
                    }
                }

                System.Diagnostics.Debug.Assert((object)result != null);

                // Add result of the lookup into the cache
                if (digThroughForwardedTypes || foundMatchInThisAssembly)
                {
                    CacheTopLevelMetadataType(ref emittedName, result);
                }

                return result;
            }
        }

        internal override abstract NamedTypeSymbol TryLookupForwardedMetadataTypeWithCycleDetection(ref MetadataTypeName emittedName, ConsList<AssemblySymbol> visitedAssemblies);

        private NamedTypeSymbol LookupTopLevelMetadataTypeInCache(ref MetadataTypeName emittedName)
        {
            NamedTypeSymbol result = null;
            if (_emittedNameToTypeMap.TryGetValue(emittedName.ToKey(), out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal NamedTypeSymbol CachedTypeByEmittedName(string emittedname)
        {
            MetadataTypeName mdName = MetadataTypeName.FromFullName(emittedname);
            return _emittedNameToTypeMap[mdName.ToKey()];
        }

        /// <summary>
        /// For test purposes only.
        /// </summary>
        internal int EmittedNameToTypeMapCount
        {
            get
            {
                return _emittedNameToTypeMap.Count;
            }
        }

        private void CacheTopLevelMetadataType(
            ref MetadataTypeName emittedName,
            NamedTypeSymbol result)
        {
            NamedTypeSymbol result1 = null;
            result1 = _emittedNameToTypeMap.GetOrAdd(emittedName.ToKey(), result);
            System.Diagnostics.Debug.Assert(TypeSymbol.Equals(result1, result, TypeCompareKind.ConsiderEverything2)); // object identity may differ in error cases
        }
    }
}

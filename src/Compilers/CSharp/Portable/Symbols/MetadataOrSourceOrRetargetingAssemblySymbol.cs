// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class MetadataOrSourceOrRetargetingAssemblySymbol
        : NonMissingAssemblySymbol
    {
        /// <summary>
        /// Determine whether this assembly has been granted access to <paramref name="potentialGiverOfAccess"></paramref>.
        /// Assumes that the public key has been determined. The result will be cached.
        /// </summary>
        /// <param name="potentialGiverOfAccess"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        internal IVTConclusion MakeFinalIVTDetermination(AssemblySymbol potentialGiverOfAccess, bool assertUnexpectedGiver)
        {
            IVTConclusion result;
            if (AssembliesToWhichInternalAccessHasBeenDetermined.TryGetValue(potentialGiverOfAccess, out result))
                return result;

            result = IVTConclusion.NoRelationshipClaimed;

            // returns an empty list if there was no IVT attribute at all for the given name
            // A name w/o a key is represented by a list with an entry that is empty
            IEnumerable<ImmutableArray<byte>> publicKeys = potentialGiverOfAccess.GetInternalsVisibleToPublicKeys(this.Name);

            // We have an easy out here. Suppose the assembly wanting access is 
            // being compiled as a module. You can only strong-name an assembly. So we are going to optimistically 
            // assume that it is going to be compiled into an assembly with a matching strong name, if necessary.
            if (publicKeys.Any() && this.IsNetModule())
            {
                return IVTConclusion.Match;
            }

            // look for one that works, if none work, then return the failure for the last one examined.
            foreach (var key in publicKeys)
            {
                // We pass the public key of this assembly explicitly so PerformIVTCheck does not need
                // to get it from this.Identity, which would trigger an infinite recursion.
                result = potentialGiverOfAccess.Identity.PerformIVTCheck(this.PublicKey, key);
                Debug.Assert(result != IVTConclusion.NoRelationshipClaimed);

                if (result == IVTConclusion.Match || result == IVTConclusion.OneSignedOneNot)
                {
                    break;
                }
            }

            if (IsDirectlyOrIndirectlyReferenced(potentialGiverOfAccess))
            {
                AssembliesToWhichInternalAccessHasBeenDetermined.TryAdd(potentialGiverOfAccess, result);
            }
            else
            {
                Debug.Assert(!assertUnexpectedGiver, "We are performing a check for an unrelated assembly which likely indicates a bug.");
            }

            return result;
        }

        protected bool IsDirectlyOrIndirectlyReferenced(AssemblySymbol potentialGiverOfAccess)
        {
            var checkedAssemblies = PooledHashSet<AssemblySymbol>.GetInstance();
            var queue = ArrayBuilder<AssemblySymbol>.GetInstance(
                this.Modules[0].ReferencedAssemblySymbols.Length +
                (this is SourceAssemblySymbol { DeclaringCompilation.PreviousSubmission: { } } ? 1 : 0));

            checkedAssemblies.Add(this);
            bool found = checkReferences(this, potentialGiverOfAccess, checkedAssemblies, queue);

            while (!found && queue.Count != 0)
            {
                found = checkReferences(queue.Pop(), potentialGiverOfAccess, checkedAssemblies, queue);
            }

            checkedAssemblies.Free();
            queue.Free();
            return found;

            static bool checkReferences(AssemblySymbol current, AssemblySymbol potentialGiverOfAccess, PooledHashSet<AssemblySymbol> checkedAssemblies, ArrayBuilder<AssemblySymbol> queue)
            {
                foreach (var module in current.Modules)
                {
                    foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
                    {
                        if (checkReference(referencedAssembly, potentialGiverOfAccess, checkedAssemblies, queue))
                        {
                            return true;
                        }
                    }
                }

                if (current is SourceAssemblySymbol { DeclaringCompilation.PreviousSubmission.Assembly: { } previous } &&
                    checkReference(previous, potentialGiverOfAccess, checkedAssemblies, queue))
                {
                    return true;
                }

                return false;
            }

            static bool checkReference(AssemblySymbol referencedAssembly, AssemblySymbol potentialGiverOfAccess, PooledHashSet<AssemblySymbol> checkedAssemblies, ArrayBuilder<AssemblySymbol> queue)
            {
                if ((object)referencedAssembly == potentialGiverOfAccess)
                {
                    return true;
                }

                if (checkedAssemblies.Add(referencedAssembly))
                {
                    queue.Push(referencedAssembly);
                }

                return false;
            }
        }

        //EDMAURER This is a cache mapping from assemblies which we have analyzed whether or not they grant
        //internals access to us to the conclusion reached.
        private ConcurrentDictionary<AssemblySymbol, IVTConclusion> _assembliesToWhichInternalAccessHasBeenAnalyzed;

        internal ConcurrentDictionary<AssemblySymbol, IVTConclusion> AssembliesToWhichInternalAccessHasBeenDetermined
        {
            get
            {
                if (_assembliesToWhichInternalAccessHasBeenAnalyzed == null)
                    Interlocked.CompareExchange(ref _assembliesToWhichInternalAccessHasBeenAnalyzed, new ConcurrentDictionary<AssemblySymbol, IVTConclusion>(), null);
                return _assembliesToWhichInternalAccessHasBeenAnalyzed;
            }
        }

        internal virtual bool IsNetModule() => false;
    }
}

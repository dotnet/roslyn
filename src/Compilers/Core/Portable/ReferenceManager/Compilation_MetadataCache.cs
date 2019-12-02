// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Compilation
    {
        /// <summary>
        /// The list of RetargetingAssemblySymbol objects created for this Compilation. 
        /// RetargetingAssemblySymbols are created when some other compilation references this one, 
        /// but the other references provided are incompatible with it. For example, compilation C1 
        /// references v1 of Lib.dll and compilation C2 references C1 and v2 of Lib.dll. In this
        /// case, in context of C2, all types from v1 of Lib.dll leaking through C1 (through method 
        /// signatures, etc.) must be retargeted to the types from v2 of Lib.dll. This is what 
        /// RetargetingAssemblySymbol is responsible for. In the example above, modules in C2 do not 
        /// reference C1.AssemblySymbol, but reference a special RetargetingAssemblySymbol created
        /// for C1 by ReferenceManager.
        ///  
        /// WeakReference is used to allow RetargetingAssemblySymbol to be collected when they become unused.
        /// 
        /// Guarded by <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </summary>
        private readonly WeakList<IAssemblySymbolInternal> _retargetingAssemblySymbols = new WeakList<IAssemblySymbolInternal>();

        /// <summary>
        /// Adds given retargeting assembly for this compilation into the cache.
        /// <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/> must be locked while calling this method.
        /// </summary>
        internal void CacheRetargetingAssemblySymbolNoLock(IAssemblySymbolInternal assembly)
        {
            _retargetingAssemblySymbols.Add(assembly);
        }

        /// <summary>
        /// Adds cached retargeting symbols into the given list.
        /// <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/> must be locked while calling this method.
        /// </summary>
        internal void AddRetargetingAssemblySymbolsNoLock<T>(List<T> result) where T : IAssemblySymbolInternal
        {
            foreach (var symbol in _retargetingAssemblySymbols)
            {
                result.Add((T)symbol);
            }
        }

        // for testing only
        internal WeakList<IAssemblySymbolInternal> RetargetingAssemblySymbols
        {
            get { return _retargetingAssemblySymbols; }
        }
    }
}

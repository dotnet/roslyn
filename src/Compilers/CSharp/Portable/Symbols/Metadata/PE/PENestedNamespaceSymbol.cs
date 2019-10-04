// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all, but Global, namespaces imported from a PE/module.
    /// Namespaces that differ only by casing in name are not merged.
    /// </summary>
    /// <remarks></remarks>
    internal sealed class PENestedNamespaceSymbol
        : PENamespaceSymbol
    {
        /// <summary>
        /// The parent namespace. There is always one, Global namespace contains all
        /// top level namespaces. 
        /// </summary>
        /// <remarks></remarks>
        private readonly PENamespaceSymbol _containingNamespaceSymbol;

        /// <summary>
        /// The name of the namespace.
        /// </summary>
        /// <remarks></remarks>
        private readonly string _name;

        /// <summary>
        /// The sequence of groups of TypeDef row ids for types contained within the namespace, 
        /// recursively including those from nested namespaces. The row ids are grouped by the 
        /// fully-qualified namespace name case-sensitively. There could be multiple groups 
        /// for each fully-qualified namespace name. The groups are sorted by their 
        /// key in case-sensitive manner. Empty string is used as namespace name for types 
        /// immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        /// will be in several first IGroupings.
        /// 
        /// This member is initialized by constructor and is cleared in EnsureAllMembersLoaded 
        /// as soon as symbols for children are created.
        /// </summary>
        /// <remarks></remarks>
        private IEnumerable<IGrouping<string, TypeDefinitionHandle>>? _typesByNS;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">
        /// Name of the namespace, must be not empty.
        /// </param>
        /// <param name="containingNamespace">
        /// Containing namespace.
        /// </param>
        /// <param name="typesByNS">
        /// The sequence of groups of TypeDef row ids for types contained within the namespace, 
        /// recursively including those from nested namespaces. The row ids are grouped by the 
        /// fully-qualified namespace name case-sensitively. There could be multiple groups 
        /// for each fully-qualified namespace name. The groups are sorted by their 
        /// key in case-sensitive manner. Empty string is used as namespace name for types 
        /// immediately contained within Global namespace. Therefore, all types in this namespace, if any, 
        /// will be in several first IGroupings.
        /// </param>
        internal PENestedNamespaceSymbol(
            string name,
            PENamespaceSymbol containingNamespace,
            IEnumerable<IGrouping<string, TypeDefinitionHandle>> typesByNS)
        {
            RoslynDebug.Assert(name != null);
            RoslynDebug.Assert((object)containingNamespace != null);
            RoslynDebug.Assert(typesByNS != null);

            _containingNamespaceSymbol = containingNamespace;
            _name = name;
            _typesByNS = typesByNS;
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingNamespaceSymbol; }
        }

        internal override PEModuleSymbol ContainingPEModule
        {
            get { return _containingNamespaceSymbol.ContainingPEModule; }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        public override bool IsGlobalNamespace
        {
            get
            {
                return false;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return ContainingPEModule.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _containingNamespaceSymbol.ContainingPEModule;
            }
        }

        protected override void EnsureAllMembersLoaded()
        {
            var typesByNS = _typesByNS;

            if (lazyTypes == null || lazyNamespaces == null)
            {
                RoslynDebug.Assert(typesByNS != null);
                LoadAllMembers(typesByNS);
                Interlocked.Exchange(ref _typesByNS, null);
            }
        }

        internal sealed override CSharpCompilation? DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}

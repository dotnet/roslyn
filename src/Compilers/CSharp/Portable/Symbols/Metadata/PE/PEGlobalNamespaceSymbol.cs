// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal sealed class PEGlobalNamespaceSymbol
        : PENamespaceSymbol
    {
        /// <summary>
        /// The module containing the namespace.
        /// </summary>
        /// <remarks></remarks>
        private readonly PEModuleSymbol _moduleSymbol;

        internal PEGlobalNamespaceSymbol(PEModuleSymbol moduleSymbol)
        {
            Debug.Assert((object)moduleSymbol != null);
            _moduleSymbol = moduleSymbol;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _moduleSymbol;
            }
        }

        internal override PEModuleSymbol ContainingPEModule
        {
            get
            {
                return _moduleSymbol;
            }
        }

        public override string Name
        {
            get
            {
                return string.Empty;
            }
        }

        public override bool IsGlobalNamespace
        {
            get
            {
                return true;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _moduleSymbol.ContainingAssembly;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return _moduleSymbol;
            }
        }

        protected override void EnsureAllMembersLoaded()
        {
            if (lazyTypes == null || lazyNamespaces == null)
            {
                IEnumerable<IGrouping<string, TypeDefinitionHandle>> groups;

                try
                {
                    groups = _moduleSymbol.Module.GroupTypesByNamespaceOrThrow(System.StringComparer.Ordinal);
                }
                catch (BadImageFormatException)
                {
                    groups = SpecializedCollections.EmptyEnumerable<IGrouping<string, TypeDefinitionHandle>>();
                }

                LoadAllMembers(groups);
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }
    }
}

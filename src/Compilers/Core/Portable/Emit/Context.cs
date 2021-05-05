// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal readonly struct EmitContext
    {
        public readonly CommonPEModuleBuilder Module;
        public readonly SyntaxNode? SyntaxNode;
        public readonly RebuildData? RebuildData;
        public readonly DiagnosticBag Diagnostics;
        private readonly Flags _flags;

        public bool IncludePrivateMembers => (_flags & Flags.IncludePrivateMembers) != 0;
        public bool MetadataOnly => (_flags & Flags.MetadataOnly) != 0;
        public bool IsRefAssembly => MetadataOnly && !IncludePrivateMembers;

        public EmitContext(CommonPEModuleBuilder module, SyntaxNode? syntaxNode, DiagnosticBag diagnostics, bool metadataOnly, bool includePrivateMembers)
            : this(module, diagnostics, metadataOnly, includePrivateMembers, syntaxNode, rebuildData: null)
        {
        }

        public EmitContext(
            CommonPEModuleBuilder module,
            DiagnosticBag diagnostics,
            bool metadataOnly,
            bool includePrivateMembers,
            SyntaxNode? syntaxNode = null,
            RebuildData? rebuildData = null)
        {
            Debug.Assert(rebuildData is null || !metadataOnly);
            RebuildData = rebuildData;
            Debug.Assert(module != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(includePrivateMembers || metadataOnly);

            Module = module;
            SyntaxNode = syntaxNode;
            RebuildData = rebuildData;
            Diagnostics = diagnostics;

            Flags flags = Flags.None;
            if (metadataOnly)
            {
                flags |= Flags.MetadataOnly;
            }
            if (includePrivateMembers)
            {
                flags |= Flags.IncludePrivateMembers;
            }
            _flags = flags;
        }

        [Flags]
        private enum Flags
        {
            None = 0,
            MetadataOnly = 1,
            IncludePrivateMembers = 2,
        }
    }
}

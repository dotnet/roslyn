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
        public readonly SyntaxNode? SyntaxNodeOpt;
        public readonly RebuildData? RebuildDataOpt;
        public readonly DiagnosticBag Diagnostics;
        private readonly Flags _flags;

        public bool IncludePrivateMembers => (_flags & Flags.IncludePrivateMembers) != 0;
        public bool MetadataOnly => (_flags & Flags.MetadataOnly) != 0;
        public bool IsRefAssembly => MetadataOnly && !IncludePrivateMembers;

        public EmitContext(CommonPEModuleBuilder module, SyntaxNode? syntaxNodeOpt, DiagnosticBag diagnostics, bool metadataOnly, bool includePrivateMembers)
        {
            Debug.Assert(module != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(includePrivateMembers || metadataOnly);

            Module = module;
            SyntaxNodeOpt = syntaxNodeOpt;
            RebuildDataOpt = null;
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

        public EmitContext(
            CommonPEModuleBuilder module,
            SyntaxNode? syntaxNodeOpt = null,
            RebuildData? rebuildDataOpt = null,
            DiagnosticBag? diagnostics = null,
            bool metadataOnly = false,
            bool includePrivateMembers = true)
            : this(module, syntaxNodeOpt, diagnostics ?? new DiagnosticBag(), metadataOnly, includePrivateMembers)
        {
            Debug.Assert(rebuildDataOpt is null || !metadataOnly);
            RebuildDataOpt = rebuildDataOpt;
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

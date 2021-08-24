// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized local variable with a val escape scope.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class SynthesizedLocalWithValEscape : SynthesizedLocal
    {
        public SynthesizedLocalWithValEscape(
            MethodSymbol? containingMethod,
            TypeWithAnnotations typeWithAnnotations,
            SynthesizedLocalKind kind,
            uint valEscapeScope,
            SyntaxNode? syntaxOpt = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None
#if DEBUG
            ,
            [CallerLineNumber] int createdAtLineNumber = 0,
            [CallerFilePath] string? createdAtFilePath = null
#endif
            ) : base(containingMethod, typeWithAnnotations, kind, syntaxOpt, isPinned, refKind
#if DEBUG
                     , createdAtLineNumber, createdAtFilePath
#endif
            )
        {
            ValEscapeScope = valEscapeScope;
        }

        internal override uint ValEscapeScope { get; }
    }
}

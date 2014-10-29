// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Represents a single fix. This is essentially a tuple
    /// that holds on to a <see cref="CodeAction"/> and the set of
    /// <see cref="Diagnostic"/>s that this <see cref="CodeAction"/> will fix.
    /// </summary>
    internal class CodeFix
    {
        internal readonly CodeAction Action;
        internal readonly ImmutableArray<Diagnostic> Diagnostics;

        internal CodeFix(CodeAction action, Diagnostic diagnostic)
        {
            this.Action = action;
            this.Diagnostics = ImmutableArray.Create(diagnostic);
        }

        internal CodeFix(CodeAction action, IEnumerable<Diagnostic> diagnostics)
        {
            this.Action = action;
            this.Diagnostics = diagnostics.ToImmutableArray();
        }
    }
}
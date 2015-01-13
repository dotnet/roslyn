// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        /// <summary>
        /// This is the diagnostic that will show up in the preview pane header when a particular fix
        /// is selected in the light bulb menu. We also group all fixes with the same <see cref="PrimaryDiagnostic"/>
        /// together (into a single SuggestedActionSet) in the light bulb menu.
        /// </summary>
        /// <remarks>
        /// A given fix can fix one or more diagnostics. However, our light bulb UI (preview pane, grouping
        /// of fixes in the light bulb menu etc.) currently keeps things simple and pretends that
        /// each fix fixes a single <see cref="PrimaryDiagnostic"/>.
        /// 
        /// Implementation-wise the <see cref="PrimaryDiagnostic"/> is always the first diagnostic that
        /// the <see cref="CodeFixProvider"/> supplied when registering the fix (<see 
        /// cref="CodeFixContext.RegisterFix(CodeAction, IEnumerable{Diagnostic})"/>). This could change
        /// in the future, if we decide to change the UI to depict the true mapping between fixes and diagnostics
        /// or if we decide to use some other heuristic to determine the <see cref="PrimaryDiagnostic"/>.
        /// </remarks>
        internal Diagnostic PrimaryDiagnostic
        {
            get
            {
                return Diagnostics[0];
            }
        }

        internal CodeFix(CodeAction action, Diagnostic diagnostic)
        {
            this.Action = action;
            this.Diagnostics = ImmutableArray.Create(diagnostic);
        }

        internal CodeFix(CodeAction action, ImmutableArray<Diagnostic> diagnostics)
        {
            Debug.Assert(!diagnostics.IsDefault);
            this.Action = action;
            this.Diagnostics = diagnostics;
        }
    }
}
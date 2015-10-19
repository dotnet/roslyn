// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SwitchBinder
    {
        private BoundSwitchStatement BindPatternSwitch(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // Note that this is technically a lowering of the switch statement. We should instead
            // be *binding* it here and *lowering* it later, in the lowering phase. The two are
            // combined here for the purposes of prototyping the pattern-matching feature.
            // The separation will be useful in
            // the full implementation to simplify subsumption checking (no case is subsumed by the
            // totality of previous cases).

            var boundSwitchExpression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);

            throw new NotImplementedException("switch binder for pattern matching");
        }

        private bool IsPatternSwitch(SwitchStatementSyntax node)
        {
            foreach (var section in node.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label.Kind() == SyntaxKind.CaseMatchLabel) return true;
                }
            }

            // We transate all switch statements as a series of if-then-else for the prototype.
            // In the full implementation we should have unified handling of "traditional" and
            // pattern-based switch statements.
            return Compilation.Feature("patterns") != null;
        }
    }
}

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
            throw new NotImplementedException();
        }

        private bool IsPatternSwitch(SwitchStatementSyntax node)
        {
            return false;
            //bool result = false;

            //foreach (var section in node.Sections)
            //{
            //    foreach (var label in section.Labels)
            //    {
            //        if (label.Kind() == SyntaxKind.CaseMatchLabel) result = true;
            //        CaseMatchLabelSyntax s;
            //    }
            //}
            //// We transate all switch statements as a series of if-then-else for the prototype.
            //// In the full implementation we should have unified handling of "traditional" and
            //// pattern-based switch statements.
            //return true;
        }
    }
}

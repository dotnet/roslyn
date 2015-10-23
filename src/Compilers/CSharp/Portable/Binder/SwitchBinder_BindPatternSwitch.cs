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
        private BoundMatchStatement BindPatternSwitch(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            var boundSwitchExpression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            // TODO: any constraints on a switch expression must be enforced here. For example,
            // it must have a type (not be target-typed, lambda, null, etc)

            GeneratedLabelSymbol defaultLabelSymbol;
            ImmutableArray<BoundMatchSection> boundMatchSections = BindMatchSections(boundSwitchExpression, node.Sections, originalBinder, out defaultLabelSymbol, diagnostics);

            return new BoundMatchStatement(node, boundSwitchExpression, Locals, LocalFunctions, boundMatchSections, this.BreakLabel, defaultLabelSymbol);
            throw new NotImplementedException("switch binder for pattern matching");
        }

        private ImmutableArray<BoundMatchSection> BindMatchSections(BoundExpression boundSwitchExpression, SyntaxList<SwitchSectionSyntax> sections, Binder originalBinder, out GeneratedLabelSymbol defaultLabelSymbol, DiagnosticBag diagnostics)
        {
            defaultLabelSymbol = null;

            // Bind match sections
            var boundMatchSectionsBuilder = ArrayBuilder<BoundMatchSection>.GetInstance();
            foreach (var sectionSyntax in sections)
            {
                boundMatchSectionsBuilder.Add(BindMatchSection(boundSwitchExpression, sectionSyntax, originalBinder, ref defaultLabelSymbol, diagnostics));
            }

            return boundMatchSectionsBuilder.ToImmutableAndFree();
        }

        private BoundMatchSection BindMatchSection(BoundExpression boundSwitchExpression, SwitchSectionSyntax node, Binder originalBinder, ref GeneratedLabelSymbol defaultLabelSymbol, DiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundMatchLabel>.GetInstance();
            var labelSymbol = new GeneratedLabelSymbol("case");
            foreach (var labelSyntax in node.Labels)
            {
                BoundMatchLabel boundLabel = BindMatchSectionLabel(boundSwitchExpression, labelSymbol, labelSyntax, ref defaultLabelSymbol, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (var statement in node.Statements)
            {
                boundStatementsBuilder.Add(originalBinder.BindStatement(statement, diagnostics));
            }

            return new BoundMatchSection(node, boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        private BoundMatchLabel BindMatchSectionLabel(BoundExpression boundSwitchExpression, GeneratedLabelSymbol labelSym, SwitchLabelSyntax node, ref GeneratedLabelSymbol defaultLabelSymbol, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CaseMatchLabel:
                    {
                        var matchLabelSyntax = (CaseMatchLabelSyntax)node;
                        return new BoundMatchLabel(node, labelSym, 
                            BindPattern(matchLabelSyntax.Pattern, boundSwitchExpression, boundSwitchExpression.Type, node.HasErrors, diagnostics),
                            matchLabelSyntax.Condition != null ? BindBooleanExpression(matchLabelSyntax.Condition, diagnostics) : null, node.HasErrors);
                    }

                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        var boundLabelExpression = BindValue(caseLabelSyntax.Value, diagnostics, BindValueKind.RValue);
                        // TODO: check compatibility of the bound switch expression with the label
                        // TODO: check that it is a constant.
                        var pattern = new BoundConstantPattern(node, boundLabelExpression, node.HasErrors);
                        return new BoundMatchLabel(node, labelSym, pattern, null, node.HasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var defaultLabelSyntax = (DefaultSwitchLabelSyntax)node;
                        var pattern = new BoundWildcardPattern(node);
                        // TODO: check that there is only one "default" label in a match statement.
                        defaultLabelSymbol = labelSym;
                        return new BoundMatchLabel(node, labelSym, pattern, null, node.HasErrors); // note that this is semantically last!
                    }

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private bool IsPatternSwitch(SwitchStatementSyntax node)
        {
            // we add a feature flag to help test binding and lowering of "traditional" non-erroneous switch statements
            // using the infrastructure added to support pattern matching.
            if (Compilation.Feature("match") != null) return true;

            foreach (var section in node.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label.Kind() == SyntaxKind.CaseMatchLabel) return true;
                }
            }

            return false;
            //// We transate all switch statements as a series of if-then-else for the prototype.
            //// In the full implementation we should have unified handling of "traditional" and
            //// pattern-based switch statements.
            //return Compilation.Feature("patterns") != null;
        }
    }
}

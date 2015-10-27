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
        private BoundMatchStatement BindPatternSwitch(SwitchStatementSyntax node, DiagnosticBag diagnostics)
        {
            var boundSwitchExpression = BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            // TODO: any constraints on a switch expression must be enforced here. For example,
            // it must have a type (not be target-typed, lambda, null, etc)

            DefaultSwitchLabelSyntax defaultLabel = null;
            ImmutableArray<BoundMatchSection> boundMatchSections = BindMatchSections(boundSwitchExpression, node.Sections, ref defaultLabel, diagnostics);

            return new BoundMatchStatement(node, boundSwitchExpression, Locals, LocalFunctions, boundMatchSections, this.BreakLabel);
        }

        private ImmutableArray<BoundMatchSection> BindMatchSections(BoundExpression boundSwitchExpression, SyntaxList<SwitchSectionSyntax> sections, ref DefaultSwitchLabelSyntax defaultLabel, DiagnosticBag diagnostics)
        {
            // Bind match sections
            var boundMatchSectionsBuilder = ArrayBuilder<BoundMatchSection>.GetInstance();
            foreach (var sectionSyntax in sections)
            {
                boundMatchSectionsBuilder.Add(BindMatchSection(boundSwitchExpression, sectionSyntax, ref defaultLabel, diagnostics));
            }

            return boundMatchSectionsBuilder.ToImmutableAndFree();
        }

        private BoundMatchSection BindMatchSection(BoundExpression boundSwitchExpression, SwitchSectionSyntax node, ref DefaultSwitchLabelSyntax defaultLabel, DiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundMatchLabel>.GetInstance();
            var sectionBinder = (PatternVariableBinder)this.GetBinder(node); // this binder can bind pattern variables from the section.
            foreach (var labelSyntax in node.Labels)
            {
                BoundMatchLabel boundLabel = BindMatchSectionLabel(sectionBinder, boundSwitchExpression, labelSyntax, ref defaultLabel, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (var statement in node.Statements)
            {
                boundStatementsBuilder.Add(sectionBinder.BindStatement(statement, diagnostics));
            }

            return new BoundMatchSection(node, sectionBinder.Locals, boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        private static BoundMatchLabel BindMatchSectionLabel(Binder sectionBinder, BoundExpression boundSwitchExpression, SwitchLabelSyntax node, ref DefaultSwitchLabelSyntax defaultLabel, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CaseMatchLabel:
                    {
                        var matchLabelSyntax = (CaseMatchLabelSyntax)node;
                        return new BoundMatchLabel(node,
                            sectionBinder.BindPattern(matchLabelSyntax.Pattern, boundSwitchExpression, boundSwitchExpression.Type, node.HasErrors, diagnostics),
                            matchLabelSyntax.Condition != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.Condition, diagnostics) : null, node.HasErrors);
                    }

                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        var boundLabelExpression = sectionBinder.BindValue(caseLabelSyntax.Value, diagnostics, BindValueKind.RValue);
                        // TODO: check compatibility of the bound switch expression with the label
                        // TODO: check that it is a constant.
                        if ((object)boundLabelExpression.Type == null && boundLabelExpression.ConstantValue?.IsNull == true)
                        {
                            // until we've implemeneted covnersions for the case expressions, ensure each has a type.
                            boundLabelExpression = sectionBinder.CreateConversion(boundLabelExpression, sectionBinder.GetSpecialType(SpecialType.System_Object, diagnostics, node), diagnostics);
                        }
                        var pattern = new BoundConstantPattern(node, boundLabelExpression, node.HasErrors);
                        return new BoundMatchLabel(node, pattern, null, node.HasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var defaultLabelSyntax = (DefaultSwitchLabelSyntax)node;
                        var pattern = new BoundWildcardPattern(node);
                        if (defaultLabel != null)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, "default");
                        }
                        defaultLabel = defaultLabelSyntax;
                        return new BoundMatchLabel(node, pattern, null, node.HasErrors); // note that this is semantically last!
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
        }
    }
}

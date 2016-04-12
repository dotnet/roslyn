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
        private BoundPatternSwitchStatement BindPatternSwitch(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            var boundSwitchExpression = originalBinder.GetBinder(node.Expression).BindValue(node.Expression, diagnostics, BindValueKind.RValue);
            // TODO: any constraints on a switch expression must be enforced here. For example,
            // it must have a type (not be target-typed, lambda, null, etc)

            DefaultSwitchLabelSyntax defaultLabel = null;
            ImmutableArray<BoundPatternSwitchSection> boundPatternSwitchSections = BindPatternSwitchSections(boundSwitchExpression, node.Sections, originalBinder, ref defaultLabel, diagnostics);

            return new BoundPatternSwitchStatement(node, boundSwitchExpression, 
                                                   GetDeclaredLocalsForScope(node), 
                                                   GetDeclaredLocalFunctionsForScope(node), boundPatternSwitchSections, this.BreakLabel);
        }

        private ImmutableArray<BoundPatternSwitchSection> BindPatternSwitchSections(BoundExpression boundSwitchExpression, SyntaxList<SwitchSectionSyntax> sections, Binder originalBinder, ref DefaultSwitchLabelSyntax defaultLabel, DiagnosticBag diagnostics)
        {
            // Bind match sections
            var boundPatternSwitchSectionsBuilder = ArrayBuilder<BoundPatternSwitchSection>.GetInstance();
            foreach (var sectionSyntax in sections)
            {
                boundPatternSwitchSectionsBuilder.Add(BindPatternSwitchSection(boundSwitchExpression, sectionSyntax, originalBinder, ref defaultLabel, diagnostics));
            }

            return boundPatternSwitchSectionsBuilder.ToImmutableAndFree();
        }

        private BoundPatternSwitchSection BindPatternSwitchSection(BoundExpression boundSwitchExpression, SwitchSectionSyntax node, Binder originalBinder, ref DefaultSwitchLabelSyntax defaultLabel, DiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundPatternSwitchLabel>.GetInstance();
            var sectionBinder = originalBinder.GetBinder(node); // this binder can bind pattern variables from the section.
            Debug.Assert(sectionBinder != null);

            foreach (var labelSyntax in node.Labels)
            {
                BoundPatternSwitchLabel boundLabel = BindPatternSwitchSectionLabel(sectionBinder, boundSwitchExpression, labelSyntax, ref defaultLabel, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (var statement in node.Statements)
            {
                boundStatementsBuilder.Add(sectionBinder.BindStatement(statement, diagnostics));
            }

            return new BoundPatternSwitchSection(node, sectionBinder.GetDeclaredLocalsForScope(node), boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        private static BoundPatternSwitchLabel BindPatternSwitchSectionLabel(Binder sectionBinder, BoundExpression boundSwitchExpression, SwitchLabelSyntax node, ref DefaultSwitchLabelSyntax defaultLabel, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CasePatternSwitchLabel:
                    {
                        var matchLabelSyntax = (CasePatternSwitchLabelSyntax)node;
                        return new BoundPatternSwitchLabel(node,
                            sectionBinder.BindPattern(matchLabelSyntax.Pattern, boundSwitchExpression, boundSwitchExpression.Type, node.HasErrors, diagnostics),
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null, node.HasErrors);
                    }

                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        var boundLabelExpression = sectionBinder.BindPatternConstant(caseLabelSyntax.Value, diagnostics);
                        var pattern = new BoundConstantPattern(node, boundLabelExpression, node.HasErrors);
                        // PROTOTYPE(patterns): still need to check compatibility of the bound switch expression with the label
                        return new BoundPatternSwitchLabel(node, pattern, null, node.HasErrors);
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
                        return new BoundPatternSwitchLabel(node, pattern, null, node.HasErrors); // note that this is semantically last!
                    }

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private bool IsPatternSwitch(SwitchStatementSyntax node)
        {
            if (_isPatternSwitch.HasValue)
            {
                return _isPatternSwitch.GetValueOrDefault();
            }

            foreach (var section in node.Sections)
            {
                foreach (var label in section.Labels)
                {
                    if (label.Kind() == SyntaxKind.CasePatternSwitchLabel)
                    {
                        _isPatternSwitch = true;
                        return true;
                    }
                }
            }

            _isPatternSwitch = false;
            return false;
        }
    }
}

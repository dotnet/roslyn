// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // We use a subclass of SwitchBinder for the pattern-matching switch statement until we have completed
    // a totally compatible implementation of switch that also accepts pattern-matching constructs.
    internal partial class PatternSwitchBinder : SwitchBinder
    {
        // Even though these fields exist in the base class, we record them again here.
        // Our ultimate intent is to remove the type SwitchBinder and have the fully functional
        // switch binding implementation present in this class.
        private readonly SwitchStatementSyntax _switchSyntax;
        private TypeSymbol _switchGoverningType;
        private readonly GeneratedLabelSymbol _breakLabel;
        private readonly bool _isPatternSwitch;

        internal PatternSwitchBinder(Binder next, SwitchStatementSyntax switchSyntax) : base(next, switchSyntax)
        {
            _switchSyntax = switchSyntax;
            _breakLabel = new GeneratedLabelSymbol("break");
            // PROTOTYPE(typeswitch): until we generate compatible code using the new binder, we only
            // bind using the new binder if either the feature flag is set, or the syntax requires
            // we use the new binder.
            _isPatternSwitch = ((CSharpParseOptions)switchSyntax.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeaturePatternMatching);
        }

        internal override BoundStatement BindSwitchExpressionAndSections(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            Debug.Assert(_switchSyntax.Equals(node));

            // Bind switch expression and set the switch governing type.
            var localDiagnostics = DiagnosticBag.GetInstance();
            var boundSwitchExpression = BindSwitchExpressionAndGoverningType(node.Expression, originalBinder, localDiagnostics);
            diagnostics.AddRangeAndFree(localDiagnostics);

            BoundPatternSwitchLabel defaultLabel;
            ImmutableArray<BoundPatternSwitchSection> switchSections = BindPatternSwitchSections(boundSwitchExpression, node.Sections, originalBinder, out defaultLabel, diagnostics);
            var locals = GetDeclaredLocalsForScope(node);
            var functions = GetDeclaredLocalFunctionsForScope(node);
            return new BoundPatternSwitchStatement(
                node, boundSwitchExpression,
                locals, functions, switchSections, defaultLabel, this.BreakLabel, this);
        }

        private SourceLabelSymbol GetDefaultLabel()
        {
            // SwitchLabelsMap: Dictionary for the switch case/default labels.
            // Default label(s) are indexed on a special DefaultKey object.

            return FindMatchingSwitchLabel(s_defaultKey);
        }

        private static readonly object s_nullKey = new object();
        private static object KeyForConstant(ConstantValue constantValue)
        {
            Debug.Assert(constantValue != (object)null);
            return constantValue.IsNull ? s_nullKey : constantValue.Value;
        }

        private SourceLabelSymbol FindMatchingSwitchCaseLabel(ConstantValue constantValue, CSharpSyntaxNode labelSyntax)
        {
            // SwitchLabelsMap: Dictionary for the switch case/default labels.
            // Case labels with a non-null constant value are indexed on their ConstantValue.
            // Invalid case labels (with null constant value) are indexed on the label syntax.

            object key;
            if (constantValue != (object)null)
            {
                key = KeyForConstant(constantValue);
            }
            else
            {
                key = labelSyntax;
            }

            return FindMatchingSwitchLabel(key);
        }

        private SourceLabelSymbol FindMatchingSwitchLabel(object key)
        {
            Debug.Assert(key != null);

            var labelsMap = LabelsByValue;
            if (labelsMap != null)
            {
                SourceLabelSymbol label;
                if (labelsMap.TryGetValue(key, out label))
                {
                    Debug.Assert(label != (object)null);
                    return label;
                }
            }

            return null;
        }

        // Dictionary for the switch case/default labels.
        // Case labels with a non-null constant value are indexed on their ConstantValue.
        // Default label(s) are indexed on a special DefaultKey object.
        // Invalid case labels with null constant value are indexed on the labelName.
        private Dictionary<object, SourceLabelSymbol> _lazySwitchLabelsMap;
        private static readonly object s_defaultKey = new object();

        private Dictionary<object, SourceLabelSymbol> LabelsByValue
        {
            get
            {
                if (_lazySwitchLabelsMap == null && this.Labels.Length > 0)
                {
                    _lazySwitchLabelsMap = BuildLabelsByValue(this.Labels);
                }

                return _lazySwitchLabelsMap;
            }
        }

        private static Dictionary<object, SourceLabelSymbol> BuildLabelsByValue(ImmutableArray<LabelSymbol> labels)
        {
            Debug.Assert(labels.Length > 0);

            var map = new Dictionary<object, SourceLabelSymbol>(labels.Length, new SwitchConstantValueHelper.SwitchLabelsComparer());
            foreach (SourceLabelSymbol label in labels)
            {
                SyntaxKind labelKind = label.IdentifierNodeOrToken.Kind();

                if (labelKind == SyntaxKind.CaseSwitchLabel ||
                    labelKind == SyntaxKind.DefaultSwitchLabel)
                {
                    object key;
                    var constantValue = label.SwitchCaseLabelConstant;
                    if (constantValue != (object)null)
                    {
                        // Case labels with a non-null constant value are indexed on their ConstantValue.
                        key = KeyForConstant(constantValue);
                    }
                    else if (labelKind == SyntaxKind.DefaultSwitchLabel)
                    {
                        // Default label(s) are indexed on a special DefaultKey object.
                        key = s_defaultKey;
                    }
                    else
                    {
                        // Invalid case labels with null constant value are indexed on the labelName.
                        key = label.Name;
                    }

                    if (!map.ContainsKey(key))
                    {
                        map.Add(key, label);
                    }
                    else
                    {
                        // If there is a duplicate label, ignore it. It will be reported when binding the switch label.
                    }
                }
            }

            return map;
        }

        private ImmutableArray<BoundPatternSwitchSection> BindPatternSwitchSections(BoundExpression boundSwitchExpression, SyntaxList<SwitchSectionSyntax> sections, Binder originalBinder, out BoundPatternSwitchLabel defaultLabel, DiagnosticBag diagnostics)
        {
            defaultLabel = null;

            // Bind match sections
            var boundPatternSwitchSectionsBuilder = ArrayBuilder<BoundPatternSwitchSection>.GetInstance();
            foreach (var sectionSyntax in sections)
            {
                boundPatternSwitchSectionsBuilder.Add(BindPatternSwitchSection(boundSwitchExpression, sectionSyntax, originalBinder, ref defaultLabel, diagnostics));
            }

            return boundPatternSwitchSectionsBuilder.ToImmutableAndFree();
        }

        private BoundPatternSwitchSection BindPatternSwitchSection(
            BoundExpression boundSwitchExpression,
            SwitchSectionSyntax node,
            Binder originalBinder,
            ref BoundPatternSwitchLabel defaultLabel,
            DiagnosticBag diagnostics)
        {
            // Bind match section labels
            var boundLabelsBuilder = ArrayBuilder<BoundPatternSwitchLabel>.GetInstance();
            var sectionBinder = originalBinder.GetBinder(node); // this binder can bind pattern variables from the section.
            Debug.Assert(sectionBinder != null);
            var labelsByNode = LabelsByNode;

            foreach (var labelSyntax in node.Labels)
            {
                LabelSymbol label = labelsByNode[labelSyntax];
                BoundPatternSwitchLabel boundLabel = BindPatternSwitchSectionLabel(sectionBinder, boundSwitchExpression, labelSyntax, label, ref defaultLabel, diagnostics);
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

        private BoundPatternSwitchLabel BindPatternSwitchSectionLabel(
            Binder sectionBinder, BoundExpression boundSwitchExpression, SwitchLabelSyntax node, LabelSymbol label, ref BoundPatternSwitchLabel defaultLabel, DiagnosticBag diagnostics)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CaseSwitchLabel:
                    {
                        var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                        bool wasExpression;
                        var pattern = sectionBinder.BindConstantPattern(
                            node, boundSwitchExpression, boundSwitchExpression.Type, caseLabelSyntax.Value, node.HasErrors, diagnostics, out wasExpression, wasSwitchCase: true);
                        bool hasErrors = pattern.HasErrors;
                        var constantValue = pattern.ConstantValue;
                        if (!hasErrors && constantValue != (object)null && this.FindMatchingSwitchCaseLabel(constantValue, caseLabelSyntax) != label)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, pattern.ConstantValue.GetValueToDisplay() ?? label.Name);
                            hasErrors = true;
                        }
                        return new BoundPatternSwitchLabel(node, label, pattern, null, hasErrors);
                    }

                case SyntaxKind.DefaultSwitchLabel:
                    {
                        var defaultLabelSyntax = (DefaultSwitchLabelSyntax)node;
                        var pattern = new BoundWildcardPattern(node);
                        bool hasErrors = pattern.HasErrors;
                        if (defaultLabel != null)
                        {
                            diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, "default");
                            hasErrors = true;
                        }

                        // Note that this is semantically last! The caller will place it in the decision tree
                        // in the final position.
                        defaultLabel = new BoundPatternSwitchLabel(node, label, pattern, null, hasErrors);
                        return defaultLabel;
                    }

                case SyntaxKind.CasePatternSwitchLabel:
                    {
                        var matchLabelSyntax = (CasePatternSwitchLabelSyntax)node;
                        var pattern = sectionBinder.BindPattern(
                            matchLabelSyntax.Pattern, boundSwitchExpression, boundSwitchExpression.Type, node.HasErrors, diagnostics, wasSwitchCase: true);
                        return new BoundPatternSwitchLabel(node, label, pattern,
                            matchLabelSyntax.WhenClause != null ? sectionBinder.BindBooleanExpression(matchLabelSyntax.WhenClause.Condition, diagnostics) : null, node.HasErrors);
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }

        private Dictionary<SyntaxNode, LabelSymbol> _labelsByNode;
        private Dictionary<SyntaxNode, LabelSymbol> LabelsByNode
        {
            get
            {
                if (_labelsByNode == null)
                {
                    var result = new Dictionary<SyntaxNode, LabelSymbol>();
                    foreach (var label in Labels)
                    {
                        var node = ((SourceLabelSymbol)label).IdentifierNodeOrToken.AsNode();
                        if (node != null)
                        {
                            result.Add(node, label);
                        }
                    }
                    _labelsByNode = result;
                }

                return _labelsByNode;
            }
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();

            foreach (var section in _switchSyntax.Sections)
            {
                builder.AddRange(BuildLocals(section.Statements));
            }

            return builder.ToImmutableAndFree();
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            var builder = ArrayBuilder<LocalFunctionSymbol>.GetInstance();

            foreach (var section in _switchSyntax.Sections)
            {
                builder.AddRange(BuildLocalFunctions(section.Statements));
            }

            return builder.ToImmutableAndFree();
        }

        internal override bool IsLocalFunctionsScopeBinder
        {
            get
            {
                return true;
            }
        }

        internal override GeneratedLabelSymbol BreakLabel
        {
            get
            {
                return _breakLabel;
            }
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            // We bind the switch expression and the switch case label expressions so that the constant values can be
            // part of the label, but we do not report any diagnostics here. Diagnostics will be reported during binding.

            ArrayBuilder<LabelSymbol> labels = ArrayBuilder<LabelSymbol>.GetInstance();
            DiagnosticBag tempDiagnosticBag = DiagnosticBag.GetInstance();
            TypeSymbol switchGoverningType = BindSwitchExpression(_switchSyntax.Expression, this, tempDiagnosticBag).Type;
            foreach (var section in _switchSyntax.Sections)
            {
                // add switch case/default labels
                BuildSwitchLabels(switchGoverningType, section.Labels, GetBinder(section), labels, tempDiagnosticBag);

                // add regular labels from the statements in the switch section
                BuildLabels(section.Statements, ref labels);
            }

            tempDiagnosticBag.Free();
            return labels.ToImmutableAndFree();
        }

        private void BuildSwitchLabels(TypeSymbol switchGoverningType, SyntaxList<SwitchLabelSyntax> labelsSyntax, Binder sectionBinder, ArrayBuilder<LabelSymbol> labels, DiagnosticBag tempDiagnosticBag)
        {
            // add switch case/default labels
            foreach (var labelSyntax in labelsSyntax)
            {
                ConstantValue boundLabelConstantOpt = null;
                switch (labelSyntax.Kind())
                {
                    case SyntaxKind.CaseSwitchLabel:
                        // compute the constant value to place in the label symbol
                        var caseLabel = (CaseSwitchLabelSyntax)labelSyntax;
                        Debug.Assert(caseLabel.Value != null);
                        var boundLabelExpression = sectionBinder.BindValue(caseLabel.Value, tempDiagnosticBag, BindValueKind.RValue);
                        boundLabelExpression = ConvertCaseExpression(switchGoverningType, labelSyntax, boundLabelExpression, sectionBinder, ref boundLabelConstantOpt, tempDiagnosticBag);
                        break;

                    default:
                        // No constant value
                        break;
                }

                // Create the label symbol
                labels.Add(new SourceLabelSymbol((MethodSymbol)this.ContainingMemberOrLambda, labelSyntax, boundLabelConstantOpt));
            }
        }

        private BoundExpression ConvertCaseExpression(TypeSymbol switchGoverningType, CSharpSyntaxNode node, BoundExpression caseExpression, Binder sectionBinder, ref ConstantValue constantValueOpt, DiagnosticBag diagnostics, bool isGotoCaseExpr = false)
        {
            if (isGotoCaseExpr)
            {
                // SPEC VIOLATION for Dev10 COMPATIBILITY:

                // Dev10 compiler violates the SPEC comment below:
                //      "if the constant-expression is not implicitly convertible (§6.1) to 
                //      the governing type of the nearest enclosing switch statement, 
                //      a compile-time error occurs"

                // If there is no implicit conversion from gotoCaseExpression to switchGoverningType,
                // but there exists an explicit conversion, Dev10 compiler generates a warning "WRN_GotoCaseShouldConvert"
                // instead of an error. See test "CS0469_NoImplicitConversionWarning".

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                Conversion conversion = Conversions.ClassifyConversionFromExpression(caseExpression, switchGoverningType, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                if (!conversion.IsValid)
                {
                    GenerateImplicitConversionError(diagnostics, node, conversion, caseExpression, switchGoverningType);
                }
                else if (!conversion.IsImplicit)
                {
                    diagnostics.Add(ErrorCode.WRN_GotoCaseShouldConvert, node.Location, switchGoverningType);
                }

                caseExpression = CreateConversion(caseExpression, conversion, switchGoverningType, diagnostics);
            }

            return ConvertPatternExpression(switchGoverningType, node, caseExpression, ref constantValueOpt, diagnostics);
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return true;
            }
        }

        // Bind the switch expression and set the switch governing type
        private BoundExpression BindSwitchExpressionAndGoverningType(ExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            var boundSwitchExpression = BindSwitchExpression(node, originalBinder, diagnostics);
            Interlocked.CompareExchange(ref _switchGoverningType, boundSwitchExpression.Type, null);
            return boundSwitchExpression;
        }

        // Bind the switch expression
        private BoundExpression BindSwitchExpression(ExpressionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // We are at present inside the switch binder, but the switch expression is not
            // bound in the context of the switch binder; it's bound in the context of the
            // enclosing binder. For example: 
            // 
            // class C { 
            //   int x; 
            //   void M() {
            //     switch(x) {
            //       case 1:
            //         int x;
            //
            // The "x" in "switch(x)" refers to this.x, not the local x that is in scope inside the switch block.

            Debug.Assert(node == _switchSyntax.Expression);
            var binder = originalBinder.GetBinder(node);
            Debug.Assert(binder != null);

            var switchExpression = binder.BindValue(node, diagnostics, BindValueKind.RValue);

            var switchGoverningType = switchExpression.Type;

            if ((object)switchGoverningType != null && !switchGoverningType.IsErrorType())
            {
                // SPEC:    The governing type of a switch statement is established by the switch expression.
                // SPEC:    1) If the type of the switch expression is sbyte, byte, short, ushort, int, uint,
                // SPEC:       long, ulong, bool, char, string, or an enum-type, or if it is the nullable type
                // SPEC:       corresponding to one of these types, then that is the governing type of the switch statement. 
                // SPEC:    2) Otherwise if exactly one user-defined implicit conversion (§6.4) exists from the
                // SPEC:       type of the switch expression to one of the following possible governing types:
                // SPEC:       sbyte, byte, short, ushort, int, uint, long, ulong, char, string, or, a nullable type
                // SPEC:       corresponding to one of those types, then the result is the switch governing type
                // SPEC:    3) Otherwise (in C# 7 and later) the switch governing type is the type of the
                // SPEC:       switch expression.

                if (switchGoverningType.IsValidV6SwitchGoverningType())
                {
                    // Condition (1) satisfied

                    // Note: dev11 actually checks the stripped type, but nullable was introduced at the same
                    // time, so it doesn't really matter.
                    if (switchGoverningType.SpecialType == SpecialType.System_Boolean && !_isPatternSwitch)
                    {
                        // GetLocation() so that it also works in speculative contexts.
                        CheckFeatureAvailability(node.GetLocation(), MessageID.IDS_FeatureSwitchOnBool, diagnostics);
                    }

                    return switchExpression;
                }
                else
                {
                    TypeSymbol resultantGoverningType;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    Conversion conversion = binder.Conversions.ClassifyImplicitUserDefinedConversionForV6SwitchGoverningType(switchGoverningType, out resultantGoverningType, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);
                    if (conversion.IsValid)
                    {
                        // Condition (2) satisfied
                        Debug.Assert(conversion.Kind == ConversionKind.ImplicitUserDefined);
                        Debug.Assert(conversion.Method.IsUserDefinedConversion());
                        Debug.Assert(conversion.UserDefinedToConversion.IsIdentity);
                        Debug.Assert(resultantGoverningType.IsValidV6SwitchGoverningType(isTargetTypeOfUserDefinedOp: true));
                        return binder.CreateConversion(node, switchExpression, conversion, false, resultantGoverningType, diagnostics);
                    }
                    else if (_isPatternSwitch && switchGoverningType.SpecialType != SpecialType.System_Void)
                    {
                        // Otherwsie (3) satisfied
                        return switchExpression;
                    }
                    else
                    {
                        // We need to create an error type here as certain diagnostics generated during binding the switch case label expression and
                        // goto case expression should be generated only if the switch expression type is a valid switch governing type.
                        switchGoverningType = CreateErrorType(switchGoverningType.Name);
                    }
                }
            }

            if (!switchExpression.HasAnyErrors)
            {
                if (_isPatternSwitch)
                {
                    diagnostics.Add(ErrorCode.ERR_PatternValueExpected, node.Location, switchExpression.Display);
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, node.Location);
                }
            }

            return new BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create<BoundNode>(switchExpression), switchGoverningType ?? CreateErrorType());
        }

        internal override BoundStatement BindGotoCaseOrDefault(GotoStatementSyntax node, Binder gotoBinder, DiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.GotoCaseStatement || node.Kind() == SyntaxKind.GotoDefaultStatement);
            BoundExpression gotoCaseExpressionOpt = null;

            // Prevent cascading diagnostics
            if (!node.HasErrors)
            {
                ConstantValue gotoCaseExpressionConstant = null;
                TypeSymbol switchGoverningType = GetSwitchGoverningType(diagnostics);
                bool hasErrors = false;
                SourceLabelSymbol matchedLabelSymbol;

                // SPEC:    If the goto case statement is not enclosed by a switch statement, if the constant-expression
                // SPEC:    is not implicitly convertible (§6.1) to the governing type of the nearest enclosing switch statement,
                // SPEC:    or if the nearest enclosing switch statement does not contain a case label with the given constant value,
                // SPEC:    a compile-time error occurs.

                // SPEC:    If the goto default statement is not enclosed by a switch statement, or if the nearest enclosing
                // SPEC:    switch statement does not contain a default label, a compile-time error occurs.

                if (node.Expression != null)
                {
                    Debug.Assert(node.Kind() == SyntaxKind.GotoCaseStatement);

                    // Bind the goto case expression
                    gotoCaseExpressionOpt = gotoBinder.BindValue(node.Expression, diagnostics, BindValueKind.RValue);

                    gotoCaseExpressionOpt = ConvertCaseExpression(switchGoverningType, node, gotoCaseExpressionOpt, gotoBinder,
                        ref gotoCaseExpressionConstant, diagnostics, isGotoCaseExpr: true);

                    // Check for bind errors
                    hasErrors = hasErrors || gotoCaseExpressionOpt.HasAnyErrors;

                    if (!hasErrors && gotoCaseExpressionConstant == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_ConstantExpected, node.Location);
                        hasErrors = true;
                    }

                    // LabelSymbols for all the switch case labels are created by BuildLabels().
                    // Fetch the matching switch case label symbols
                    matchedLabelSymbol = FindMatchingSwitchCaseLabel(gotoCaseExpressionConstant, node);
                }
                else
                {
                    Debug.Assert(node.Kind() == SyntaxKind.GotoDefaultStatement);
                    matchedLabelSymbol = GetDefaultLabel();
                }

                if (matchedLabelSymbol == (object)null)
                {
                    if (!hasErrors)
                    {
                        // No matching case label/default label found
                        var labelName = SyntaxFacts.GetText(node.CaseOrDefaultKeyword.Kind());
                        if (node.Kind() == SyntaxKind.GotoCaseStatement)
                        {
                            labelName += " " + gotoCaseExpressionConstant.Value?.ToString();
                        }
                        labelName += ":";

                        diagnostics.Add(ErrorCode.ERR_LabelNotFound, node.Location, labelName);
                        hasErrors = true;
                    }
                }
                else
                {
                    return new BoundGotoStatement(node, matchedLabelSymbol, gotoCaseExpressionOpt, null, hasErrors);
                }
            }

            return new BoundBadStatement(
                syntax: node,
                childBoundNodes: gotoCaseExpressionOpt != null ? ImmutableArray.Create<BoundNode>(gotoCaseExpressionOpt) : ImmutableArray<BoundNode>.Empty,
                hasErrors: true);
        }

        private TypeSymbol GetSwitchGoverningType(DiagnosticBag diagnostics)
        {
            if ((object)_switchGoverningType == null)
            {
                // Can reach here only when we are called from the Binding API
                // Let us bind the switch expression and switch governing type
                var discarded = BindSwitchExpressionAndGoverningType(_switchSyntax.Expression, this, diagnostics);
                Debug.Assert((object)_switchGoverningType != null);
            }
            return _switchGoverningType;
        }

    }
}

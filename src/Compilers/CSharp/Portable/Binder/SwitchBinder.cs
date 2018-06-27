// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SwitchBinder : LocalScopeBinder
    {
        protected readonly SwitchStatementSyntax SwitchSyntax;

        private readonly GeneratedLabelSymbol _breakLabel;
        private BoundExpression _switchGoverningExpression;
        private DiagnosticBag _switchGoverningDiagnostics;

        protected SwitchBinder(Binder next, SwitchStatementSyntax switchSyntax)
            : base(next)
        {
            SwitchSyntax = switchSyntax;
            _breakLabel = new GeneratedLabelSymbol("break");
        }

        internal static SwitchBinder Create(Binder next, SwitchStatementSyntax switchSyntax)
        {
            var parseOptions = switchSyntax?.SyntaxTree?.Options as CSharpParseOptions;
            return
                // In C# 6 and earlier, we use the old binder. In C# 7 and later, we use the new binder which
                // is capable of binding both the old and new syntax. However, the new binder does not yet
                // lead to a translation that fully supports edit-and-continue, so it delegates to the C# 6
                // binder when it can. The "testV7SwitchBinder" feature flag forces the use of the C# 7 switch binder
                // for all operations; we use it to enhance test coverage.
                (parseOptions?.IsFeatureEnabled(MessageID.IDS_FeaturePatternMatching) != false ||
                 parseOptions?.Features.ContainsKey("testV7SwitchBinder") != false ||
                 switchSyntax.HasErrors && HasPatternSwitchSyntax(switchSyntax))
                ? new PatternSwitchBinder(next, switchSyntax)
                : new SwitchBinder(next, switchSyntax);
        }

        internal static bool HasPatternSwitchSyntax(SwitchStatementSyntax switchSyntax)
        {
            foreach (var section in switchSyntax.Sections)
            {
                if (section.Labels.Any(SyntaxKind.CasePatternSwitchLabel))
                {
                    return true;
                }
            }

            return false;
        }

        protected bool PatternsEnabled =>
            ((CSharpParseOptions)SwitchSyntax.SyntaxTree.Options)?.IsFeatureEnabled(MessageID.IDS_FeaturePatternMatching) != false;

        protected BoundExpression SwitchGoverningExpression
        {
            get
            {
                if (_switchGoverningExpression == null)
                {
                    EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                }

                Debug.Assert(_switchGoverningExpression != null);
                return _switchGoverningExpression;
            }
        }

        protected TypeSymbol SwitchGoverningType => SwitchGoverningExpression.Type;

        protected DiagnosticBag SwitchGoverningDiagnostics
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                return _switchGoverningDiagnostics;
            }
        }

        private void EnsureSwitchGoverningExpressionAndDiagnosticsBound()
        {
            var switchGoverningDiagnostics = new DiagnosticBag();
            var boundSwitchExpression = BindSwitchExpression(switchGoverningDiagnostics);
            _switchGoverningDiagnostics = switchGoverningDiagnostics;
            Interlocked.CompareExchange(ref _switchGoverningExpression, boundSwitchExpression, null);
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
                if (labelKind == SyntaxKind.IdentifierToken)
                {
                    continue;
                }

                object key;
                var constantValue = label.SwitchCaseLabelConstant;
                if ((object)constantValue != null && !constantValue.IsBad)
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
                    key = label.IdentifierNodeOrToken.AsNode();
                }

                // If there is a duplicate label, ignore it. It will be reported when binding the switch label.
                if (!map.ContainsKey(key))
                {
                    map.Add(key, label);
                }
            }

            return map;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();

            foreach (var section in SwitchSyntax.Sections)
            {
                builder.AddRange(BuildLocals(section.Statements, GetBinder(section)));
            }

            return builder.ToImmutableAndFree();
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            var builder = ArrayBuilder<LocalFunctionSymbol>.GetInstance();

            foreach (var section in SwitchSyntax.Sections)
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
            foreach (var section in SwitchSyntax.Sections)
            {
                // add switch case/default labels
                BuildSwitchLabels(section.Labels, GetBinder(section), labels, tempDiagnosticBag);

                // add regular labels from the statements in the switch section
                BuildLabels(section.Statements, ref labels);
            }

            tempDiagnosticBag.Free();
            return labels.ToImmutableAndFree();
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return true;
            }
        }

        private void BuildSwitchLabels(SyntaxList<SwitchLabelSyntax> labelsSyntax, Binder sectionBinder, ArrayBuilder<LabelSymbol> labels, DiagnosticBag tempDiagnosticBag)
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
                        boundLabelExpression = ConvertCaseExpression(labelSyntax, boundLabelExpression, sectionBinder, ref boundLabelConstantOpt, tempDiagnosticBag);
                        break;

                    case SyntaxKind.CasePatternSwitchLabel:
                        // bind the pattern, to cause its pattern variables to be inferred if necessary
                        var matchLabel = (CasePatternSwitchLabelSyntax)labelSyntax;
                        var pattern = sectionBinder.BindPattern(
                            SwitchGoverningExpression, matchLabel.Pattern, SwitchGoverningType, labelSyntax.HasErrors, tempDiagnosticBag);
                        break;

                    default:
                        // No constant value
                        break;
                }

                // Create the label symbol
                labels.Add(new SourceLabelSymbol((MethodSymbol)this.ContainingMemberOrLambda, labelSyntax, boundLabelConstantOpt));
            }
        }

        protected BoundExpression ConvertCaseExpression(CSharpSyntaxNode node, BoundExpression caseExpression, Binder sectionBinder, ref ConstantValue constantValueOpt, DiagnosticBag diagnostics, bool isGotoCaseExpr = false)
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
                Conversion conversion = Conversions.ClassifyConversionFromExpression(caseExpression, SwitchGoverningType, ref useSiteDiagnostics);
                diagnostics.Add(node, useSiteDiagnostics);
                if (!conversion.IsValid)
                {
                    GenerateImplicitConversionError(diagnostics, node, conversion, caseExpression, SwitchGoverningType);
                }
                else if (!conversion.IsImplicit)
                {
                    diagnostics.Add(ErrorCode.WRN_GotoCaseShouldConvert, node.Location, SwitchGoverningType);
                }

                caseExpression = CreateConversion(caseExpression, conversion, SwitchGoverningType, diagnostics);
            }

            return ConvertPatternExpression(SwitchGoverningType, node, caseExpression, ref constantValueOpt, diagnostics);
        }

        private static readonly object s_nullKey = new object();
        protected static object KeyForConstant(ConstantValue constantValue)
        {
            Debug.Assert((object)constantValue != null);
            return constantValue.IsNull ? s_nullKey : constantValue.Value;
        }

        protected SourceLabelSymbol FindMatchingSwitchCaseLabel(ConstantValue constantValue, CSharpSyntaxNode labelSyntax)
        {
            // SwitchLabelsMap: Dictionary for the switch case/default labels.
            // Case labels with a non-null constant value are indexed on their ConstantValue.
            // Invalid case labels (with null constant value) are indexed on the label syntax.

            object key;
            if ((object)constantValue != null && !constantValue.IsBad)
            {
                key = KeyForConstant(constantValue);
            }
            else
            {
                key = labelSyntax;
            }

            return FindMatchingSwitchLabel(key);
        }

        private SourceLabelSymbol GetDefaultLabel()
        {
            // SwitchLabelsMap: Dictionary for the switch case/default labels.
            // Default label(s) are indexed on a special DefaultKey object.

            return FindMatchingSwitchLabel(s_defaultKey);
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
                    Debug.Assert((object)label != null);
                    return label;
                }
            }

            return null;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (SwitchSyntax == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (SwitchSyntax == scopeDesignator)
            {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable;
        }
        
        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return SwitchSyntax;
            }
        }

        # region "Switch statement binding methods"

        internal override BoundStatement BindSwitchExpressionAndSections(SwitchStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            Debug.Assert(SwitchSyntax.Equals(node));

            // Bind switch expression and set the switch governing type.
            var boundSwitchExpression = this.SwitchGoverningExpression;
            diagnostics.AddRange(this.SwitchGoverningDiagnostics);

            // Switch expression might be a constant expression.
            // For this scenario we can determine the target label of the switch statement
            // at compile time.            
            LabelSymbol constantTargetOpt = null;
            var constantValue = boundSwitchExpression.ConstantValue;
            if (constantValue != null)
            {
                constantTargetOpt = BindConstantJumpTarget(constantValue, node);
            }
            else if (!node.Sections.Any())
            {
                // empty switch block, set the break label as target
                constantTargetOpt = this.BreakLabel;
            }

            // Bind switch section
            ImmutableArray<BoundSwitchSection> boundSwitchSections = BindSwitchSections(node.Sections, originalBinder, diagnostics);

            return new BoundSwitchStatement(node, null, boundSwitchExpression, constantTargetOpt, 
                                            GetDeclaredLocalsForScope(node), 
                                            GetDeclaredLocalFunctionsForScope(node), boundSwitchSections, this.BreakLabel, null);
        }

        internal override void BindPatternSwitchLabelForInference(CasePatternSwitchLabelSyntax node, DiagnosticBag diagnostics)
        {
            // A pattern should be handled by a pattern switch binder.
            throw ExceptionUtilities.Unreachable;
        }

        // Bind the switch expression
        private BoundExpression BindSwitchExpression(DiagnosticBag diagnostics)
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

            Debug.Assert(ScopeDesignator == SwitchSyntax);
            ExpressionSyntax node = SwitchSyntax.Expression;
            var binder = this.GetBinder(node);
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
                    if (switchGoverningType.SpecialType == SpecialType.System_Boolean)
                    {
                        CheckFeatureAvailability(node, MessageID.IDS_FeatureSwitchOnBool, diagnostics);
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
                    else if (switchGoverningType.SpecialType != SpecialType.System_Void)
                    {
                        // Otherwise (3) satisfied
                        if (!PatternsEnabled)
                        {
                            diagnostics.Add(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, node.Location);
                        }

                        return switchExpression;
                    }
                    else
                    {
                        switchGoverningType = CreateErrorType(switchGoverningType.Name);
                    }
                }
            }

            if (!switchExpression.HasAnyErrors)
            {
                Debug.Assert((object)switchExpression.Type == null || switchExpression.Type.SpecialType == SpecialType.System_Void);
                diagnostics.Add(ErrorCode.ERR_SwitchExpressionValueExpected, node.Location, switchExpression.Display);
            }

            return new BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create(switchExpression), switchGoverningType ?? CreateErrorType());
        }

        private LabelSymbol BindConstantJumpTarget(ConstantValue constantValue, CSharpSyntaxNode syntax)
        {
            LabelSymbol boundLabel = null;

            // If the switch statement has a matching switch case label with the same constant
            // value, that label is set as the target.
            SourceLabelSymbol labelSymbol = FindMatchingSwitchCaseLabel(constantValue, syntax);

            if ((object)labelSymbol != null)
            {
                boundLabel = labelSymbol;
            }
            else
            {
                // If not, we check if there is a default label in the
                // switch statement and set that as the target label.
                labelSymbol = GetDefaultLabel();

                if ((object)labelSymbol != null)
                {
                    boundLabel = labelSymbol;
                }
                else
                {
                    // Otherwise the switch statement's BreakLabel is set as the target.
                    boundLabel = BreakLabel;
                }
            }

            return boundLabel;
        }

        private ImmutableArray<BoundSwitchSection> BindSwitchSections(SyntaxList<SwitchSectionSyntax> switchSections, Binder originalBinder, DiagnosticBag diagnostics)
        {
            // Bind switch sections
            var boundSwitchSectionsBuilder = ArrayBuilder<BoundSwitchSection>.GetInstance();
            foreach (var sectionSyntax in switchSections)
            {
                boundSwitchSectionsBuilder.Add(BindSwitchSection(sectionSyntax, originalBinder, diagnostics));
            }

            return boundSwitchSectionsBuilder.ToImmutableAndFree();
        }

        private BoundSwitchSection BindSwitchSection(SwitchSectionSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            var sectionBinder = originalBinder.GetBinder(node);
            var locals = sectionBinder.GetDeclaredLocalsForScope(node);

            // Bind switch section labels
            var boundLabelsBuilder = ArrayBuilder<BoundSwitchLabel>.GetInstance();
            foreach (var labelSyntax in node.Labels)
            {
                LabelSymbol label = LabelsByNode[labelSyntax];
                BoundSwitchLabel boundLabel = BindSwitchSectionLabel(labelSyntax, sectionBinder, label, diagnostics);
                boundLabelsBuilder.Add(boundLabel);
            }

            // Bind switch section statements
            var boundStatementsBuilder = ArrayBuilder<BoundStatement>.GetInstance();
            foreach (var statement in node.Statements)
            {
                boundStatementsBuilder.Add(sectionBinder.BindStatement(statement, diagnostics));
            }

            return new BoundSwitchSection(node, locals, boundLabelsBuilder.ToImmutableAndFree(), boundStatementsBuilder.ToImmutableAndFree());
        }

        private Dictionary<SyntaxNode, LabelSymbol> _labelsByNode;
        protected Dictionary<SyntaxNode, LabelSymbol> LabelsByNode
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

        private BoundSwitchLabel BindSwitchSectionLabel(SwitchLabelSyntax node, Binder sectionBinder, LabelSymbol label, DiagnosticBag diagnostics)
        {
            var switchGoverningType = SwitchGoverningType;
            BoundExpression boundLabelExpressionOpt = null;
            ConstantValue labelExpressionConstant = null;

            // Prevent cascading diagnostics
            bool hasErrors = node.HasErrors;

            switch (node.Kind())
            {
                case SyntaxKind.CaseSwitchLabel:
                    var caseLabelSyntax = (CaseSwitchLabelSyntax)node;
                    // Bind the label case expression
                    boundLabelExpressionOpt = sectionBinder.BindValue(caseLabelSyntax.Value, diagnostics, BindValueKind.RValue);
                    boundLabelExpressionOpt = ConvertCaseExpression(caseLabelSyntax, boundLabelExpressionOpt, sectionBinder, ref labelExpressionConstant, diagnostics);

                    // Check for bind errors
                    hasErrors = hasErrors || boundLabelExpressionOpt.HasAnyErrors;

                    // SPEC:    The constant expression of each case label must denote a value that
                    // SPEC:    is implicitly convertible (§6.1) to the governing type of the switch statement.
                    if (!hasErrors && labelExpressionConstant == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_ConstantExpected, caseLabelSyntax.Value.Location);
                        hasErrors = true;
                    }

                    if (!hasErrors && (object)labelExpressionConstant != null && FindMatchingSwitchCaseLabel(labelExpressionConstant, caseLabelSyntax) != label)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, labelExpressionConstant?.GetValueToDisplay() ?? label.Name);
                        hasErrors = true;
                    }

                    SyntaxNode innerValueSyntax = caseLabelSyntax.Value.SkipParens();
                    if (innerValueSyntax.Kind() == SyntaxKind.DefaultLiteralExpression)
                    {
                        diagnostics.Add(ErrorCode.ERR_DefaultInSwitch, innerValueSyntax.Location);
                    }

                    // LabelSymbols for all the switch case labels are created by BuildLabels().
                    // Fetch the matching switch case label symbols
                    break;
                case SyntaxKind.CasePatternSwitchLabel:
                    if (!node.HasErrors)
                    {
                        // This should not occur, because we would be using a pattern switch binder
                        throw ExceptionUtilities.UnexpectedValue(node.Kind());
                    }
                    break;
                case SyntaxKind.DefaultSwitchLabel:
                    if (GetDefaultLabel() != label)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateCaseLabel, node.Location, label.Name);
                        hasErrors = true;
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }

            return new BoundSwitchLabel(
                syntax: node,
                label: label,
                expressionOpt: boundLabelExpressionOpt,
                constantValueOpt: labelExpressionConstant,
                hasErrors: hasErrors);
        }

        internal BoundStatement BindGotoCaseOrDefault(GotoStatementSyntax node, Binder gotoBinder, DiagnosticBag diagnostics)
        {
            Debug.Assert(node.Kind() == SyntaxKind.GotoCaseStatement || node.Kind() == SyntaxKind.GotoDefaultStatement);
            BoundExpression gotoCaseExpressionOpt = null;

            // Prevent cascading diagnostics
            if (!node.HasErrors)
            {
                ConstantValue gotoCaseExpressionConstant = null;
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

                    gotoCaseExpressionOpt = ConvertCaseExpression(node, gotoCaseExpressionOpt, gotoBinder,
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

                if ((object)matchedLabelSymbol == null)
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

        #endregion
    }
}

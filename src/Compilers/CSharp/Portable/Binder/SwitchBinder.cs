// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SwitchBinder : LocalScopeBinder
    {
        protected readonly SwitchStatementSyntax SwitchSyntax;

        private readonly GeneratedLabelSymbol _breakLabel;
        private BoundExpression _switchGoverningExpression;
        private ImmutableArray<Diagnostic> _switchGoverningDiagnostics;
        private ImmutableArray<AssemblySymbol> _switchGoverningDependencies;

        private SwitchBinder(Binder next, SwitchStatementSyntax switchSyntax)
            : base(next)
        {
            SwitchSyntax = switchSyntax;
            _breakLabel = new GeneratedLabelSymbol("break");
        }

        protected bool PatternsEnabled =>
            ((CSharpParseOptions)SwitchSyntax.SyntaxTree.Options)?.IsFeatureEnabled(MessageID.IDS_FeaturePatternMatching) != false;

        protected BoundExpression SwitchGoverningExpression
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                Debug.Assert(_switchGoverningExpression != null);
                return _switchGoverningExpression;
            }
        }

        protected TypeSymbol SwitchGoverningType => SwitchGoverningExpression.Type;

        protected ReadOnlyBindingDiagnostic<AssemblySymbol> SwitchGoverningDiagnostics
        {
            get
            {
                EnsureSwitchGoverningExpressionAndDiagnosticsBound();
                return new ReadOnlyBindingDiagnostic<AssemblySymbol>(_switchGoverningDiagnostics, _switchGoverningDependencies);
            }
        }

        private void EnsureSwitchGoverningExpressionAndDiagnosticsBound()
        {
            if (_switchGoverningExpression == null)
            {
                var switchGoverningDiagnostics = BindingDiagnosticBag.GetInstance();
                var boundSwitchExpression = BindSwitchGoverningExpression(switchGoverningDiagnostics);

                var immutableSwitchGoverningDiagnostics = switchGoverningDiagnostics.ToReadOnlyAndFree();
                ImmutableInterlocked.InterlockedInitialize(ref _switchGoverningDiagnostics, immutableSwitchGoverningDiagnostics.Diagnostics);
                ImmutableInterlocked.InterlockedInitialize(ref _switchGoverningDependencies, immutableSwitchGoverningDiagnostics.Dependencies);
                Interlocked.CompareExchange(ref _switchGoverningExpression, boundSwitchExpression, null);
            }
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

        protected override ImmutableArray<LocalSymbol> BuildLocals()
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
            foreach (var section in SwitchSyntax.Sections)
            {
                // add switch case/default labels
                BuildSwitchLabels(section.Labels, GetBinder(section), labels, BindingDiagnosticBag.Discarded);

                // add regular labels from the statements in the switch section
                BuildLabels(section.Statements, ref labels);
            }

            return labels.ToImmutableAndFree();
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return true;
            }
        }

        private void BuildSwitchLabels(SyntaxList<SwitchLabelSyntax> labelsSyntax, Binder sectionBinder, ArrayBuilder<LabelSymbol> labels, BindingDiagnosticBag tempDiagnosticBag)
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
                        var boundLabelExpression = sectionBinder.BindTypeOrRValue(caseLabel.Value, tempDiagnosticBag);
                        if (boundLabelExpression is BoundTypeExpression type)
                        {
                            // Nothing to do at this point.  The label will be bound later.
                        }
                        else
                        {
                            _ = ConvertCaseExpression(labelSyntax, boundLabelExpression, out boundLabelConstantOpt, tempDiagnosticBag);
                        }
                        break;

                    case SyntaxKind.CasePatternSwitchLabel:
                        // bind the pattern, to cause its pattern variables to be inferred if necessary
                        var matchLabel = (CasePatternSwitchLabelSyntax)labelSyntax;
                        _ = sectionBinder.BindPattern(
                            matchLabel.Pattern, SwitchGoverningType, permitDesignations: true, labelSyntax.HasErrors, tempDiagnosticBag, hasUnionMatching: out _);
                        break;

                    default:
                        // No constant value
                        break;
                }

                // Create the label symbol
                labels.Add(new SourceLabelSymbol((MethodSymbol)this.ContainingMemberOrLambda, labelSyntax, boundLabelConstantOpt));
            }
        }

        protected BoundExpression ConvertCaseExpression(CSharpSyntaxNode node, BoundExpression caseExpression, out ConstantValue constantValueOpt, BindingDiagnosticBag diagnostics, bool isGotoCaseExpr = false)
        {
            bool hasErrors = false;
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

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                Conversion conversion = Conversions.ClassifyConversionFromExpression(caseExpression, SwitchGoverningType, isChecked: CheckOverflowAtRuntime, ref useSiteInfo);
                diagnostics.Add(node, useSiteInfo);
                if (!conversion.IsValid)
                {
                    GenerateImplicitConversionError(diagnostics, node, conversion, caseExpression, SwitchGoverningType);
                    hasErrors = true;
                }
                else if (!conversion.IsImplicit)
                {
                    diagnostics.Add(ErrorCode.WRN_GotoCaseShouldConvert, node.Location, SwitchGoverningType);
                    hasErrors = true;
                }

                caseExpression = CreateConversion(caseExpression, conversion, SwitchGoverningType, diagnostics);
            }

            return ConvertPatternExpression(SwitchGoverningType, node, caseExpression, out constantValueOpt, hasErrors, diagnostics, out _);
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

            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (SwitchSyntax == scopeDesignator)
            {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable();
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return SwitchSyntax;
            }
        }

        # region "Switch statement binding methods"

        // Bind the switch expression
        private BoundExpression BindSwitchGoverningExpression(BindingDiagnosticBag diagnostics)
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

            var switchGoverningExpression = binder.BindRValueWithoutTargetType(node, diagnostics);
            var switchGoverningType = switchGoverningExpression.Type;

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

                    return switchGoverningExpression;
                }
                else
                {
                    TypeSymbol resultantGoverningType;
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    Conversion conversion = binder.Conversions.ClassifyImplicitUserDefinedConversionForV6SwitchGoverningType(switchGoverningType, out resultantGoverningType, ref useSiteInfo);
                    diagnostics.Add(node, useSiteInfo);
                    if (conversion.IsValid)
                    {
                        // Condition (2) satisfied
                        Debug.Assert(conversion.Kind == ConversionKind.ImplicitUserDefined);
                        Debug.Assert(conversion.Method.IsUserDefinedConversion());
                        Debug.Assert(conversion.UserDefinedToConversion.IsIdentity);
                        Debug.Assert(resultantGoverningType.IsValidV6SwitchGoverningType(isTargetTypeOfUserDefinedOp: true));
                        return binder.CreateConversion(node, switchGoverningExpression, conversion, isCast: false, conversionGroupOpt: null, resultantGoverningType, diagnostics);
                    }
                    else if (!switchGoverningType.IsVoidType())
                    {
                        // Otherwise (3) satisfied
                        if (!PatternsEnabled)
                        {
                            diagnostics.Add(ErrorCode.ERR_V6SwitchGoverningTypeValueExpected, node.Location);
                        }

                        return switchGoverningExpression;
                    }
                    else
                    {
                        switchGoverningType = CreateErrorType(switchGoverningType.Name);
                    }
                }
            }

            if (!switchGoverningExpression.HasAnyErrors)
            {
                Debug.Assert((object)switchGoverningExpression.Type == null || switchGoverningExpression.Type.IsVoidType());
                diagnostics.Add(ErrorCode.ERR_SwitchExpressionValueExpected, node.Location, switchGoverningExpression.Display);
            }

            return new BoundBadExpression(node, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, ImmutableArray.Create(switchGoverningExpression), switchGoverningType ?? CreateErrorType());
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

        internal BoundStatement BindGotoCaseOrDefault(GotoStatementSyntax node, Binder gotoBinder, BindingDiagnosticBag diagnostics)
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

                    gotoCaseExpressionOpt = ConvertCaseExpression(node, gotoCaseExpressionOpt, out gotoCaseExpressionConstant,
                        diagnostics, isGotoCaseExpr: true);

                    // Check for bind errors
                    hasErrors = hasErrors || gotoCaseExpressionOpt.HasAnyErrors;

                    if (!hasErrors && gotoCaseExpressionConstant == null)
                    {
                        diagnostics.Add(ErrorCode.ERR_ConstantExpected, node.Location);
                        hasErrors = true;
                    }

                    ConstantValueUtils.CheckLangVersionForConstantValue(gotoCaseExpressionOpt, diagnostics);

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

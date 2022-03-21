// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public string Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration_CodeStyle
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration); }
            set { SetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value); }
        }

        public string Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess_CodeStyle
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess); }
            set { SetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value); }
        }

        public string Style_QualifyFieldAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyFieldAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyFieldAccess, value); }
        }

        public string Style_QualifyPropertyAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyPropertyAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyPropertyAccess, value); }
        }

        public string Style_QualifyMethodAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyMethodAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyMethodAccess, value); }
        }

        public string Style_QualifyEventAccess
        {
            get { return GetXmlOption(CodeStyleOptions2.QualifyEventAccess); }
            set { SetXmlOption(CodeStyleOptions2.QualifyEventAccess, value); }
        }

        public string Style_PreferThrowExpression
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferThrowExpression); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferThrowExpression, value); }
        }

        public string Style_PreferObjectInitializer
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferObjectInitializer); }
            set { SetXmlOption(CodeStyleOptions2.PreferObjectInitializer, value); }
        }

        public string Style_PreferCollectionInitializer
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferCollectionInitializer); }
            set { SetXmlOption(CodeStyleOptions2.PreferCollectionInitializer, value); }
        }

        public string Style_PreferCoalesceExpression
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferCoalesceExpression); }
            set { SetXmlOption(CodeStyleOptions2.PreferCoalesceExpression, value); }
        }

        public string Style_PreferMethodGroupConversion
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferMethodGroupConversion); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferMethodGroupConversion, value); }
        }

        public string Style_PreferNullPropagation
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferNullPropagation); }
            set { SetXmlOption(CodeStyleOptions2.PreferNullPropagation, value); }
        }

        public string Style_PreferInlinedVariableDeclaration
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferInlinedVariableDeclaration, value); }
        }

        public string Style_PreferExplicitTupleNames
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferExplicitTupleNames); }
            set { SetXmlOption(CodeStyleOptions2.PreferExplicitTupleNames, value); }
        }

        public string Style_PreferInferredTupleNames
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferInferredTupleNames); }
            set { SetXmlOption(CodeStyleOptions2.PreferInferredTupleNames, value); }
        }

        public string Style_PreferInferredAnonymousTypeMemberNames
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames); }
            set { SetXmlOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, value); }
        }

        public string Style_UseImplicitTypeWherePossible
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.VarElsewhere); }
            set { SetXmlOption(CSharpCodeStyleOptions.VarElsewhere, value); }
        }

        public string Style_UseImplicitTypeWhereApparent
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent); }
            set { SetXmlOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, value); }
        }

        public string Style_UseImplicitTypeForIntrinsicTypes
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.VarForBuiltInTypes); }
            set { SetXmlOption(CSharpCodeStyleOptions.VarForBuiltInTypes, value); }
        }

        public string Style_PreferConditionalDelegateCall
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferConditionalDelegateCall, value); }
        }

        public string Style_PreferSwitchExpression
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferSwitchExpression); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferSwitchExpression, value); }
        }

        public string Style_PreferPatternMatching
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferPatternMatching); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferPatternMatching, value); }
        }

        public string Style_PreferPatternMatchingOverAsWithNullCheck
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, value); }
        }

        public string Style_PreferPatternMatchingOverIsWithCastCheck
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, value); }
        }

        public string Style_PreferExpressionBodiedConstructors
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, value); }
        }

        public string Style_PreferExpressionBodiedMethods
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, value); }
        }

        public string Style_PreferExpressionBodiedOperators
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedOperators, value); }
        }

        public string Style_PreferExpressionBodiedProperties
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, value); }
        }

        public string Style_PreferExpressionBodiedIndexers
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, value); }
        }

        public string Style_PreferExpressionBodiedAccessors
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, value); }
        }

        public string Style_PreferExpressionBodiedLambdas
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedLambdas, value); }
        }

        public string Style_PreferExpressionBodiedLocalFunctions
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, value); }
        }

        public string Style_PreferBraces
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferBraces); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferBraces, value); }
        }

        public string Style_PreferReadonly
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferReadonly); }
            set { SetXmlOption(CodeStyleOptions2.PreferReadonly, value); }
        }

        public int Style_PreferObjectInitializer_FadeOutCode
        {
            get { return GetBooleanOption(CodeStyleOptions2.PreferObjectInitializer_FadeOutCode); }
            set { SetBooleanOption(CodeStyleOptions2.PreferObjectInitializer_FadeOutCode, value); }
        }

        public int Style_PreferCollectionInitializer_FadeOutCode
        {
            get { return GetBooleanOption(CodeStyleOptions2.PreferCollectionInitializer_FadeOutCode); }
            set { SetBooleanOption(CodeStyleOptions2.PreferCollectionInitializer_FadeOutCode, value); }
        }

        public string Style_PreferSimplifiedBooleanExpressions
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions); }
            set { SetXmlOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, value); }
        }

        public string Style_PreferAutoProperties
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferAutoProperties); }
            set { SetXmlOption(CodeStyleOptions2.PreferAutoProperties, value); }
        }

        public string Style_PreferIsNullCheckOverReferenceEqualityMethod
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod); }
            set { SetXmlOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, value); }
        }

        public string Style_PreferParameterNullChecking
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferParameterNullChecking); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferParameterNullChecking, value); }
        }

        public string Style_PreferNullCheckOverTypeCheck
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck, value); }
        }

        public string Style_PreferConditionalExpressionOverAssignment
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment); }
            set { SetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, value); }
        }

        public string Style_PreferConditionalExpressionOverReturn
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn); }
            set { SetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn, value); }
        }

        public string Style_PreferCompoundAssignment
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferCompoundAssignment); }
            set { SetXmlOption(CodeStyleOptions2.PreferCompoundAssignment, value); }
        }

        public string Style_PreferSimplifiedInterpolation
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferSimplifiedInterpolation); }
            set { SetXmlOption(CodeStyleOptions2.PreferSimplifiedInterpolation, value); }
        }

        public string Style_RequireAccessibilityModifiers
        {
            get { return GetXmlOption(CodeStyleOptions2.RequireAccessibilityModifiers); }
            set { SetXmlOption(CodeStyleOptions2.RequireAccessibilityModifiers, value); }
        }

        public string Style_RemoveUnnecessarySuppressionExclusions
        {
            get { return GetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions); }
            set { SetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, value); }
        }

        public string Style_PreferSystemHashCode
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferSystemHashCode); }
            set { SetXmlOption(CodeStyleOptions2.PreferSystemHashCode, value); }
        }

        public string Style_PreferNamespaceAndFolderMatchStructure
        {
            get { return GetXmlOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure); }
            set { SetXmlOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure, value); }
        }

        public string Style_AllowMultipleBlankLines
        {
            get { return GetXmlOption(CodeStyleOptions2.AllowMultipleBlankLines); }
            set { SetXmlOption(CodeStyleOptions2.AllowMultipleBlankLines, value); }
        }

        public string Style_AllowStatementImmediatelyAfterBlock
        {
            get { return GetXmlOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock); }
            set { SetXmlOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, value); }
        }

        public string Style_PreferNotPattern
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferNotPattern); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferNotPattern, value); }
        }

        public string Style_PreferDeconstructedVariableDeclaration
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration, value); }
        }

        public string Style_PreferIndexOperator
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferIndexOperator); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferIndexOperator, value); }
        }

        public string Style_PreferRangeOperator
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferRangeOperator); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferRangeOperator, value); }
        }

        public string Style_PreferSimpleDefaultExpression
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, value); }
        }

        public string Style_PreferredModifierOrder
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferredModifierOrder); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferredModifierOrder, value); }
        }

        public string Style_PreferStaticLocalFunction
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferStaticLocalFunction); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferStaticLocalFunction, value); }
        }

        public string Style_PreferSimpleUsingStatement
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferSimpleUsingStatement, value); }
        }

        public string Style_PreferLocalOverAnonymousFunction
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, value); }
        }

        public string Style_PreferTupleSwap
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferTupleSwap); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferTupleSwap, value); }
        }

        public string Style_PreferredUsingDirectivePlacement
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement); }
            set { SetXmlOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, value); }
        }

        public string Style_UnusedValueExpressionStatement
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement); }
            set { SetXmlOption(CSharpCodeStyleOptions.UnusedValueExpressionStatement, value); }
        }

        public string Style_UnusedValueAssignment
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.UnusedValueAssignment); }
            set { SetXmlOption(CSharpCodeStyleOptions.UnusedValueAssignment, value); }
        }

        public string Style_ImplicitObjectCreationWhenTypeIsApparent
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent); }
            set { SetXmlOption(CSharpCodeStyleOptions.ImplicitObjectCreationWhenTypeIsApparent, value); }
        }

        public string Style_AllowEmbeddedStatementsOnSameLine
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine); }
            set { SetXmlOption(CSharpCodeStyleOptions.AllowEmbeddedStatementsOnSameLine, value); }
        }

        public string Style_AllowBlankLinesBetweenConsecutiveBraces
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces); }
            set { SetXmlOption(CSharpCodeStyleOptions.AllowBlankLinesBetweenConsecutiveBraces, value); }
        }

        public string Style_AllowBlankLineAfterColonInConstructorInitializer
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer); }
            set { SetXmlOption(CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer, value); }
        }

        public string Style_NamespaceDeclarations
        {
            get { return GetXmlOption(CSharpCodeStyleOptions.NamespaceDeclarations); }
            set { SetXmlOption(CSharpCodeStyleOptions.NamespaceDeclarations, value); }
        }
    }
}

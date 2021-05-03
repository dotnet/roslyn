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
    }
}

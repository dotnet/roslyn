' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Style_PreferIntrinsicPredefinedTypeKeywordInDeclaration_CodeStyle As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, value)
            End Set
        End Property

        Public Property Style_PreferIntrinsicPredefinedTypeKeywordInMemberAccess_CodeStyle As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, value)
            End Set
        End Property

        Public Property Style_QualifyFieldAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions2.QualifyFieldAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.QualifyFieldAccess, value)
            End Set
        End Property

        Public Property Style_QualifyPropertyAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions2.QualifyPropertyAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.QualifyPropertyAccess, value)
            End Set
        End Property

        Public Property Style_QualifyMethodAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions2.QualifyMethodAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.QualifyMethodAccess, value)
            End Set
        End Property

        Public Property Style_QualifyEventAccess As String
            Get
                Return GetXmlOption(CodeStyleOptions2.QualifyEventAccess)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.QualifyEventAccess, value)
            End Set
        End Property

        Public Property Style_PreferObjectInitializer As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferObjectInitializer)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferObjectInitializer, value)
            End Set
        End Property

        Public Property Style_PreferCollectionInitializer As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferCollectionInitializer)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferCollectionInitializer, value)
            End Set
        End Property

        Public Property Style_PreferSimplifiedBooleanExpressions As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, value)
            End Set
        End Property

        Public Property Style_PreferCoalesceExpression As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferCoalesceExpression)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferCoalesceExpression, value)
            End Set
        End Property

        Public Property Style_PreferNullPropagation As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferNullPropagation)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferNullPropagation, value)
            End Set
        End Property

        Public Property Style_PreferAutoProperties As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferAutoProperties)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferAutoProperties, value)
            End Set
        End Property

        Public Property Style_PreferInferredTupleNames As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferInferredTupleNames)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferInferredTupleNames, value)
            End Set
        End Property

        Public Property Style_PreferInferredAnonymousTypeMemberNames As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, value)
            End Set
        End Property

        Public Property Style_PreferExplicitTupleNames As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferExplicitTupleNames)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferExplicitTupleNames, value)
            End Set
        End Property

        Public Property Style_PreferIsNullCheckOverReferenceEqualityMethod As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, value)
            End Set
        End Property

        Public Property Style_PreferConditionalExpressionOverAssignment As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, value)
            End Set
        End Property

        Public Property Style_PreferConditionalExpressionOverReturn As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferConditionalExpressionOverReturn, value)
            End Set
        End Property

        Public Property Style_PreferCompoundAssignment As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferCompoundAssignment)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferCompoundAssignment, value)
            End Set
        End Property

        Public Property Style_PreferSimplifiedInterpolation As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferSimplifiedInterpolation)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferSimplifiedInterpolation, value)
            End Set
        End Property

        Public Property Style_RequireAccessibilityModifiers As String
            Get
                Return GetXmlOption(CodeStyleOptions2.AccessibilityModifiersRequired)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.AccessibilityModifiersRequired, value)
            End Set
        End Property

        Public Property Style_RemoveUnnecessarySuppressionExclusions As String
            Get
                Return GetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions)
            End Get
            Set(value As String)
                SetOption(CodeStyleOptions2.RemoveUnnecessarySuppressionExclusions, value)
            End Set
        End Property

        Public Property Style_PreferSystemHashCode As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferSystemHashCode)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferSystemHashCode, value)
            End Set
        End Property

        Public Property Style_PreferNamespaceAndFolderMatchStructure As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure, value)
            End Set
        End Property

        Public Property Style_AllowMultipleBlankLines As String
            Get
                Return GetXmlOption(CodeStyleOptions2.AllowMultipleBlankLines)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.AllowMultipleBlankLines, value)
            End Set
        End Property

        Public Property Style_AllowStatementImmediatelyAfterBlock As String
            Get
                Return GetXmlOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, value)
            End Set
        End Property

        Public Property Style_PreferReadonly As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferReadonly)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferReadonly, value)
            End Set
        End Property

        Public Property Style_PreferredModifierOrder As String
            Get
                Return GetXmlOption(VisualBasicCodeStyleOptions.PreferredModifierOrder)
            End Get
            Set(value As String)
                SetXmlOption(VisualBasicCodeStyleOptions.PreferredModifierOrder, value)
            End Set
        End Property

        Public Property Style_PreferIsNotExpression As String
            Get
                Return GetXmlOption(VisualBasicCodeStyleOptions.PreferIsNotExpression)
            End Get
            Set(value As String)
                SetXmlOption(VisualBasicCodeStyleOptions.PreferIsNotExpression, value)
            End Set
        End Property

        Public Property Style_PreferSimplifiedObjectCreation As String
            Get
                Return GetXmlOption(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation)
            End Get
            Set(value As String)
                SetXmlOption(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, value)
            End Set
        End Property

        Public Property Style_UnusedValueAssignment As String
            Get
                Return GetXmlOption(VisualBasicCodeStyleOptions.UnusedValueAssignment)
            End Get
            Set(value As String)
                SetXmlOption(VisualBasicCodeStyleOptions.UnusedValueAssignment, value)
            End Set
        End Property

        Public Property Style_UnusedValueExpressionStatement As String
            Get
                Return GetXmlOption(VisualBasicCodeStyleOptions.UnusedValueExpressionStatement)
            End Get
            Set(value As String)
                SetXmlOption(VisualBasicCodeStyleOptions.UnusedValueExpressionStatement, value)
            End Set
        End Property
    End Class
End Namespace

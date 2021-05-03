' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle

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

        Public Property Style_PreferReadonly As String
            Get
                Return GetXmlOption(CodeStyleOptions2.PreferReadonly)
            End Get
            Set(value As String)
                SetXmlOption(CodeStyleOptions2.PreferReadonly, value)
            End Set
        End Property
    End Class
End Namespace

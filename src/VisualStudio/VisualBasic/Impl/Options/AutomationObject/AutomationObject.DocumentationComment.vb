' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.DocumentationComments

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property AutoComment As Boolean
            Get
                Return GetBooleanOption(DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration)
            End Get
            Set(value As Boolean)
                SetBooleanOption(DocumentationCommentOptionsStorage.AutoXmlDocCommentGeneration, value)
            End Set
        End Property

        Public Property GenerateSummaryTagOnSingleLine As Boolean
            Get
                Return GetBooleanOption(DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine)
            End Get
            Set(value As Boolean)
                SetBooleanOption(DocumentationCommentOptionsStorage.GenerateSummaryTagOnSingleLine, value)
            End Set
        End Property

        Public Property GenerateOnlySummaryTag As Boolean
            Get
                Return GetBooleanOption(DocumentationCommentOptionsStorage.GenerateOnlySummaryTag)
            End Get
            Set(value As Boolean)
                SetBooleanOption(DocumentationCommentOptionsStorage.GenerateOnlySummaryTag, value)
            End Set
        End Property
    End Class
End Namespace

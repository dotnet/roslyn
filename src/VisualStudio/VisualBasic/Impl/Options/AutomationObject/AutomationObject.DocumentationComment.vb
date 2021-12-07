' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.DocumentationComments

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property AutoComment As Boolean
            Get
                Return GetBooleanOption(DocumentationCommentOptions.Metadata.AutoXmlDocCommentGeneration)
            End Get
            Set(value As Boolean)
                SetBooleanOption(DocumentationCommentOptions.Metadata.AutoXmlDocCommentGeneration, value)
            End Set
        End Property
    End Class
End Namespace

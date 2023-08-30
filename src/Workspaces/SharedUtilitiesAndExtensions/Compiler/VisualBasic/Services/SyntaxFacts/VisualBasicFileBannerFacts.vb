' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageService
    Friend Class VisualBasicFileBannerFacts
        Inherits AbstractFileBannerFacts

        Public Shared ReadOnly Instance As IFileBannerFacts = New VisualBasicFileBannerFacts()

        Protected Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts
            Get
                Return VisualBasicSyntaxFacts.Instance
            End Get
        End Property

        Protected Overrides ReadOnly Property DocumentationCommentService As IDocumentationCommentService
            Get
                Return VisualBasicDocumentationCommentService.Instance
            End Get
        End Property
    End Class
End Namespace

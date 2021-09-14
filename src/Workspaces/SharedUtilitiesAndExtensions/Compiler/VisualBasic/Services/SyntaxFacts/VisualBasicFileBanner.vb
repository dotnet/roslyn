' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices

#If CODE_STYLE Then
Imports Microsoft.CodeAnalysis.Internal.Editing
#Else
Imports Microsoft.CodeAnalysis.Editing
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageServices
    Friend Class VisualBasicFileBanner
        Inherits AbstractFileBanner

        Public Shared ReadOnly Instance As IFileBanner = New VisualBasicFileBanner()

        Protected Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance
        Protected Overrides ReadOnly Property DocumentationCommentService As IDocumentationCommentService = VisualBasicDocumentationCommentService.Instance
    End Class
End Namespace

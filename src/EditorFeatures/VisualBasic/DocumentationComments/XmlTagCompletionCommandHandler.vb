' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name("XmlTagCompletionCommandHandler")>
    <Order(Before:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend NotInheritable Class XmlTagCompletionCommandHandler
        Inherits AbstractXmlTagCompletionCommandHandler(Of
            XmlNameSyntax,
            XmlTextSyntax,
            XmlElementSyntax,
            XmlElementStartTagSyntax,
            XmlElementEndTagSyntax,
            DocumentationCommentTriviaSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(undoHistory As ITextUndoHistoryRegistry)
            MyBase.New(undoHistory)
        End Sub

        Protected Overrides Function GetStartTag(xmlElement As XmlElementSyntax) As XmlElementStartTagSyntax
            Return xmlElement.StartTag
        End Function

        Protected Overrides Function GetEndTag(xmlElement As XmlElementSyntax) As XmlElementEndTagSyntax
            Return xmlElement.EndTag
        End Function

        Protected Overrides Function GetName(startTag As XmlElementStartTagSyntax) As XmlNameSyntax
            Return DirectCast(startTag.Name, XmlNameSyntax)
        End Function

        Protected Overrides Function GetName(endTag As XmlElementEndTagSyntax) As XmlNameSyntax
            Return endTag.Name
        End Function

        Protected Overrides Function GetLocalName(name As XmlNameSyntax) As SyntaxToken
            Return If(name Is Nothing, Nothing, name.LocalName)
        End Function
    End Class
End Namespace

' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    ''' <summary>
    ''' Identifiers coming from IL can be just about any valid string and since VB doesn't have a way to escape all possible
    ''' IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    ''' </summary>
    <Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
    Public Class InvalidIdentifierStructureTests
        Inherits AbstractSyntaxStructureProviderTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property WorkspaceKind As String
            Get
                Return CodeAnalysis.WorkspaceKind.MetadataAsSource
            End Get
        End Property

        Friend Overrides Async Function GetBlockSpansWorkerAsync(document As Document, options As BlockStructureOptions, position As Integer) As Task(Of ImmutableArray(Of BlockSpan))
            Dim outliningService = document.GetLanguageService(Of BlockStructureService)()

            Return (Await outliningService.GetBlockStructureAsync(document, options, CancellationToken.None)).Spans
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        Public Async Function PrependDollarSign() As Task
            Const code = "
{|hint:{|textspan:$$Class C
    Public Sub $Invoke()
End Class|}|}
"
            Await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", "Class C " & Ellipsis, autoCollapse:=False))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        Public Async Function SymbolsAndPunctuation() As Task
            Const code = "
$$Class C
    Public Sub !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`()
End Class
"
            Await VerifyNoBlockSpansAsync(code)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        Public Async Function IdentifierThatLooksLikeCode() As Task
            Const code = "
{|hint1:{|textspan1:$$Class C
    {|hint2:{|textspan2:Public Sub : End Sub|}|} : End Class|}|} "" now the document is a string until the next quote ()
End Class
"
            Await VerifyBlockSpansAsync(code,
                Region("textspan2", "hint2", "Public Sub  " & Ellipsis, autoCollapse:=True),
                Region("textspan1", "hint1", "Class C " & Ellipsis, autoCollapse:=False))
        End Function

    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure
Imports Microsoft.CodeAnalysis.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    ''' <summary>
    ''' Identifiers coming from IL can be just about any valid string and since VB doesn't have a way to escape all possible
    ''' IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    ''' </summary>
    Public Class InvalidIdentifierTests
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

        Friend Overrides Async Function GetBlockSpansWorkerAsync(document As Document, position As Integer) As Task(Of ImmutableArray(Of BlockSpan))
            Dim outliningService = document.GetLanguageService(Of BlockStructureService)()

            Return (Await outliningService.GetBlockStructureAsync(document, CancellationToken.None)).Spans
        End Function

        <WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function PrependDollarSign() As Task
            Const code = "
$$Class C
    Public Sub $Invoke()
End Class
"
            Await VerifyNoBlockSpansAsync(code)
        End Function

        <WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function SymbolsAndPunctuation() As Task
            Const code = "
$$Class C
    Public Sub !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`()
End Class
"
            Await VerifyNoBlockSpansAsync(code)
        End Function

        <WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function IdentifierThatLooksLikeCode() As Task
            Const code = "
$$Class C
    Public Sub : End Sub : End Class "" now the document is a string until the next quote ()
End Class
"
            Await VerifyNoBlockSpansAsync(code)
        End Function

    End Class
End Namespace

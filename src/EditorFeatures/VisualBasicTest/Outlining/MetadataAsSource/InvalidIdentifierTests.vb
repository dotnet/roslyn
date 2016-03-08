' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    ''' <summary>
    ''' Identifiers coming from IL can be just about any valid string and since VB doesn't have a way to escape all possible
    ''' IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    ''' </summary>
    Public Class InvalidIdentifierTests
        Inherits AbstractSyntaxOutlinerTests

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

        Friend Overrides Async Function GetRegionsAsync(document As Document, position As Integer) As Task(Of OutliningSpan())
            Dim outliningService = document.Project.LanguageServices.GetService(Of IOutliningService)()

            Return (Await outliningService.GetOutliningSpansAsync(document, CancellationToken.None)) _
                .WhereNotNull() _
                .ToArray()
        End Function

        <WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function PrependDollarSign() As Task
            Const code = "
$$Class C
    Public Sub $Invoke()
End Class
"
            Await VerifyNoRegionsAsync(code)
        End Function

        <WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function SymbolsAndPunctuation() As Task
            Const code = "
$$Class C
    Public Sub !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`()
End Class
"
            Await VerifyNoRegionsAsync(code)
        End Function

        <WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")>
        <Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function IdentifierThatLooksLikeCode() As Task
            Const code = "
$$Class C
    Public Sub : End Sub : End Class "" now the document is a string until the next quote ()
End Class
"
            Await VerifyNoRegionsAsync(code)
        End Function

    End Class
End Namespace
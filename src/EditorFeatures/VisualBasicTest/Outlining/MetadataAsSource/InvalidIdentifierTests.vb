' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining.MetadataAsSource
    ''' <summary>
    ''' Identifiers coming from IL can be just about any valid string and since VB doesn't have a way to escape all possible
    ''' IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    ''' </summary>
    Public Class InvalidIdentifierTests
        Inherits AbstractOutlinerTests

        Private Async Function TestAsync(fileContents As String, ParamArray ByVal expectedSpans As OutliningSpan()) As Tasks.Task
            Dim workspace = Await TestWorkspaceFactory.CreateWorkspaceFromFilesAsync(WorkspaceKind.MetadataAsSource, LanguageNames.VisualBasic, Nothing, Nothing, fileContents)
            Dim outliningService = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService(Of IOutliningService)()
            Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
            Dim actualOutliningSpans = (Await outliningService.GetOutliningSpansAsync(document, CancellationToken.None)).Where(Function(s) s IsNot Nothing).ToArray()

            Assert.Equal(expectedSpans.Length, actualOutliningSpans.Length)
            For i As Integer = 0 To expectedSpans.Length - 1
                AssertRegion(expectedSpans(i), actualOutliningSpans(i))
            Next
        End Function

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function PrependDollarSign() As Tasks.Task
            Dim source = "
Class C
    Public Sub $Invoke()
End Class
"
            Await TestAsync(source)
        End Function

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function SymbolsAndPunctuation() As Tasks.Task
            Dim source = "
Class C
    Public Sub !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`()
End Class
"
            Await TestAsync(source)
        End Function

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Async Function IdentifierThatLooksLikeCode() As Tasks.Task
            Dim source = "
Class C
    Public Sub : End Sub : End Class "" now the document is a string until the next quote ()
End Class
"
            Await TestAsync(source)
        End Function
    End Class
End Namespace
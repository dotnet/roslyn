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

        Private Sub Test(fileContents As String, ParamArray ByVal expectedSpans As OutliningSpan())
            Dim workspace = TestWorkspaceFactory.CreateWorkspaceFromFiles(WorkspaceKind.MetadataAsSource, LanguageNames.VisualBasic, Nothing, Nothing, fileContents)
            Dim outliningService = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService(Of IOutliningService)()
            Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
            Dim actualOutliningSpans = outliningService.GetOutliningSpansAsync(document, CancellationToken.None).Result.Where(Function(s) s IsNot Nothing).ToArray()

            Assert.Equal(expectedSpans.Length, actualOutliningSpans.Length)
            For i As Integer = 0 To expectedSpans.Length - 1
                AssertRegion(expectedSpans(i), actualOutliningSpans(i))
            Next
        End Sub

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub PrependDollarSign()
            Dim source = "
Class C
    Public Sub $Invoke()
End Class
"
            Test(source)
        End Sub

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub SymbolsAndPunctuation()
            Dim source = "
Class C
    Public Sub !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`()
End Class
"
            Test(source)
        End Sub

        <WorkItem(1174405)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)>
        Public Sub IdentifierThatLooksLikeCode()
            Dim source = "
Class C
    Public Sub : End Sub : End Class "" now the document is a string until the next quote ()
End Class
"
            Test(source)
        End Sub

    End Class
End Namespace

' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        Protected Shared Async Function ExpectExtractMethodToFailAsync(codeWithMarker As XElement, Optional dontPutOutOrRefOnStruct As Boolean = True) As Tasks.Task
            Dim codeWithoutMarker As String = Nothing
            Dim textSpan As TextSpan
            MarkupTestFile.GetSpan(codeWithMarker.NormalizedValue, codeWithoutMarker, textSpan)

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(codeWithoutMarker)
                Dim treeAfterExtractMethod = Await ExtractMethodAsync(workspace, workspace.Documents.First(), textSpan, succeeded:=False, dontPutOutOrRefOnStruct:=dontPutOutOrRefOnStruct)
            End Using
        End Function

        Private Shared Async Function NotSupported_ExtractMethodAsync(codeWithMarker As XElement) As Tasks.Task
            Dim codeWithoutMarker As String = Nothing
            Dim textSpan As TextSpan
            MarkupTestFile.GetSpan(codeWithMarker.NormalizedValue, codeWithoutMarker, textSpan)

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(codeWithoutMarker)
                Assert.NotNull(Await Record.ExceptionAsync(Async Function()
                                                               Dim tree = Await ExtractMethodAsync(workspace, workspace.Documents.First(), textSpan)
                                                           End Function))
            End Using
        End Function

        Protected Overloads Shared Async Function TestExtractMethodAsync(
            codeWithMarker As String,
            expected As String,
            Optional temporaryFailing As Boolean = False,
            Optional allowMovingDeclaration As Boolean = True,
            Optional dontPutOutOrRefOnStruct As Boolean = True,
            Optional metadataReference As String = Nothing,
            Optional compareTokens As Boolean = False
        ) As Tasks.Task

            Dim metadataReferences = If(metadataReference Is Nothing, Array.Empty(Of String)(), New String() {metadataReference})

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(New String() {codeWithMarker}, metadataReferences:=metadataReferences, compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

                Dim document = workspace.Documents.First()
                Dim subjectBuffer = document.TextBuffer
                Dim textSpan = document.SelectedSpans.First()

                Dim tree = Await ExtractMethodAsync(workspace, workspace.Documents.First(), textSpan, allowMovingDeclaration:=allowMovingDeclaration, dontPutOutOrRefOnStruct:=dontPutOutOrRefOnStruct)

                Using edit = subjectBuffer.CreateEdit()
                    edit.Replace(0, edit.Snapshot.Length, tree.ToFullString())
                    edit.Apply()
                End Using

                If temporaryFailing Then
                    Assert.NotEqual(expected, subjectBuffer.CurrentSnapshot.GetText())
                Else
                    If compareTokens Then
                        TokenUtilities.AssertTokensEqual(expected, subjectBuffer.CurrentSnapshot.GetText(), LanguageNames.VisualBasic)
                    Else
                        Assert.Equal(expected, subjectBuffer.CurrentSnapshot.GetText())
                    End If
                End If
            End Using
        End Function

        Protected Overloads Shared Async Function TestExtractMethodAsync(
            codeWithMarker As XElement,
            expected As XElement,
            Optional temporaryFailing As Boolean = False,
            Optional allowMovingDeclaration As Boolean = True,
            Optional dontPutOutOrRefOnStruct As Boolean = True,
            Optional metadataReference As String = Nothing,
            Optional compareTokens As Boolean = False
        ) As Tasks.Task

            Await TestExtractMethodAsync(codeWithMarker.NormalizedValue, expected.NormalizedValue, temporaryFailing, allowMovingDeclaration, dontPutOutOrRefOnStruct, metadataReference, compareTokens)
        End Function

        Private Shared Async Function ExtractMethodAsync(workspace As TestWorkspace,
                                              testDocument As TestHostDocument,
                                              textSpan As TextSpan,
                                              Optional succeeded As Boolean = True,
                                              Optional allowMovingDeclaration As Boolean = True,
                                              Optional dontPutOutOrRefOnStruct As Boolean = True) As Tasks.Task(Of SyntaxNode)
            Dim snapshotSpan = textSpan.ToSnapshotSpan(testDocument.TextBuffer.CurrentSnapshot)

            Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)
            Assert.NotNull(document)

            Dim options = document.Project.Solution.Workspace.Options.
                                   WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, allowMovingDeclaration).
                                   WithChangedOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language, dontPutOutOrRefOnStruct)

            Dim sdocument = Await SemanticDocument.CreateAsync(document, CancellationToken.None)
            Dim validator = New VisualBasicSelectionValidator(sdocument, snapshotSpan.Span.ToTextSpan(), options)

            Dim selectedCode = Await validator.GetValidSelectionAsync(CancellationToken.None)
            If Not succeeded And selectedCode.Status.Failed() Then
                Return Nothing
            End If

            Assert.True(selectedCode.ContainsValidContext)

            ' extract method
            Dim extractor = New VisualBasicMethodExtractor(CType(selectedCode, VisualBasicSelectionResult))
            Dim result = Await extractor.ExtractMethodAsync(CancellationToken.None)
            Assert.NotNull(result)
            Assert.Equal(succeeded, result.Succeeded OrElse result.SucceededWithSuggestion)

            Return Await result.Document.GetSyntaxRootAsync()
        End Function

        Private Shared Async Function TestSelectionAsync(codeWithMarker As XElement, Optional ByVal expectedFail As Boolean = False) As Tasks.Task
            Dim codeWithoutMarker As String = Nothing
            Dim namedSpans = CType(New Dictionary(Of String, IList(Of TextSpan))(), IDictionary(Of String, IList(Of TextSpan)))

            MarkupTestFile.GetSpans(codeWithMarker.NormalizedValue, codeWithoutMarker, namedSpans)

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(codeWithoutMarker)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Assert.NotNull(document)

                Dim options = document.Project.Solution.Workspace.Options.WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, True)
                Dim sdocument = Await SemanticDocument.CreateAsync(document, CancellationToken.None)
                Dim validator = New VisualBasicSelectionValidator(sdocument, namedSpans("b").Single(), options)
                Dim result = Await validator.GetValidSelectionAsync(CancellationToken.None)

                If expectedFail Then
                    Assert.True(result.Status.Failed(), "Selection didn't fail as expected")
                Else
                    Assert.True(Microsoft.CodeAnalysis.ExtractMethod.Extensions.Succeeded(result.Status), "Selection wasn't expected to fail")
                End If

                If (Microsoft.CodeAnalysis.ExtractMethod.Extensions.Succeeded(result.Status) OrElse result.Status.Flag.HasBestEffort()) AndAlso result.Status.Flag.HasSuggestion() Then
                    Assert.Equal(namedSpans("r").Single(), result.FinalSpan)
                End If
            End Using
        End Function

        Private Shared Async Function TestInMethodAsync(codeWithMarker As XElement, Optional ByVal expectedFail As Boolean = False) As Tasks.Task
            Dim markupWithMarker = <text>Class C
    Sub S<%= codeWithMarker.Value %>    End Sub
End Class</text>
            Await TestSelectionAsync(markupWithMarker, expectedFail)
        End Function

        Private Shared Async Function IterateAllAsync(code As String) As Tasks.Task
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicAsync(code)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Assert.NotNull(document)

                Dim sdocument = Await SemanticDocument.CreateAsync(document, CancellationToken.None)

                Dim root = Await document.GetSyntaxRootAsync()
                Dim iterator = root.DescendantNodesAndSelf()

                Dim options = document.Project.Solution.Workspace.Options _
                                      .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, True)

                For Each node In iterator
                    Try
                        Dim validator = New VisualBasicSelectionValidator(sdocument, node.Span, options)
                        Dim result = Await validator.GetValidSelectionAsync(CancellationToken.None)

                        ' check the obvious case
                        If Not (TypeOf node Is ExpressionSyntax) AndAlso (Not node.UnderValidContext()) Then
                            Assert.True(result.Status.Flag.Failed())
                        End If
                    Catch e1 As ArgumentException
                        ' catch and ignore unknown issue. currently control flow analysis engine doesn't support field initializer.
                    End Try
                Next node
            End Using
        End Function
    End Class
End Namespace

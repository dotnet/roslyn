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

            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync(codeWithoutMarker)
                Dim treeAfterExtractMethod = ExtractMethod(workspace, workspace.Documents.First(), textSpan, succeeded:=False, dontPutOutOrRefOnStruct:=dontPutOutOrRefOnStruct)
            End Using
        End Function

        Private Shared Sub NotSupported_ExtractMethod(codeWithMarker As XElement)
            Dim codeWithoutMarker As String = Nothing
            Dim textSpan As TextSpan
            MarkupTestFile.GetSpan(codeWithMarker.NormalizedValue, codeWithoutMarker, textSpan)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(codeWithoutMarker)
                Assert.NotNull(Record.Exception(Sub()
                                                    Dim tree = ExtractMethod(workspace, workspace.Documents.First(), textSpan)
                                                End Sub))
            End Using
        End Sub

        Protected Overloads Shared Sub TestExtractMethod(
            codeWithMarker As String,
            expected As String,
            Optional temporaryFailing As Boolean = False,
            Optional allowMovingDeclaration As Boolean = True,
            Optional dontPutOutOrRefOnStruct As Boolean = True,
            Optional metadataReference As String = Nothing,
            Optional compareTokens As Boolean = False
        )

            Dim metadataReferences = If(metadataReference Is Nothing, Array.Empty(Of String)(), New String() {metadataReference})

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFiles(New String() {codeWithMarker}, metadataReferences:=metadataReferences, compilationOptions:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

                Dim document = workspace.Documents.First()
                Dim subjectBuffer = document.TextBuffer
                Dim textSpan = document.SelectedSpans.First()

                Dim tree = ExtractMethod(workspace, workspace.Documents.First(), textSpan, allowMovingDeclaration:=allowMovingDeclaration, dontPutOutOrRefOnStruct:=dontPutOutOrRefOnStruct)

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
        End Sub

        Protected Overloads Shared Sub TestExtractMethod(
            codeWithMarker As XElement,
            expected As XElement,
            Optional temporaryFailing As Boolean = False,
            Optional allowMovingDeclaration As Boolean = True,
            Optional dontPutOutOrRefOnStruct As Boolean = True,
            Optional metadataReference As String = Nothing,
            Optional compareTokens As Boolean = False
        )

            TestExtractMethod(codeWithMarker.NormalizedValue, expected.NormalizedValue, temporaryFailing, allowMovingDeclaration, dontPutOutOrRefOnStruct, metadataReference, compareTokens)
        End Sub

        Private Shared Function ExtractMethod(workspace As TestWorkspace,
                                              testDocument As TestHostDocument,
                                              textSpan As TextSpan,
                                              Optional succeeded As Boolean = True,
                                              Optional allowMovingDeclaration As Boolean = True,
                                              Optional dontPutOutOrRefOnStruct As Boolean = True) As SyntaxNode
            Dim snapshotSpan = textSpan.ToSnapshotSpan(testDocument.TextBuffer.CurrentSnapshot)

            Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)
            Assert.NotNull(document)

            Dim options = document.Project.Solution.Workspace.Options.
                                   WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, allowMovingDeclaration).
                                   WithChangedOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language, dontPutOutOrRefOnStruct)

            Dim sdocument = SemanticDocument.CreateAsync(document, CancellationToken.None).Result
            Dim validator = New VisualBasicSelectionValidator(sdocument, snapshotSpan.Span.ToTextSpan(), options)

            Dim selectedCode = validator.GetValidSelectionAsync(CancellationToken.None).Result
            If Not succeeded And selectedCode.Status.Failed() Then
                Return Nothing
            End If

            Assert.True(selectedCode.ContainsValidContext)

            ' extract method
            Dim extractor = New VisualBasicMethodExtractor(CType(selectedCode, VisualBasicSelectionResult))
            Dim result = extractor.ExtractMethodAsync(CancellationToken.None).Result
            Assert.NotNull(result)
            Assert.Equal(succeeded, result.Succeeded OrElse result.SucceededWithSuggestion)

            Return result.Document.GetSyntaxRootAsync().Result
        End Function

        Private Shared Sub TestSelection(codeWithMarker As XElement, Optional ByVal expectedFail As Boolean = False)
            Dim codeWithoutMarker As String = Nothing
            Dim namedSpans = CType(New Dictionary(Of String, IList(Of TextSpan))(), IDictionary(Of String, IList(Of TextSpan)))

            MarkupTestFile.GetSpans(codeWithMarker.NormalizedValue, codeWithoutMarker, namedSpans)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(codeWithoutMarker)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Assert.NotNull(document)

                Dim options = document.Project.Solution.Workspace.Options.WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, True)
                Dim sdocument = SemanticDocument.CreateAsync(document, CancellationToken.None).Result
                Dim validator = New VisualBasicSelectionValidator(sdocument, namedSpans("b").Single(), options)
                Dim result = validator.GetValidSelectionAsync(CancellationToken.None).Result

                If expectedFail Then
                    Assert.True(result.Status.Failed(), "Selection didn't fail as expected")
                Else
                    Assert.True(Microsoft.CodeAnalysis.ExtractMethod.Extensions.Succeeded(result.Status), "Selection wasn't expected to fail")
                End If

                If (Microsoft.CodeAnalysis.ExtractMethod.Extensions.Succeeded(result.Status) OrElse result.Status.Flag.HasBestEffort()) AndAlso result.Status.Flag.HasSuggestion() Then
                    Assert.Equal(namedSpans("r").Single(), result.FinalSpan)
                End If
            End Using
        End Sub

        Private Shared Sub TestInMethod(codeWithMarker As XElement, Optional ByVal expectedFail As Boolean = False)
            Dim markupWithMarker = <text>Class C
    Sub S<%= codeWithMarker.Value %>    End Sub
End Class</text>
            TestSelection(markupWithMarker, expectedFail)
        End Sub

        Private Shared Sub IterateAll(ByVal code As String)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Assert.NotNull(document)

                Dim sdocument = SemanticDocument.CreateAsync(document, CancellationToken.None).Result

                Dim tree = document.GetSyntaxTreeAsync().Result
                Dim iterator = tree.GetRoot().DescendantNodesAndSelf()

                Dim options = document.Project.Solution.Workspace.Options _
                                      .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, True)

                For Each node In iterator
                    Try
                        Dim validator = New VisualBasicSelectionValidator(sdocument, node.Span, options)
                        Dim result = validator.GetValidSelectionAsync(CancellationToken.None).Result

                        ' check the obvious case
                        If Not (TypeOf node Is ExpressionSyntax) AndAlso (Not node.UnderValidContext()) Then
                            Assert.True(result.Status.Flag.Failed())
                        End If
                    Catch e1 As ArgumentException
                        ' catch and ignore unknown issue. currently control flow analysis engine doesn't support field initializer.
                    End Try
                Next node
            End Using
        End Sub
    End Class
End Namespace

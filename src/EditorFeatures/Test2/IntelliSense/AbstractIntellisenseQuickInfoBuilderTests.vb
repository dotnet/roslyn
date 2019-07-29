' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Adornments
Imports Moq

Imports VSQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    Public MustInherit Class AbstractIntellisenseQuickInfoBuilderTests
        Protected Async Function GetQuickInfoItemAsync(quickInfoItem As QuickInfoItem) As Task(Of VSQuickInfoItem)
            Dim workspaceDefinition =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            $$
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorBuffer = cursorDocument.TextBuffer

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                    .DefaultValue = DefaultValue.Mock
                }

                Dim streamingPresenter = workspace.ExportProvider.GetExport(Of IStreamingFindUsagesPresenter)()
                Return Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, quickInfoItem, cursorBuffer.CurrentSnapshot, document, streamingPresenter, CancellationToken.None)
            End Using
        End Function

        Protected Async Function GetQuickInfoItemAsync(workspaceDefinition As XElement, language As String) As Task(Of VSQuickInfoItem)
            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim cursorBuffer = cursorDocument.TextBuffer

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim languageServiceProvider = workspace.Services.GetLanguageServices(language)
                Dim quickInfoService = languageServiceProvider.GetRequiredService(Of QuickInfoService)

                Dim codeAnalysisQuickInfoItem = Await quickInfoService.GetQuickInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(False)

                Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                    .DefaultValue = DefaultValue.Mock
                }

                Dim streamingPresenter = workspace.ExportProvider.GetExport(Of IStreamingFindUsagesPresenter)()
                Return Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, cursorBuffer.CurrentSnapshot, document, streamingPresenter, CancellationToken.None)
            End Using
        End Function

        Protected Shared Sub AssertEqualAdornments(expected As Object, actual As Object)
            Try
                Assert.IsType(expected.GetType, actual)

                Dim containerElement = TryCast(expected, ContainerElement)
                If containerElement IsNot Nothing Then
                    AssertEqualContainerElement(containerElement, DirectCast(actual, ContainerElement))
                    Return
                End If

                Dim imageElement = TryCast(expected, ImageElement)
                If imageElement IsNot Nothing Then
                    AssertEqualImageElement(imageElement, DirectCast(actual, ImageElement))
                    Return
                End If

                Dim classifiedTextElement = TryCast(expected, ClassifiedTextElement)
                If classifiedTextElement IsNot Nothing Then
                    AssertEqualClassifiedTextElement(classifiedTextElement, DirectCast(actual, ClassifiedTextElement))
                    Return
                End If

                Dim classifiedTextRun = TryCast(expected, ClassifiedTextRun)
                If classifiedTextRun IsNot Nothing Then
                    AssertEqualClassifiedTextRun(classifiedTextRun, DirectCast(actual, ClassifiedTextRun))
                    Return
                End If

                Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
            Catch ex As Exception
                Dim renderedExpected = ContainerToString(expected)
                Dim renderedActual = ContainerToString(actual)
                AssertEx.EqualOrDiff(renderedExpected, renderedActual)

                ' This is not expected to be hit, but it will be hit if the difference cannot be detected within the diff
                Throw
            End Try
        End Sub

        Private Shared Sub AssertEqualContainerElement(expected As ContainerElement, actual As ContainerElement)
            Assert.Equal(expected.Style, actual.Style)
            Assert.Equal(expected.Elements.Count, actual.Elements.Count)
            For Each pair In expected.Elements.Zip(actual.Elements, Function(expectedElement, actualElement) (expectedElement, actualElement))
                AssertEqualAdornments(pair.expectedElement, pair.actualElement)
            Next
        End Sub

        Private Shared Sub AssertEqualImageElement(expected As ImageElement, actual As ImageElement)
            Assert.Equal(expected.ImageId.Guid, actual.ImageId.Guid)
            Assert.Equal(expected.ImageId.Id, actual.ImageId.Id)
            Assert.Equal(expected.AutomationName, actual.AutomationName)
        End Sub

        Private Shared Sub AssertEqualClassifiedTextElement(expected As ClassifiedTextElement, actual As ClassifiedTextElement)
            Assert.Equal(expected.Runs.Count, actual.Runs.Count)
            For Each pair In expected.Runs.Zip(actual.Runs, Function(expectedRun, actualRun) (expectedRun, actualRun))
                AssertEqualClassifiedTextRun(pair.expectedRun, pair.actualRun)
            Next
        End Sub

        Private Shared Sub AssertEqualClassifiedTextRun(expected As ClassifiedTextRun, actual As ClassifiedTextRun)
            Assert.Equal(expected.ClassificationTypeName, actual.ClassificationTypeName)
            Assert.Equal(expected.Text, actual.Text)
            Assert.Equal(expected.Tooltip, actual.Tooltip)
            Assert.Equal(expected.Style, actual.Style)
        End Sub

        Private Shared Function ContainerToString(element As Object) As String
            Dim result = New StringBuilder
            ContainerToString(element, "", result)
            Return result.ToString()
        End Function

        Private Shared Sub ContainerToString(element As Object, indent As String, result As StringBuilder)
            result.Append($"{indent}New {element.GetType().Name}(")

            Dim container = TryCast(element, ContainerElement)
            If container IsNot Nothing Then
                result.AppendLine()
                indent += "    "
                result.AppendLine($"{indent}{ContainerStyleToString(container.Style)},")
                Dim elements = container.Elements.ToArray()
                For i = 0 To elements.Length - 1
                    ContainerToString(elements(i), indent, result)

                    If i < elements.Length - 1 Then
                        result.AppendLine(",")
                    Else
                        result.Append(")")
                    End If
                Next

                Return
            End If

            Dim image = TryCast(element, ImageElement)
            If image IsNot Nothing Then
                Dim guid = GetKnownImageGuid(image.ImageId.Guid)
                Dim id = GetKnownImageId(image.ImageId.Id)
                result.Append($"New {NameOf(ImageId)}({guid}, {id}))")
                Return
            End If

            Dim classifiedTextElement = TryCast(element, ClassifiedTextElement)
            If classifiedTextElement IsNot Nothing Then
                result.AppendLine()
                indent += "    "
                Dim runs = classifiedTextElement.Runs.ToArray()
                For i = 0 To runs.Length - 1
                    ContainerToString(runs(i), indent, result)

                    If i < runs.Length - 1 Then
                        result.AppendLine(",")
                    Else
                        result.Append(")")
                    End If
                Next

                Return
            End If

            Dim classifiedTextRun = TryCast(element, ClassifiedTextRun)
            If classifiedTextRun IsNot Nothing Then
                Dim classification = GetKnownClassification(classifiedTextRun.ClassificationTypeName)
                result.Append($"{classification}, ""{classifiedTextRun.Text.Replace("""", """""")}""")
                If classifiedTextRun.NavigationAction IsNot Nothing OrElse Not String.IsNullOrEmpty(classifiedTextRun.Tooltip) Then
                    Dim tooltip = If(classifiedTextRun.Tooltip IsNot Nothing, $"""{classifiedTextRun.Tooltip.Replace("""", """""")}""", "Nothing")
                    result.Append($", navigationAction:=Sub() Return, {tooltip}")
                End If

                If classifiedTextRun.Style <> ClassifiedTextRunStyle.Plain Then
                    result.Append($", {TextRunStyleToString(classifiedTextRun.Style)}")
                End If

                result.Append(")")
                Return
            End If

            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Sub

        Private Shared Function ContainerStyleToString(style As ContainerElementStyle) As String
            Dim stringValue = style.ToString()
            Return String.Join(" Or ", stringValue.Split({","c, " "c}, StringSplitOptions.RemoveEmptyEntries).Select(Function(value) $"{NameOf(ContainerElementStyle)}.{value}"))
        End Function

        Private Shared Function TextRunStyleToString(style As ClassifiedTextRunStyle) As String
            Dim stringValue = style.ToString()
            Return String.Join(" Or ", stringValue.Split({","c, " "c}, StringSplitOptions.RemoveEmptyEntries).Select(Function(value) $"{NameOf(ClassifiedTextRunStyle)}.{value}"))
        End Function

        Private Shared Function GetKnownClassification(classification As String) As String
            For Each field In GetType(ClassificationTypeNames).GetFields()
                If Not field.IsStatic Then
                    Continue For
                End If

                Dim rawValue = field.GetValue(Nothing)
                Dim value = TryCast(rawValue, String)
                If value = classification Then
                    Return $"{NameOf(ClassificationTypeNames)}.{field.Name}"
                End If
            Next

            Return $"""{classification}"""
        End Function

        Private Shared Function GetKnownImageGuid(guid As Guid) As String
            For Each field In GetType(KnownImageIds).GetFields()
                If Not field.IsStatic Then
                    Continue For
                End If

                Dim rawValue = field.GetValue(Nothing)
                Dim value As Guid? = If(TypeOf rawValue Is Guid, DirectCast(rawValue, Guid), Nothing)
                If value = guid Then
                    Return $"{NameOf(KnownImageIds)}.{field.Name}"
                End If
            Next

            Return guid.ToString()
        End Function

        Private Shared Function GetKnownImageId(id As Integer) As String
            For Each field In GetType(KnownImageIds).GetFields()
                If Not field.IsStatic Then
                    Continue For
                End If

                Dim rawValue = field.GetValue(Nothing)
                Dim value As Integer? = If(TypeOf rawValue Is Integer, CInt(rawValue), Nothing)
                If value = id Then
                    Return $"{NameOf(KnownImageIds)}.{field.Name}"
                End If
            Next

            Return id.ToString()
        End Function
    End Class
End Namespace

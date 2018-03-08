' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Adornments
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq
Imports QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class IntellisenseQuickInfoBuilderTests

        Public Sub New()
            TestWorkspace.ResetThreadAffinity()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub BuildQuickInfoItem()

            Dim codeAnalysisQuickInfoItem _
                    = QuickInfoItem.Create(New Text.TextSpan(0, 0), ImmutableArray.Create({"Method", "Public"}),
                        ImmutableArray.Create _
                            ({QuickInfoSection.Create("Description",
                                ImmutableArray.Create({
                                    New TaggedText("Keyword", "void"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Class", "Console"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Method", "WriteLine"),
                                    New TaggedText("Punctuation", "("),
                                    New TaggedText("Keyword", "string"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Parameter", "value"),
                                    New TaggedText("Punctuation", ")"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Punctuation", "("),
                                    New TaggedText("Punctuation", "+"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Text", "18"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Text", "overloads"),
                                    New TaggedText("Punctuation", ")")})),
                            QuickInfoSection.Create("DocumentationComments",
                                ImmutableArray.Create({New TaggedText("Text", "Writes the specified string value, followed by the current line terminator, to the standard output stream.")})),
                            QuickInfoSection.Create("Exception",
                                ImmutableArray.Create({
                                    New TaggedText("Text", "Exceptions"),
                                    New TaggedText("LineBreak", "\r\n"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Namespace", "System"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Namespace", "IO"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Class", "IOException")}))}))

            Dim view = New Mock(Of ITextView) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim snapshotPoint = New SnapshotPoint(view.Object.TextSnapshot, view.Object.Caret.Position.BufferPosition.Position)
            Dim intellisenseQuickInfo = IntellisenseQuickInfoBuilder.BuildItem(snapshotPoint, codeAnalysisQuickInfoItem)

            Assert.NotNull(intellisenseQuickInfo)

            Assert.IsType(Of Adornments.ContainerElement)(intellisenseQuickInfo.Item)
            Dim container = CType(intellisenseQuickInfo.Item, Adornments.ContainerElement)
            Assert.Equal(3, container.Elements.Count())
            Assert.Equal(ContainerElementStyle.Stacked, container.Style)


            Assert.IsType(Of Adornments.ContainerElement)(container.Elements.ElementAt(0))
            Dim firstRowContainer = CType(container.Elements.ElementAt(0), Adornments.ContainerElement)
            Assert.Equal(2, firstRowContainer.Elements.Count())
            Assert.Equal(ContainerElementStyle.Wrapped, firstRowContainer.Style)



            Assert.IsType(Of ImageElement)(firstRowContainer.Elements.ElementAt(0))
            Dim element00 = CType(firstRowContainer.Elements.ElementAt(0), ImageElement)
            Assert.Equal(KnownImageIds.ImageCatalogGuid, element00.ImageId.Guid)
            Assert.Equal(KnownImageIds.MethodPublic, element00.ImageId.Id)

            Assert.IsType(Of ClassifiedTextElement)(firstRowContainer.Elements.ElementAt(1))
            Dim element01 = CType(firstRowContainer.Elements.ElementAt(1), ClassifiedTextElement)
            Assert.Equal(18, element01.Runs.Count())

            Assert.IsType(Of ClassifiedTextElement)(container.Elements.ElementAt(1))
            Dim element1 = CType(container.Elements.ElementAt(1), ClassifiedTextElement)
            Assert.Equal(1, element1.Runs.Count())

            Assert.IsType(Of ClassifiedTextElement)(container.Elements.ElementAt(2))
            Dim element2 = CType(container.Elements.ElementAt(2), ClassifiedTextElement)
            Assert.Equal(8, element2.Runs.Count())

        End Sub

        Private Shared ReadOnly s_document As Document =
            (Function()
                 Dim workspace = TestWorkspace.CreateWorkspace(
                     <Workspace>
                         <Project Language="C#">
                             <Document>
                             </Document>
                         </Project>
                     </Workspace>)
                 Return workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id)
             End Function)()
        Private Shared ReadOnly s_bufferFactory As ITextBufferFactoryService = DirectCast(s_document.Project.Solution.Workspace, TestWorkspace).GetService(Of ITextBufferFactoryService)

    End Class
End Namespace

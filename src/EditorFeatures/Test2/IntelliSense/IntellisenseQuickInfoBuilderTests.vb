' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Adornments
Imports Moq
Imports QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class IntellisenseQuickInfoBuilderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Sub BuildQuickInfoItem()

            Dim codeAnalysisQuickInfoItem _
                    = QuickInfoItem.Create(New TextSpan(0, 0), ImmutableArray.Create({"Method", "Public"}),
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

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, Threading.CancellationToken.None)

            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of Adornments.ContainerElement)(intellisenseQuickInfo.Item)
            Assert.Equal(2, container.Elements.Count())
            Assert.Equal(ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding, container.Style)

            Assert.Collection(
                container.Elements,
                New Action(Of Object)() {
                    Sub(row0row1 As Object)
                        Dim row0row1container = Assert.IsAssignableFrom(Of ContainerElement)(row0row1)
                        Assert.Equal(ContainerElementStyle.Stacked, row0row1container.Style)
                        Assert.Collection(
                            row0row1container.Elements,
                            New Action(Of Object)() {
                                Sub(row0 As Object)
                                    Dim firstRowContainer = Assert.IsType(Of ContainerElement)(row0)
                                    Assert.Equal(2, firstRowContainer.Elements.Count())
                                    Assert.Equal(ContainerElementStyle.Wrapped, firstRowContainer.Style)

                                    Assert.Collection(firstRowContainer.Elements,
                                            New Action(Of Object)() {
                                            Sub(row0col0 As Object)
                                                Dim element00 = Assert.IsType(Of ImageElement)(row0col0)
                                                Assert.Equal(KnownImageIds.ImageCatalogGuid, element00.ImageId.Guid)
                                                Assert.Equal(KnownImageIds.MethodPublic, element00.ImageId.Id)
                                            End Sub,
                                            Sub(row0col1 As Object)
                                                Dim element01 = Assert.IsType(Of ClassifiedTextElement)(row0col1)
                                                Assert.Equal(18, element01.Runs.Count())
                                            End Sub})
                                End Sub,
                                Sub(row1 As Object)
                                    Dim element1 = Assert.IsType(Of ClassifiedTextElement)(row1)
                                    Assert.Equal(1, element1.Runs.Count())
                                End Sub
                            })
                    End Sub,
                    Sub(row2row3 As Object)
                        Dim row2row3container = Assert.IsAssignableFrom(Of ContainerElement)(row2row3)
                        Assert.Equal(ContainerElementStyle.Stacked, row2row3container.Style)
                        Assert.Collection(
                            row2row3container.Elements,
                            Sub(row2 As Object)
                                Dim element2 = Assert.IsType(Of ClassifiedTextElement)(row2)
                                Assert.Equal(1, element2.Runs.Count())
                            End Sub,
                            Sub(row3 As Object)
                                Dim element3 = Assert.IsType(Of ClassifiedTextElement)(row3)
                                Assert.Equal(6, element3.Runs.Count())
                            End Sub)
                    End Sub})

        End Sub

    End Class
End Namespace

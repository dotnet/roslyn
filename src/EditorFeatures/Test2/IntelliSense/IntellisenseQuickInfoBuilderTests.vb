' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Adornments
Imports Moq
Imports QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class IntellisenseQuickInfoBuilderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Sub BuildQuickInfoItem()

            Dim codeAnalysisQuickInfoItem =
                QuickInfoItem.Create(
                    New TextSpan(0, 0),
                    ImmutableArray.Create(WellKnownTags.Method, WellKnownTags.Public),
                    ImmutableArray.Create(
                        QuickInfoSection.Create(
                            QuickInfoSectionKinds.Description,
                            ImmutableArray.Create(
                                New TaggedText(TextTags.Keyword, "void"),
                                New TaggedText(TextTags.Space, " "),
                                New TaggedText(TextTags.Class, "Console"),
                                New TaggedText(TextTags.Punctuation, "."),
                                New TaggedText(TextTags.Method, "WriteLine"),
                                New TaggedText(TextTags.Punctuation, "("),
                                New TaggedText(TextTags.Keyword, "string"),
                                New TaggedText(TextTags.Space, " "),
                                New TaggedText(TextTags.Parameter, "value"),
                                New TaggedText(TextTags.Punctuation, ")"),
                                New TaggedText(TextTags.Space, " "),
                                New TaggedText(TextTags.Punctuation, "("),
                                New TaggedText(TextTags.Punctuation, "+"),
                                New TaggedText(TextTags.Space, " "),
                                New TaggedText(TextTags.Text, "18"),
                                New TaggedText(TextTags.Space, " "),
                                New TaggedText(TextTags.Text, "overloads"),
                                New TaggedText(TextTags.Punctuation, ")"))),
                        QuickInfoSection.Create(
                            QuickInfoSectionKinds.DocumentationComments,
                            ImmutableArray.Create(New TaggedText(TextTags.Text, "Writes the specified string value, followed by the current line terminator, to the standard output stream."))),
                        QuickInfoSection.Create(
                            QuickInfoSectionKinds.Exception,
                            ImmutableArray.Create(
                                New TaggedText(TextTags.Text, "Exceptions"),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.Space, " "),
                                New TaggedText(TextTags.Namespace, "System"),
                                New TaggedText(TextTags.Punctuation, "."),
                                New TaggedText(TextTags.Namespace, "IO"),
                                New TaggedText(TextTags.Punctuation, "."),
                                New TaggedText(TextTags.Class, "IOException")))))

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, Threading.CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "Console"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "WriteLine"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "string"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "value"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "+"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "18"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "overloads"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Writes the specified string value, followed by the current line terminator, to the standard output stream."))),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Exceptions")),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.NamespaceName, "System"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.NamespaceName, "IO"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.ClassName, "IOException"))))

            AssertEqualAdornments(expected, container)
        End Sub

        Private Shared Sub AssertEqualAdornments(expected As Object, actual As Object)
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
        End Sub
    End Class
End Namespace

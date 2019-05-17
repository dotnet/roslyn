' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
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

    <UseExportProvider>
    Public Class IntellisenseQuickInfoBuilderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Sub BuildQuickInfoItemWithoutDocumentation()

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

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Sub BuildQuickInfoItemWithMultiLineDocumentation()

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
                            ImmutableArray.Create(
                                New TaggedText(TextTags.Text, "Documentation line 1."),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.Text, "Documentation line 2."),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.Text, "Documentation paragraph 2."),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.Text, "Documentation paragraph 2 line 2."),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.LineBreak, "\r\n"),
                                New TaggedText(TextTags.Text, "Documentation paragraph 3."))),
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
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation line 1.")),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation line 2.")))),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation paragraph 2.")),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation paragraph 2 line 2."))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation paragraph 3.")),
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Sub BuildQuickInfoFromSymbol()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.IO;
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// Documentation line 1.&lt;br/&gt;
                                /// Documentation line 2.
                                /// &lt;para&gt;Documentation paragraph 2.&lt;br/&gt;
                                /// Documentation paragraph 2
                                /// line 2.&lt;/para&gt;
                                /// &lt;para&gt;Documentation paragraph 3.&lt;/para&gt;
                                /// &lt;/summary&gt;
                                /// &lt;exception cref="IOException"&gt;If something fails&lt;/exception&gt;
                                void MyMethod(CancellationToken cancellationToken = default(CancellationToken)) {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "["),
                            New ClassifiedTextRun(ClassificationTypeNames.StructName, "CancellationToken"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "cancellationToken"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "="),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "default"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "]"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation line 1.")),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation line 2.")))),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation paragraph 2.")),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation paragraph 2 line 2."))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "Documentation paragraph 3.")),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, FeaturesResources.Exceptions_colon)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, "  "),
                        New ClassifiedTextRun(ClassificationTypeNames.ClassName, "IOException"))))

            AssertEqualAdornments(expected, container)
        End Sub

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("<para>text1</para><para>text2</para>")>
        <InlineData("text1<br/><br/>text2")>
        <InlineData("text1<br/><br/><br/>text2")>
        <InlineData("<br/>text1<br/><br/>text2<br/>")>
        <InlineData("<br/><br/>text1<br/><br/>text2<br/><br/>")>
        <InlineData("<para>text1<br/><br/>text2</para>")>
        <InlineData("<para/>text1<br/><br/>text2<para/>")>
        <InlineData("<para/><para/>text1<br/><br/>text2<para/><para/>")>
        <InlineData("<para>text1</para><br/><para>text2</para>")>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Sub EquivalentParagraphForms(summary As String)
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.IO;
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// <%= summary %>
                                /// &lt;/summary&gt;
                                void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "text1"))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "text2")))

            AssertEqualAdornments(expected, container)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Sub QuickInfoForParameterReference()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// The parameter is &lt;paramref name="p"/&gt;.
                                /// &lt;/summary&gt;
                                void MyMethod(CancellationToken p) {
                                    MyM$$ethod(CancellationToken.None);
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.StructName, "CancellationToken"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "p"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "The parameter is"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "p"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            AssertEqualAdornments(expected, container)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Sub QuickInfoForReadOnlyMethodReference()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            struct MyStruct {
                                readonly void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "readonly"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.StructName, "MyStruct"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))))

            AssertEqualAdornments(expected, container)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Sub QuickInfoForReadOnlyPropertyReference()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            struct MyStruct {
                                readonly int MyProperty => My$$Property;
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPrivate)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "readonly"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.StructName, "MyStruct"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.PropertyName, "MyProperty"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "{"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "get"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ";"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "}"))))

            AssertEqualAdornments(expected, container)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Sub QuickInfoForReadOnlyEventReference()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            struct MyStruct {
                                readonly event System.Action MyEvent { add { My$$Event += value; } remove { } }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EventPrivate)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "readonly"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.NamespaceName, "System"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.DelegateName, "Action"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.StructName, "MyStruct"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.EventName, "MyEvent"))))

            AssertEqualAdornments(expected, container)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Sub QuickInfoForTypeParameterReference()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// The type parameter is &lt;typeparamref name="T"/&gt;.
                                /// &lt;/summary&gt;
                                void MyMethod&lt;T&gt;() {
                                    MyM$$ethod&lt;int&gt;();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim codeAnalysisQuickInfoItem = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

            Dim trackingSpan = New Mock(Of ITrackingSpan) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim intellisenseQuickInfo = Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, Nothing, Nothing, CancellationToken.None)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPrivate)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "<"),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ">"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "The type parameter is"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.TypeParameterName, "T"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            AssertEqualAdornments(expected, container)
        End Sub

        Private Async Function GetQuickInfoItemAsync(workspaceDefinition As XElement, language As String) As Task(Of QuickInfoItem)
            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim cursorBuffer = cursorDocument.TextBuffer

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim languageServiceProvider = workspace.Services.GetLanguageServices(language)
                Dim quickInfoService = languageServiceProvider.GetRequiredService(Of QuickInfoService)

                Return Await quickInfoService.GetQuickInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(False)
            End Using
        End Function

        Private Shared Sub AssertEqualAdornments(expected As Object, actual As Object)
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
                result.Append($"{classification}, ""{classifiedTextRun.Text.Replace("""", """""")}"")")
                Return
            End If

            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Sub

        Private Shared Function ContainerStyleToString(style As ContainerElementStyle) As String
            Dim stringValue = style.ToString()
            Return String.Join(" Or ", stringValue.Split({","c, " "c}, StringSplitOptions.RemoveEmptyEntries).Select(Function(value) $"{NameOf(ContainerElementStyle)}.{value}"))
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

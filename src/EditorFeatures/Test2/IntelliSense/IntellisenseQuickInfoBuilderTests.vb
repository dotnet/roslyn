' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Test.Utilities.QuickInfo
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text.Adornments
Imports QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class IntellisenseQuickInfoBuilderTests
        Inherits AbstractIntellisenseQuickInfoBuilderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Function BuildQuickInfoItem() As Task

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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(codeAnalysisQuickInfoItem)
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

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Function BuildQuickInfoItemWithoutDocumentation() As Task

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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(codeAnalysisQuickInfoItem)
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

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Function BuildQuickInfoItemWithMultiLineDocumentation() As Task

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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(codeAnalysisQuickInfoItem)
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

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33001, "https://github.com/dotnet/roslyn/issues/33001")>
        Public Async Function BuildQuickInfoFromSymbol() As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "void MyClass.MyMethod(CancellationToken cancellationToken = default(CancellationToken))"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "["),
                            New ClassifiedTextRun(ClassificationTypeNames.StructName, "CancellationToken", navigationAction:=Sub() Return, "CancellationToken"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "cancellationToken", navigationAction:=Sub() Return, "CancellationToken cancellationToken = default(CancellationToken)"),
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
                        New ClassifiedTextRun(ClassificationTypeNames.ClassName, "IOException", navigationAction:=Sub() Return, "IOException"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(31618, "https://github.com/dotnet/roslyn/issues/31618")>
        Public Async Function QuickInfoShowsMethodRemarks() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class MyClass {
                                /// &lt;summary&gt;
                                /// Summary text.
                                /// &lt;/summary&gt;
                                /// &lt;remarks&gt;
                                /// Remarks text.
                                /// &lt;/remarks&gt;
                                int Me$$thod() => throw null;
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "Method", navigationAction:=Sub() Return, "int MyClass.Method()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Summary text."))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "Remarks text.")))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(31618, "https://github.com/dotnet/roslyn/issues/31618")>
        Public Async Function QuickInfoShowsMethodReturns() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class MyClass {
                                /// &lt;summary&gt;
                                /// Summary text.
                                /// &lt;/summary&gt;
                                /// &lt;returns&gt;
                                /// Returns text.
                                /// &lt;/returns&gt;
                                int Me$$thod() => throw null;
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "Method", navigationAction:=Sub() Return, "int MyClass.Method()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Summary text."))),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, FeaturesResources.Returns_colon)),
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "  ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Returns text."))))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(31618, "https://github.com/dotnet/roslyn/issues/31618")>
        Public Async Function QuickInfoShowsDelegateReturns() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class MyClass {
                                /// &lt;summary&gt;
                                /// Summary text.
                                /// &lt;/summary&gt;
                                /// &lt;returns&gt;
                                /// Returns text.
                                /// &lt;/returns&gt;
                                delegate int Me$$thod();
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.DelegatePrivate)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "delegate"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.DelegateName, "Method", navigationAction:=Sub() Return, "Method"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Summary text."))),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, FeaturesResources.Returns_colon)),
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "  ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Returns text."))))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(31618, "https://github.com/dotnet/roslyn/issues/31618")>
        Public Async Function QuickInfoShowsPropertyValue() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class MyClass {
                                /// &lt;summary&gt;
                                /// Summary text.
                                /// &lt;/summary&gt;
                                /// &lt;value&gt;
                                /// Value text.
                                /// &lt;/value&gt;
                                int Pr$$operty { get; }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.PropertyPrivate)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.PropertyName, "Property", navigationAction:=Sub() Return, "int MyClass.Property"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "{"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "get"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ";"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "}"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Summary text."))),
                New ContainerElement(
                    ContainerElementStyle.Stacked,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, FeaturesResources.Value_colon)),
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "  ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Value text."))))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

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
        Public Async Function EquivalentParagraphForms(summary As String) As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "void MyClass.MyMethod()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "text1"))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "text2")))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function InlineCodeElement() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.IO;
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// This method returns &lt;c&gt;true&lt;/c&gt;.
                                /// &lt;/summary&gt;
                                bool MyMethod() {
                                    return MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "bool", navigationAction:=Sub() Return, "bool"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "bool MyClass.MyMethod()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This method returns"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "true", ClassifiedTextRunStyle.UseClassificationFont),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function BlockLevelCodeElement() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.IO;
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// This method returns &lt;code&gt;true&lt;/code&gt;.
                                /// &lt;/summary&gt;
                                bool MyMethod() {
                                    return MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "bool", navigationAction:=Sub() Return, "bool"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "bool MyClass.MyMethod()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This method returns"))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "true", ClassifiedTextRunStyle.UseClassificationFont)),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, ".")))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForParameterReference() As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "void MyClass.MyMethod(CancellationToken p)"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.StructName, "CancellationToken", navigationAction:=Sub() Return, "CancellationToken"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "p", navigationAction:=Sub() Return, "CancellationToken p"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "The parameter is"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "p"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForReadOnlyMethodReference() As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                        New ClassifiedTextRun(ClassificationTypeNames.StructName, "MyStruct", navigationAction:=Sub() Return, "MyStruct"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "readonly void MyStruct.MyMethod()"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForReadOnlyPropertyReference() As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.StructName, "MyStruct", navigationAction:=Sub() Return, "MyStruct"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.PropertyName, "MyProperty", navigationAction:=Sub() Return, "readonly int MyStruct.MyProperty"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "{"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "get"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ";"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "}"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForReadOnlyEventReference() As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                        New ClassifiedTextRun(ClassificationTypeNames.DelegateName, "Action", navigationAction:=Sub() Return, "Action"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.StructName, "MyStruct", navigationAction:=Sub() Return, "MyStruct"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.EventName, "MyEvent", navigationAction:=Sub() Return, "readonly event Action MyStruct.MyEvent"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForTypeParameterReference() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// The type parameter is &lt;typeparamref name="T"/&gt;.
                                /// &lt;/summary&gt;
                                void MyM$$ethod&lt;T&gt;() {
                                    MyMethod&lt;int&gt;();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "void MyClass.MyMethod<T>()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "<"),
                            New ClassifiedTextRun(ClassificationTypeNames.TypeParameterName, "T", navigationAction:=Sub() Return, "T"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ">"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "The type parameter is"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.TypeParameterName, "T", navigationAction:=Sub() Return, "T"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForTypeParameterReferenceClosedGeneric() As Task
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

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "void MyClass.MyMethod<int>()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "<"),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ">"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "The type parameter is"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(33546, "https://github.com/dotnet/roslyn/issues/33546")>
        Public Async Function QuickInfoForTypeParameterReferenceBoundGeneric() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass&lt;K&gt; {
                                /// &lt;summary&gt;
                                /// The type parameter is &lt;typeparamref name="T"/&gt;.
                                /// &lt;/summary&gt;
                                void MyMethod&lt;T&gt;() {
                                    MyM$$ethod&lt;K&gt;();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
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
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass<K>"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "<"),
                            New ClassifiedTextRun(ClassificationTypeNames.TypeParameterName, "K", navigationAction:=Sub() Return, "K"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ">"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "MyMethod", navigationAction:=Sub() Return, "void MyClass<K>.MyMethod<K>()"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "<"),
                            New ClassifiedTextRun(ClassificationTypeNames.TypeParameterName, "K", navigationAction:=Sub() Return, "K"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ">"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "The type parameter is"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.TypeParameterName, "K", navigationAction:=Sub() Return, "K"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(46985, "https://github.com/dotnet/roslyn/issues/46985")>
        Public Async Function QuickInfoForRecords() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public sealed record TestRecord(int X, int Y) { }

                            class C
                            {
                                void M()
                                {
                                    var x = new Test$$Record(1, 2);
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.RecordClassName, "TestRecord", navigationAction:=Sub() Return, "TestRecord"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.RecordClassName, "TestRecord", navigationAction:=Sub() Return, "TestRecord.TestRecord(int X, int Y)"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "X", navigationAction:=Sub() Return, "int X"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ","),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "Y", navigationAction:=Sub() Return, "int Y"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(52490, "https://github.com/dotnet/roslyn/issues/52490")>
        Public Async Function QuickInfoForUnderlyingEnumTypes() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public enum E$$ : byte { A, B }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.EnumerationPublic)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "enum"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.EnumName, "E", navigationAction:=Sub() Return, "E"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "byte", navigationAction:=Sub() Return, "byte"))))
            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function QuickInfoForRecordClass() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public sealed record class TestRecord(int X, int Y) { }

                            class C
                            {
                                void M()
                                {
                                    var x = new Test$$Record(1, 2);
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.RecordClassName, "TestRecord", navigationAction:=Sub() Return, "TestRecord"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.RecordClassName, "TestRecord", navigationAction:=Sub() Return, "TestRecord.TestRecord(int X, int Y)"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "X", navigationAction:=Sub() Return, "int X"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ","),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "Y", navigationAction:=Sub() Return, "int Y"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function QuickInfoForRecordStructs() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            public sealed record struct TestRecord(int X, int Y) { }

                            class C
                            {
                                void M()
                                {
                                    var x = new Test$$Record(1, 2);
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)
            Assert.NotNull(intellisenseQuickInfo)

            Dim container = Assert.IsType(Of ContainerElement)(intellisenseQuickInfo.Item)

            Dim expected = New ContainerElement(
                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodPublic)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.RecordStructName, "TestRecord", navigationAction:=Sub() Return, "TestRecord"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                        New ClassifiedTextRun(ClassificationTypeNames.RecordStructName, "TestRecord", navigationAction:=Sub() Return, "TestRecord.TestRecord(int X, int Y)"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "X", navigationAction:=Sub() Return, "int X"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ","),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "Y", navigationAction:=Sub() Return, "int Y"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "+"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "1"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, FeaturesResources.overload),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function
    End Class
End Namespace

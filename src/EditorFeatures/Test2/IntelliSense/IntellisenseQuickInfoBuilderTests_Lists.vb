﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Test.Utilities.QuickInfo
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text.Adornments

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class IntellisenseQuickInfoBuilderTests_Lists
        Inherits AbstractIntellisenseQuickInfoBuilderTests

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData(New Object() {New String() {"item", "description"}})>
        <InlineData(New Object() {New String() {"item"}})>
        Public Async Function QuickInfoForBulletedList(itemTags As String()) As Task
            Dim openItemTag = String.Join("", itemTags.Select(Function(tag) $"<{tag}>"))
            Dim closeItemTag = String.Join("", itemTags.Reverse().Select(Function(tag) $"</{tag}>"))
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// &lt;list type="bullet"&gt;
                                /// <%= openItemTag %>Item 1<%= closeItemTag %>
                                /// <%= openItemTag %>Item 2<%= closeItemTag %>
                                /// &lt;/list&gt;
                                /// &lt;/summary&gt;
                                void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

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
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "• ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 1"))))),
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "• ")),
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 2")))))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData(New Object() {New String() {"item", "description"}})>
        <InlineData(New Object() {New String() {"item"}})>
        Public Async Function QuickInfoForNumberedList(itemTags As String()) As Task
            Dim openItemTag = String.Join("", itemTags.Select(Function(tag) $"<{tag}>"))
            Dim closeItemTag = String.Join("", itemTags.Reverse().Select(Function(tag) $"</{tag}>"))
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// &lt;list type="number"&gt;
                                /// <%= openItemTag %>Item 1<%= closeItemTag %>
                                /// <%= openItemTag %>Item 2<%= closeItemTag %>
                                /// &lt;/list&gt;
                                /// &lt;/summary&gt;
                                void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

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
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "1. ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 1"))))),
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "2. ")),
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 2")))))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function QuickInfoForBulletedTermList() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// &lt;list type="bullet"&gt;
                                /// &lt;item&gt;&lt;term&gt;word1&lt;/term&gt;&lt;description&gt;Item 1&lt;/description&gt;&lt;/item&gt;
                                /// &lt;item&gt;&lt;term&gt;word2&lt;/term&gt;&lt;description&gt;Item 2&lt;/description&gt;&lt;/item&gt;
                                /// &lt;/list&gt;
                                /// &lt;/summary&gt;
                                void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

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
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "• ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "word1", ClassifiedTextRunStyle.Bold),
                                New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "–"),
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 1"))))),
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "• ")),
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "word2", ClassifiedTextRunStyle.Bold),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "–"),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 2")))))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function QuickInfoForNumberedTermList() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// &lt;list type="number"&gt;
                                /// &lt;item&gt;&lt;term&gt;word1&lt;/term&gt;&lt;description&gt;Item 1&lt;/description&gt;&lt;/item&gt;
                                /// &lt;item&gt;&lt;term&gt;word2&lt;/term&gt;&lt;description&gt;Item 2&lt;/description&gt;&lt;/item&gt;
                                /// &lt;/list&gt;
                                /// &lt;/summary&gt;
                                void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

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
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "1. ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "word1", ClassifiedTextRunStyle.Bold),
                                New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "–"),
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 1"))))),
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "2. ")),
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "word2", ClassifiedTextRunStyle.Bold),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "–"),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 2")))))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData(New Object() {New String() {"item", "description"}})>
        <InlineData(New Object() {New String() {"item"}})>
        Public Async Function QuickInfoForNestedLists(itemTags As String()) As Task
            Dim openItemTag = String.Join("", itemTags.Select(Function(tag) $"<{tag}>"))
            Dim closeItemTag = String.Join("", itemTags.Reverse().Select(Function(tag) $"</{tag}>"))
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// &lt;list type="number"&gt;
                                /// <%= openItemTag %>
                                /// &lt;list type="bullet"&gt;
                                /// <%= openItemTag %>&lt;para&gt;Line1&lt;/para&gt;&lt;para&gt;Line2&lt;/para&gt;<%= closeItemTag %>
                                /// <%= openItemTag %>Item 1.2<%= closeItemTag %>
                                /// &lt;/list&gt;
                                /// <%= closeItemTag %>
                                /// <%= openItemTag %>
                                /// &lt;list type="number"&gt;
                                /// <%= openItemTag %>Item 2.1<%= closeItemTag %>
                                /// <%= openItemTag %>&lt;para&gt;Line1&lt;/para&gt;&lt;para&gt;Line2&lt;/para&gt;<%= closeItemTag %>
                                /// &lt;/list&gt;
                                /// <%= closeItemTag %>
                                /// &lt;/list&gt;
                                /// &lt;/summary&gt;
                                void MyMethod() {
                                    MyM$$ethod();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim intellisenseQuickInfo = Await GetQuickInfoItemAsync(workspace, LanguageNames.CSharp)

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
                    New ContainerElement(
                        ContainerElementStyle.Wrapped,
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Text, "1. ")),
                        New ContainerElement(
                            ContainerElementStyle.Stacked,
                            New ContainerElement(
                                ContainerElementStyle.Wrapped,
                                New ClassifiedTextElement(
                                    New ClassifiedTextRun(ClassificationTypeNames.Text, "• ")),
                                New ContainerElement(
                                    ContainerElementStyle.Stacked,
                                    New ClassifiedTextElement(
                                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Line1")),
                                    New ContainerElement(
                                        ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                                        New ClassifiedTextElement(
                                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Line2"))))),
                            New ContainerElement(
                                ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                                New ContainerElement(
                                    ContainerElementStyle.Wrapped,
                                    New ClassifiedTextElement(
                                        New ClassifiedTextRun(ClassificationTypeNames.Text, "• ")),
                                    New ContainerElement(
                                        ContainerElementStyle.Stacked,
                                        New ClassifiedTextElement(
                                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 1.2")))))))),
                New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "2. ")),
                    New ContainerElement(
                        ContainerElementStyle.Stacked,
                        New ContainerElement(
                            ContainerElementStyle.Wrapped,
                            New ClassifiedTextElement(
                                New ClassifiedTextRun(ClassificationTypeNames.Text, "1. ")),
                            New ContainerElement(
                                ContainerElementStyle.Stacked,
                                New ClassifiedTextElement(
                                    New ClassifiedTextRun(ClassificationTypeNames.Text, "Item 2.1")))),
                        New ContainerElement(
                            ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                            New ContainerElement(
                                ContainerElementStyle.Wrapped,
                                New ClassifiedTextElement(
                                    New ClassifiedTextRun(ClassificationTypeNames.Text, "2. ")),
                                New ContainerElement(
                                    ContainerElementStyle.Stacked,
                                    New ClassifiedTextElement(
                                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Line1")),
                                    New ContainerElement(
                                        ContainerElementStyle.Stacked Or ContainerElementStyle.VerticalPadding,
                                        New ClassifiedTextElement(
                                            New ClassifiedTextRun(ClassificationTypeNames.Text, "Line2")))))))))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function
    End Class
End Namespace

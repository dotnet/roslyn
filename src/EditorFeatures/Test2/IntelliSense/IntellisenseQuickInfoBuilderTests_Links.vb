' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text.Adornments

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class IntellisenseQuickInfoBuilderTests_Links
        Inherits AbstractIntellisenseQuickInfoBuilderTests

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("see")>
        <InlineData("a")>
        Public Async Sub QuickInfoForPlainHyperlink(tag As String)
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// This contains a link to &lt;<%= tag %> href="https://github.com/dotnet/roslyn"/&gt;.
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
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This contains a link to"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "https://github.com/dotnet/roslyn", navigationAction:=Sub() Return, "https://github.com/dotnet/roslyn"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            AssertEqualAdornments(expected, intellisenseQuickInfo.Item)
        End Sub

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData("see")>
        <InlineData("a")>
        Public Async Sub QuickInfoForHyperlinkWithText(tag As String)
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// This contains a link to &lt;<%= tag %> href="https://github.com/dotnet/roslyn"&gt;dotnet/roslyn&lt;/<%= tag %>&gt;.
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
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This contains a link to"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "dotnet/roslyn", navigationAction:=Sub() Return, "https://github.com/dotnet/roslyn"),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            AssertEqualAdornments(expected, intellisenseQuickInfo.Item)
        End Sub
    End Class
End Namespace

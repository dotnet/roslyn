' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Test.Utilities.QuickInfo
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text.Adornments

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class IntellisenseQuickInfoBuilderTests_Styles
        Inherits AbstractIntellisenseQuickInfoBuilderTests

        <WpfTheory, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <InlineData({"em"}, ClassifiedTextRunStyle.Italic)>
        <InlineData({"i"}, ClassifiedTextRunStyle.Italic)>
        <InlineData({"strong"}, ClassifiedTextRunStyle.Bold)>
        <InlineData({"b"}, ClassifiedTextRunStyle.Bold)>
        <InlineData({"u"}, ClassifiedTextRunStyle.Underline)>
        <InlineData({"c"}, ClassifiedTextRunStyle.UseClassificationFont)>
        <InlineData({"tt"}, ClassifiedTextRunStyle.UseClassificationFont)>
        <InlineData({"em", "strong"}, ClassifiedTextRunStyle.Italic Or ClassifiedTextRunStyle.Bold)>
        <InlineData({"b", "i"}, ClassifiedTextRunStyle.Italic Or ClassifiedTextRunStyle.Bold)>
        <InlineData({"i", "strong", "c", "u"}, ClassifiedTextRunStyle.Bold Or ClassifiedTextRunStyle.Italic Or ClassifiedTextRunStyle.Underline Or ClassifiedTextRunStyle.UseClassificationFont)>
        Public Async Function QuickInfoForStylizedText(styleTags As String(), style As ClassifiedTextRunStyle) As Task
            Dim openStyleTag = String.Join("", styleTags.Select(Function(tag) $"<{tag}>"))
            Dim closeStyleTag = String.Join("", styleTags.Reverse().Select(Function(tag) $"</{tag}>"))
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;
                            class MyClass {
                                /// &lt;summary&gt;
                                /// This is some <%= openStyleTag %>stylized text<%= closeStyleTag %>.
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
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This is some"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "stylized text", style),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "."))))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function
    End Class
End Namespace

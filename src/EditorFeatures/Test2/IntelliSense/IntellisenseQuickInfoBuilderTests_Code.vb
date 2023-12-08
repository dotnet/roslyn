' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Test.Utilities.QuickInfo
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text.Adornments

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <Trait(Traits.Feature, Traits.Features.QuickInfo)>
    Public Class IntellisenseQuickInfoBuilderTests_Code
        Inherits AbstractIntellisenseQuickInfoBuilderTests
        <WpfFact>
        Public Async Function QuickInfoForXmlCodeElementWithCDATA() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class MyClass {
                                /// &lt;summary&gt;
                                /// summary for MyClass
                                /// &lt;code&gt;&lt;![CDATA[
                                /// List&lt;string&gt; y = null;
                                /// ]]&gt;&lt;/code&gt;
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
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "summary for MyClass"))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "List<string> y = null;", ClassifiedTextRunStyle.UseClassificationFont)))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function

        <WpfFact>
        Public Async Function QuickInfoForXmlCodeElement() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class MyClass {
                                /// &lt;summary&gt;
                                /// Normalize    this, &lt;c&gt;and             also
                                /// this&lt;/c&gt;
                                /// &lt;code&gt;
                                /// line 1
                                /// line     2
                                /// &lt;/code&gt;
                                /// Extra text after code.
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
                       New ClassifiedTextRun(ClassificationTypeNames.Text, "Normalize this,"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "and also this", ClassifiedTextRunStyle.UseClassificationFont))),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, $"line 1{vbCrLf}line     2", ClassifiedTextRunStyle.UseClassificationFont)),
                New ClassifiedTextElement(
                    New ClassifiedTextRun(ClassificationTypeNames.Text, "Extra text after code.")))

            ToolTipAssert.EqualContent(expected, intellisenseQuickInfo.Item)
        End Function
    End Class
End Namespace

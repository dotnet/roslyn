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
    Public Class IntellisenseQuickInfoBuilderTests_Inheritdoc
        Inherits AbstractIntellisenseQuickInfoBuilderTests

        <WpfFact>
        Public Async Function NoImplicitInheritedQuickInfoForType() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;

                            /// &lt;summary&gt;
                            /// This is the base class.
                            /// &lt;/summary&gt;
                            class BaseClass
                            {
                            }

                            class My$$Class : BaseClass {
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
                    New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal)),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Keyword, "class"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact>
        Public Async Function ExplicitInheritedQuickInfoForType() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;

                            /// &lt;summary&gt;
                            /// This is the base class.
                            /// &lt;/summary&gt;
                            class BaseClass
                            {
                            }

                            /// &lt;inheritdoc cref="BaseClass"/&gt;
                            class My$$Class {
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
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "class"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This is the base class."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact>
        Public Async Function ExplicitInheritedQuickInfoForSummary1() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;

                            /// &lt;summary&gt;
                            /// This is the base class.
                            /// &lt;/summary&gt;
                            class BaseClass
                            {
                            }

                            /// &lt;summary&gt;
                            /// &lt;inheritdoc cref="BaseClass"/&gt;
                            /// &lt;/summary&gt;
                            class My$$Class {
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
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "class"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This is the base class."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact>
        Public Async Function ExplicitInheritedQuickInfoForSummary2() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;

                            /// &lt;summary&gt;
                            /// This is the base class.
                            /// &lt;/summary&gt;
                            class BaseClass
                            {
                            }

                            /// &lt;summary&gt;
                            /// This is not the base class.
                            /// &lt;/summary&gt;
                            class NotBaseClass
                            {
                            }

                            /// &lt;summary&gt;
                            /// &lt;inheritdoc cref="BaseClass"/&gt;
                            /// &lt;inheritdoc cref="NotBaseClass"/&gt;
                            /// &lt;/summary&gt;
                            class My$$Class {
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
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.ClassInternal)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "class"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "This is the base class. This is not the base class."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact>
        Public Async Function InheritedQuickInfoForParameterButNotSummary1() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;

                            abstract class BaseClass
                            {
                                /// &lt;summary&gt;
                                /// Base summary.
                                /// &lt;/summary&gt;
                                /// &lt;param name="x"&gt;A parameter.&lt;/param&gt;
                                protected abstract void Method(int x);
                            }

                            /// &lt;inheritdoc cref="BaseClass"/&gt;
                            class MyClass : BaseClass {
                                /// &lt;summary&gt;
                                /// Override summary.
                                /// &lt;/summary&gt;
                                /// &lt;inheritdoc/&gt;
                                protected override void Met$$hod(int x) { }
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
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.MethodProtected)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "void"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ClassName, "MyClass", navigationAction:=Sub() Return, "MyClass"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "."),
                            New ClassifiedTextRun(ClassificationTypeNames.MethodName, "Method", navigationAction:=Sub() Return, "void MyClass.Method(int x)"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "x", navigationAction:=Sub() Return, "int x"),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "Override summary."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function

        <WpfFact>
        Public Async Function InheritedQuickInfoForParameterButNotSummary2() As Task
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System.Threading;

                            abstract class BaseClass
                            {
                                /// &lt;summary&gt;
                                /// Base summary.
                                /// &lt;/summary&gt;
                                /// &lt;param name="x"&gt;A parameter.&lt;/param&gt;
                                protected abstract void Method(int x);
                            }

                            /// &lt;inheritdoc cref="BaseClass"/&gt;
                            class MyClass : BaseClass {
                                /// &lt;summary&gt;
                                /// Override summary.
                                /// &lt;/summary&gt;
                                /// &lt;inheritdoc/&gt;
                                protected override void Method(int $$x) { }
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
                        New ImageElement(New ImageId(KnownImageIds.ImageCatalogGuid, KnownImageIds.LocalVariable)),
                        New ClassifiedTextElement(
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, "("),
                            New ClassifiedTextRun(ClassificationTypeNames.Text, FeaturesResources.parameter),
                            New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ")"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.Keyword, "int", navigationAction:=Sub() Return, "int"),
                            New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                            New ClassifiedTextRun(ClassificationTypeNames.ParameterName, "x", navigationAction:=Sub() Return, "int x"))),
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "A parameter."))))

            ToolTipAssert.EqualContent(expected, container)
        End Function
    End Class
End Namespace

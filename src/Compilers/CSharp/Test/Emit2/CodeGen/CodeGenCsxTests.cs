// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    /// <summary>
    /// End-to-end tests for the CSX (C# JSX-like) feature:
    ///   parse → bind → lower → emit.
    /// </summary>
    public class CodeGenCsxTests : CSharpTestBase
    {
        // ---------------------------------------------------------------------------
        // Shared CSX runtime library source that satisfies the factory contract.
        // Authors embed this in their own library; here we inline it for tests.
        // ---------------------------------------------------------------------------
        private const string CsxRuntimeSource = """
            #nullable enable
            namespace CsxRuntime
            {
                public static class H
                {
                    public static class CSX
                    {
                        public interface Element { }

                        private sealed class TextElement : Element
                        {
                            public string Text;
                            public TextElement(string text) { Text = text; }
                        }

                        private sealed class NodeElement : Element
                        {
                            public string Tag;
                            public object Props;
                            public Element?[] Children;
                            public NodeElement(string tag, object props, Element?[] children)
                            {
                                Tag = tag; Props = props; Children = children;
                            }
                        }

                    public static Element CreateTextNode(string text) => new TextElement(text);
                }

                    public static CSX.Element CreateElement<TProps>(
                        System.Func<TProps, CSX.Element?[], CSX.Element> component,
                        TProps props,
                        params CSX.Element?[] children)
                    {
                        return component(props, children);
                    }
                }
            }
            """;

        // ---------------------------------------------------------------------------
        // Helper: build CSharpParseOptions with CSX enabled (Preview + factory set).
        // ---------------------------------------------------------------------------
        private static CSharpParseOptions CsxParseOptions(string factory = "CsxRuntime.H")
            => TestOptions.RegularPreview.WithCsxFactory(factory);

        // ---------------------------------------------------------------------------
        // Tests
        // ---------------------------------------------------------------------------

        [Fact]
        public void SelfClosingElement_NoProps_Compiles()
        {
            // <Button /> with an empty props record should compile without errors.
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record ButtonProps();

                public static class Button
                {
                    public static H.CSX.Element Render(ButtonProps props, H.CSX.Element?[] children)
                        => H.CSX.CreateTextNode("button");
                }

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element el = <Button />;
                        Console.WriteLine(el is not null ? "ok" : "null");
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "ok");
        }

        [Fact]
        public void SelfClosingElement_WithStringProp_Compiles()
        {
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record LinkProps(string Href);

                public static class Link
                {
                    public static H.CSX.Element Render(LinkProps props, H.CSX.Element?[] children)
                        => H.CSX.CreateTextNode(props.Href);
                }

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element el = <Link Href="https://example.com" />;
                        Console.WriteLine("ok");
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ok");
        }

        [Fact]
        public void ElementWithTextChild_Compiles()
        {
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record DivProps();

                public static class Div
                {
                    public static H.CSX.Element Render(DivProps props, H.CSX.Element?[] children)
                        => H.CSX.CreateTextNode("div");
                }

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element el = <Div>Hello world</Div>;
                        Console.WriteLine("ok");
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ok");
        }

        [Fact]
        public void NestedElements_Compile()
        {
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record ContainerProps();
                public record ButtonProps(string Label);

                public static class Container
                {
                    public static H.CSX.Element Render(ContainerProps props, H.CSX.Element?[] children)
                        => H.CSX.CreateTextNode("container");
                }

                public static class Button
                {
                    public static H.CSX.Element Render(ButtonProps props, H.CSX.Element?[] children)
                        => H.CSX.CreateTextNode(props.Label);
                }

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element el = <Container><Button Label="click me" /></Container>;
                        Console.WriteLine("ok");
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ok");
        }

        [Fact]
        public void CsxDisabledWithoutFactory_NoDiagnostic()
        {
            // Without a factory set, '<' should not be parsed as CSX.
            // The expression `x < y` should still work normally.
            var source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        int x = 1, y = 2;
                        bool result = x < y;
                        Console.WriteLine(result);
                    }
                }
                """;

            // Default parse options: no CSX factory.
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "True");
        }

        [Fact]
        public void MissingFactory_ReportsError()
        {
            var source = """
                #nullable enable
                using CsxRuntime;
                public record BtnProps();
                public static class Btn
                {
                    public static H.CSX.Element Render(BtnProps p, H.CSX.Element?[] children) => H.CSX.CreateTextNode("x");
                }
                class Program
                {
                    static void Main()
                    {
                        var el = <Btn />;
                    }
                }
                """;

            // Point at a non-existent factory type.
            var parseOptions = TestOptions.RegularPreview.WithCsxFactory("NonExistent.Factory");

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: parseOptions,
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            // Should get ERR_CsxFactoryTypeNotFound (9380)
            comp.VerifyDiagnostics(
                Diagnostic((ErrorCode)9380, "<Btn />").WithArguments("NonExistent.Factory").WithLocation(12, 18));
        }

        [Fact]
        public void RenderWithoutChildrenParam_ReportsError()
        {
            // A Render method missing the CSX.Element?[] children parameter is invalid.
            var source = """
                using CsxRuntime;
                public record BtnProps();
                public static class Btn
                {
                    public static H.CSX.Element Render(BtnProps p) => H.CSX.CreateTextNode("x");
                }
                class Program
                {
                    static void Main()
                    {
                        var el = <Btn />;
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            // Should get ERR_CsxRenderMethodInvalidSignature (9391)
            comp.VerifyDiagnostics(
                Diagnostic((ErrorCode)9391, "Btn").WithArguments(
                    "Btn.Render(BtnProps)",
                    "CsxRuntime.H.CSX.Element").WithLocation(11, 19));
        }

        [Fact]
        public void OptionalProp_NotSupplied_UsesDefault()
        {
            // A component with an optional nullable prop should compile and run correctly
            // when the optional prop is not supplied — default value is used.
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record CardProps(string Title, string? Subtitle = null);

                public static class Card
                {
                    public static H.CSX.Element Render(CardProps props, H.CSX.Element?[] children)
                        => H.CSX.CreateTextNode(props.Subtitle ?? "no-subtitle");
                }

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element el = <Card Title="Hello" />;
                        Console.WriteLine(el is not null ? "ok" : "null");
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ok");
        }

        [Fact]
        public void OptionalProp_ActionDelegate_NotSupplied_UsesDefault()
        {
            // A component with an optional Action? prop should compile and run correctly
            // when the action is not supplied — the null default is used without crashing.
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record ButtonProps(string Label, Action? OnClick = null);

                public static class Button
                {
                    public static H.CSX.Element Render(ButtonProps props, H.CSX.Element?[] children)
                    {
                        props.OnClick?.Invoke();
                        return H.CSX.CreateTextNode(props.Label);
                    }
                }

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element el = <Button Label="click me" />;
                        Console.WriteLine(el is not null ? "ok" : "null");
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, CsxRuntimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "ok");
        }

        [Fact]
        public void ViewAndText_RenderToHtml()
        {
            // Verifies end-to-end: CSX tree built with View + Text primitives,
            // passed through H.CreateElement, and walked by Renderer.RenderToHtml.
            var source = """
                using System;
                using CsxRuntime;

                class Program
                {
                    static void Main()
                    {
                        H.CSX.Element app =
                            <View>
                                <View Row>
                                    <Text Content="Left" />
                                    <Text Content="Right" />
                                </View>
                                <Text Content="Below" />
                            </View>;

                        string html = Renderer.RenderToFragment(app);
                        Console.WriteLine(html);
                    }
                }
                """;

            // Use the full CsxHtmlRuntime source (from the standalone file).
            var runtimeSource = """
                #nullable enable
                namespace CsxRuntime
                {
                    public static class H
                    {
                        public static class CSX
                        {
                            public interface Element { }
                            internal sealed class TextElement : Element
                            {
                                internal readonly string Text;
                                internal TextElement(string text) { Text = text; }
                            }
                            internal sealed class NodeElement : Element
                            {
                                internal readonly object Props;
                                internal readonly Element?[] Children;
                                internal NodeElement(object props, Element?[] children)
                                {
                                    Props = props; Children = children;
                                }
                            }
                            public static Element CreateTextNode(string text) => new TextElement(text);
                        }
                        public static CSX.Element CreateElement<TProps>(
                            System.Func<TProps, CSX.Element?[], CSX.Element> component,
                            TProps props,
                            params CSX.Element?[] children)
                            => component(props, children);
                    }

                    public static class View
                    {
                        public record ViewProps(bool Row = false);
                        public static H.CSX.Element Render(ViewProps props, H.CSX.Element?[] children)
                            => new H.CSX.NodeElement(props, children);
                    }

                    public static class Text
                    {
                        public record TextProps(string Content);
                        public static H.CSX.Element Render(TextProps props, H.CSX.Element?[] children)
                            => H.CSX.CreateTextNode(props.Content);
                    }

                    public static class Renderer
                    {
                        public static string RenderToFragment(H.CSX.Element root)
                        {
                            var sb = new System.Text.StringBuilder();
                            RenderElement(root, sb);
                            return sb.ToString();
                        }
                        private static void RenderElement(H.CSX.Element el, System.Text.StringBuilder sb)
                        {
                            switch (el)
                            {
                                case H.CSX.TextElement t:
                                    sb.Append(t.Text);
                                    break;
                                case H.CSX.NodeElement n:
                                    RenderNode(n, sb);
                                    break;
                            }
                        }
                        private static void RenderNode(H.CSX.NodeElement node, System.Text.StringBuilder sb)
                        {
                            switch (node.Props)
                            {
                                case View.ViewProps v:
                                    var dir = v.Row ? "row" : "column";
                                    sb.Append($"<div style=\"display:flex;flex-direction:{dir}\">");
                                    foreach (var c in node.Children) if (c != null) RenderElement(c, sb);
                                    sb.Append("</div>");
                                    break;
                                default:
                                    sb.Append("<div>");
                                    foreach (var c in node.Children) if (c != null) RenderElement(c, sb);
                                    sb.Append("</div>");
                                    break;
                            }
                        }
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, runtimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();

            var expected =
                "<div style=\"display:flex;flex-direction:column\">" +
                    "<div style=\"display:flex;flex-direction:row\">" +
                        "Left" +
                        "Right" +
                    "</div>" +
                    "Below" +
                "</div>";

            CompileAndVerify(comp, expectedOutput: expected);
        }

        [Fact]
        public void ExpressionChild_WithSurroundingText_PreservesSpaces()
        {
            // Verifies that inline whitespace around {expr} children is preserved:
            // "Count: {count} items" should render as "Count: 42 items", not "Count:42items".
            var source = """
                #nullable enable
                using System;
                using CsxRuntime;

                public record ViewProps();

                public static class View
                {
                    public static H.CSX.Element Render(ViewProps props, H.CSX.Element?[] children)
                    {
                        var sb = new System.Text.StringBuilder();
                        foreach (var c in children)
                            if (c is H.CSX.TextNode t) sb.Append(t.Text);
                        return new H.CSX.TextNode(sb.ToString());
                    }
                }

                class Program
                {
                    static void Main()
                    {
                        int count = 42;
                        H.CSX.Element el = <View>
                        Count: {count} {count} items
                        </View>;
                        Console.WriteLine(((H.CSX.TextNode)el).Text);
                    }
                }
                """;

            var runtimeSource = """
                #nullable enable
                namespace CsxRuntime
                {
                    public static class H
                    {
                        public static class CSX
                        {
                            public interface Element { }
                            public sealed class TextNode : Element
                            {
                                public readonly string Text;
                                public TextNode(string text) { Text = text; }
                            }
                            public static Element CreateTextNode(string text) => new TextNode(text);
                        }
                        public static CSX.Element CreateElement<TProps>(
                            System.Func<TProps, CSX.Element?[], CSX.Element> component,
                            TProps props,
                            params CSX.Element?[] children)
                            => component(props, children);
                    }
                }
                """;

            var comp = CreateCompilation(
                new[] { source, runtimeSource },
                parseOptions: CsxParseOptions(),
                targetFramework: TargetFramework.NetLatest,
                options: TestOptions.ReleaseExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "Count: 42 42 items");
        }
    }
}

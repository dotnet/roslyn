// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNamespace;

[UseExportProvider]
public class ConvertNamespaceCommandHandlerTests
{
    internal sealed class ConvertNamespaceTestState : AbstractCommandHandlerTestState
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeaturesWpf.AddParts(
            typeof(ConvertNamespaceCommandHandler));

        private readonly ConvertNamespaceCommandHandler _commandHandler;

        public ConvertNamespaceTestState(XElement workspaceElement)
            : base(workspaceElement, s_composition)
        {
            _commandHandler = (ConvertNamespaceCommandHandler)GetExportedValues<ICommandHandler>().
                Single(c => c is ConvertNamespaceCommandHandler);
        }

        public static ConvertNamespaceTestState CreateTestState(string markup)
            => new(GetWorkspaceXml(markup));

        public static XElement GetWorkspaceXml(string markup)
            => XElement.Parse(string.Format("""
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>{0}</Document>
                    </Project>
                </Workspace>
                """, markup));

        internal void AssertCodeIs(string expectedCode)
        {
            MarkupTestFile.GetPosition(expectedCode, out var massaged, out int caretPosition);
            Assert.Equal(massaged, TextView.TextSnapshot.GetText());
            Assert.Equal(caretPosition, TextView.Caret.Position.BufferPosition.Position);
        }

        public void SendTypeChar(char ch)
            => SendTypeChar(ch, _commandHandler.ExecuteCommand, () => EditorOperations.InsertText(ch.ToString()));
    }

    [WpfFact]
    public void TestSingleName()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace N$$
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace N;$$

            class C
            {
            }
            """);
    }

    [WpfFact]
    public void TestOptionOff()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace N$$
            {
                class C
                {
                }
            }
            """);

        testState.Workspace.GlobalOptions.SetGlobalOption(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, false);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace N;$$
            {
                class C
                {
                }
            }
            """);
    }

    [WpfFact]
    public void TestDottedName1()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace A.B$$
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace A.B;$$

            class C
            {
            }
            """);
    }

    [WpfFact]
    public void TestDottedName2()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace A.$$B
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace A.;$$B
            {
                class C
                {
                }
            }
            """);
    }

    [WpfFact]
    public void TestDottedName3()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace A$$.B
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace A;$$.B
            {
                class C
                {
                }
            }
            """);
    }

    [WpfFact]
    public void TestDottedName4()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace $$A.B
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace ;$$A.B
            {
                class C
                {
                }
            }
            """);
    }

    [WpfFact]
    public void TestAfterWhitespace()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace A.B  $$
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace A.B;$$  

            class C
            {
            }
            """);
    }

    [WpfFact]
    public void TestBeforeName()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace $$N
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace ;$$N
            {
                class C
                {
                }
            }
            """);
    }

    [WpfFact]
    public void TestNestedNamespace()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace N$$
            {
                namespace N2
                {
                    class C
                    {
                    }
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace N;$$
            {
                namespace N2
                {
                    class C
                    {
                    }
                }
            }
            """);
    }

    [WpfFact]
    public void TestSiblingNamespace()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace N$$
            {
            }

            namespace N2
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace N;$$
            {
            }

            namespace N2
            {
                class C
                {
                }
            }
            """);
    }

    [WpfFact]
    public void TestOuterUsings()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            using A;
            using B;

            namespace N$$
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            using A;
            using B;

            namespace N;$$

            class C
            {
            }
            """);
    }

    [WpfFact]
    public void TestInnerUsings()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace N$$
            {
                using A;
                using B;

                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace N;$$

            using A;
            using B;

            class C
            {
            }
            """);
    }

    [WpfFact]
    public void TestCommentAfterName()
    {
        using var testState = ConvertNamespaceTestState.CreateTestState(
            """
            namespace N$$ // Goo
            {
                class C
                {
                }
            }
            """);

        testState.SendTypeChar(';');
        testState.AssertCodeIs(
            """
            namespace N;$$ // Goo

            class C
            {
            }
            """);
    }
}

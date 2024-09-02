// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments;

[Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
public class XmlTagCompletionTests : AbstractXmlTagCompletionTests
{
    private protected override IChainedCommandHandler<TypeCharCommandArgs> CreateCommandHandler(EditorTestWorkspace workspace)
        => workspace.ExportProvider.GetCommandHandler<XmlTagCompletionCommandHandler>(nameof(XmlTagCompletionCommandHandler), ContentTypeNames.CSharpContentType);

    private protected override EditorTestWorkspace CreateTestWorkspace(string initialMarkup)
        => EditorTestWorkspace.CreateCSharp(initialMarkup);

    [WpfFact]
    public void SimpleTagCompletion()
    {
        var text = """
            /// <goo$$
            class c { }
            """;

        var expected = """
            /// <goo>$$</goo>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void NestedTagCompletion()
    {
        var text = """
            /// <summary>
            /// <goo$$
            /// </summary>
            class c { }
            """;

        var expected = """
            /// <summary>
            /// <goo>$$</goo>
            /// </summary>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void CompleteBeforeIncompleteTag()
    {
        var text = """
            /// <goo$$
            /// </summary>
            class c { }
            """;

        var expected = """
            /// <goo>$$</goo>
            /// </summary>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void NotEmptyElement()
    {
        var text = """
            /// <$$
            class c { }
            """;

        var expected = """
            /// <>$$
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void NotAlreadyCompleteTag()
    {
        var text = """
            /// <goo$$</goo>
            class c { }
            """;

        var expected = """
            /// <goo>$$</goo>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void NotAlreadyCompleteTag2()
    {
        var text = """
            /// <goo$$
            ///
            /// </goo>
            class c { }
            """;

        var expected = """
            /// <goo>$$
            ///
            /// </goo>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void SimpleSlashCompletion()
    {
        var text = """
            /// <goo><$$
            class c { }
            """;

        var expected = """
            /// <goo></goo>$$
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact]
    public void NestedSlashTagCompletion()
    {
        var text = """
            /// <summary>
            /// <goo><$$
            /// </summary>
            class c { }
            """;

        var expected = """
            /// <summary>
            /// <goo></goo>$$
            /// </summary>
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact]
    public void SlashCompleteBeforeIncompleteTag()
    {
        var text = """
            /// <goo><$$
            /// </summary>
            class c { }
            """;

        var expected = """
            /// <goo></goo>$$
            /// </summary>
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact]
    public void SlashNotEmptyElement()
    {
        var text = """
            /// <><$$
            class c { }
            """;

        var expected = """
            /// <></$$
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact]
    public void SlashNotAlreadyCompleteTag()
    {
        var text = """
            /// <goo><$$goo>
            class c { }
            """;

        var expected = """
            /// <goo></$$goo>
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact]
    public void SlashNotAlreadyCompleteTag2()
    {
        var text = """
            /// <goo>
            ///
            /// <$$goo>
            class c { }
            """;

        var expected = """
            /// <goo>
            ///
            /// </$$goo>
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")]
    public void NestedIdenticalTags()
    {
        var text = """
            /// <goo><goo$$</goo>
            class c { }
            """;

        var expected = """
            /// <goo><goo>$$</goo></goo>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")]
    public void MultipleNestedIdenticalTags()
    {
        var text = """
            /// <goo><goo><goo$$</goo></goo>
            class c { }
            """;

        var expected = """
            /// <goo><goo><goo>$$</goo></goo></goo>
            class c { }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")]
    public void SlashNotIfCloseTagFollows()
    {
        var text = """
            /// <summary>
            /// <$$
            /// </summary>
            class c { }
            """;

        var expected = """
            /// <summary>
            /// </$$
            /// </summary>
            class c { }
            """;

        Verify(text, expected, '/');
    }

    [WpfFact]
    public void TestSimpleTagCompletion()
    {
        var text = """
            /// <goo$$
            class C {}
            """;

        var expected = """
            /// <goo>$$</goo>
            class C {}
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void TestNestedTagCompletion()
    {
        var text = """
            /// <summary>
            /// <goo$$
            /// </summary>
            class C {}
            """;

        var expected = """
            /// <summary>
            /// <goo>$$</goo>
            /// </summary>
            class C {}
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void TestCompleteBeforeIncompleteTag()
    {
        var text = """
            /// <goo$$
            /// </summary>
            class C {}
            """;

        var expected = """
            /// <goo>$$</goo>
            /// </summary>
            class C {}
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void TestNotEmptyElement()
    {
        var text = """
            /// <$$
            class C {}
            """;

        var expected = """
            /// <>$$
            class C {}
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void TestNotAlreadyCompleteTag()
    {
        var text = """
            /// <goo$$</goo>
            class C {}
            """;

        var expected = """
            /// <goo>$$</goo>
            class C {}
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void TestNotAlreadyCompleteTag2()
    {
        var text = """
            /// <goo$$
            ///
            /// </goo>
            class C {}
            """;

        var expected = """
            /// <goo>$$
            ///
            /// </goo>
            class C {}
            """;

        Verify(text, expected, '>');
    }

    [WpfFact]
    public void TestNotOutsideDocComment()
    {
        var text = """
            class C
            {
                private int z = <goo$$
            }
            """;

        var expected = """
            class C
            {
                private int z = <goo>$$
            }
            """;

        Verify(text, expected, '>');
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")]
    public void TestNotCloseClosedTag()
    {
        var text = """
            /// <summary>
            /// <$$
            /// </summary>
            class C {}
            """;

        var expected = """
            /// <summary>
            /// </$$
            /// </summary>
            class C {}
            """;

        Verify(text, expected, '/');
    }
}

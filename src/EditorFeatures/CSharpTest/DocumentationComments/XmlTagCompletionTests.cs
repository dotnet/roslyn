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
public sealed class XmlTagCompletionTests : AbstractXmlTagCompletionTests
{
    private protected override IChainedCommandHandler<TypeCharCommandArgs> CreateCommandHandler(EditorTestWorkspace workspace)
        => workspace.ExportProvider.GetCommandHandler<XmlTagCompletionCommandHandler>(nameof(XmlTagCompletionCommandHandler), ContentTypeNames.CSharpContentType);

    private protected override EditorTestWorkspace CreateTestWorkspace(string initialMarkup)
        => EditorTestWorkspace.CreateCSharp(initialMarkup);

    [WpfFact]
    public void SimpleTagCompletion()
        => Verify("""
            /// <goo$$
            class c { }
            """, """
            /// <goo>$$</goo>
            class c { }
            """, '>');

    [WpfFact]
    public void NestedTagCompletion()
        => Verify("""
            /// <summary>
            /// <goo$$
            /// </summary>
            class c { }
            """, """
            /// <summary>
            /// <goo>$$</goo>
            /// </summary>
            class c { }
            """, '>');

    [WpfFact]
    public void CompleteBeforeIncompleteTag()
        => Verify("""
            /// <goo$$
            /// </summary>
            class c { }
            """, """
            /// <goo>$$</goo>
            /// </summary>
            class c { }
            """, '>');

    [WpfFact]
    public void NotEmptyElement()
        => Verify("""
            /// <$$
            class c { }
            """, """
            /// <>$$
            class c { }
            """, '>');

    [WpfFact]
    public void NotAlreadyCompleteTag()
        => Verify("""
            /// <goo$$</goo>
            class c { }
            """, """
            /// <goo>$$</goo>
            class c { }
            """, '>');

    [WpfFact]
    public void NotAlreadyCompleteTag2()
        => Verify("""
            /// <goo$$
            ///
            /// </goo>
            class c { }
            """, """
            /// <goo>$$
            ///
            /// </goo>
            class c { }
            """, '>');

    [WpfFact]
    public void SimpleSlashCompletion()
        => Verify("""
            /// <goo><$$
            class c { }
            """, """
            /// <goo></goo>$$
            class c { }
            """, '/');

    [WpfFact]
    public void NestedSlashTagCompletion()
        => Verify("""
            /// <summary>
            /// <goo><$$
            /// </summary>
            class c { }
            """, """
            /// <summary>
            /// <goo></goo>$$
            /// </summary>
            class c { }
            """, '/');

    [WpfFact]
    public void SlashCompleteBeforeIncompleteTag()
        => Verify("""
            /// <goo><$$
            /// </summary>
            class c { }
            """, """
            /// <goo></goo>$$
            /// </summary>
            class c { }
            """, '/');

    [WpfFact]
    public void SlashNotEmptyElement()
        => Verify("""
            /// <><$$
            class c { }
            """, """
            /// <></$$
            class c { }
            """, '/');

    [WpfFact]
    public void SlashNotAlreadyCompleteTag()
        => Verify("""
            /// <goo><$$goo>
            class c { }
            """, """
            /// <goo></$$goo>
            class c { }
            """, '/');

    [WpfFact]
    public void SlashNotAlreadyCompleteTag2()
        => Verify("""
            /// <goo>
            ///
            /// <$$goo>
            class c { }
            """, """
            /// <goo>
            ///
            /// </$$goo>
            class c { }
            """, '/');

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")]
    public void NestedIdenticalTags()
        => Verify("""
            /// <goo><goo$$</goo>
            class c { }
            """, """
            /// <goo><goo>$$</goo></goo>
            class c { }
            """, '>');

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")]
    public void MultipleNestedIdenticalTags()
        => Verify("""
            /// <goo><goo><goo$$</goo></goo>
            class c { }
            """, """
            /// <goo><goo><goo>$$</goo></goo></goo>
            class c { }
            """, '>');

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")]
    public void SlashNotIfCloseTagFollows()
        => Verify("""
            /// <summary>
            /// <$$
            /// </summary>
            class c { }
            """, """
            /// <summary>
            /// </$$
            /// </summary>
            class c { }
            """, '/');

    [WpfFact]
    public void TestSimpleTagCompletion()
        => Verify("""
            /// <goo$$
            class C {}
            """, """
            /// <goo>$$</goo>
            class C {}
            """, '>');

    [WpfFact]
    public void TestNestedTagCompletion()
        => Verify("""
            /// <summary>
            /// <goo$$
            /// </summary>
            class C {}
            """, """
            /// <summary>
            /// <goo>$$</goo>
            /// </summary>
            class C {}
            """, '>');

    [WpfFact]
    public void TestCompleteBeforeIncompleteTag()
        => Verify("""
            /// <goo$$
            /// </summary>
            class C {}
            """, """
            /// <goo>$$</goo>
            /// </summary>
            class C {}
            """, '>');

    [WpfFact]
    public void TestNotEmptyElement()
        => Verify("""
            /// <$$
            class C {}
            """, """
            /// <>$$
            class C {}
            """, '>');

    [WpfFact]
    public void TestNotAlreadyCompleteTag()
        => Verify("""
            /// <goo$$</goo>
            class C {}
            """, """
            /// <goo>$$</goo>
            class C {}
            """, '>');

    [WpfFact]
    public void TestNotAlreadyCompleteTag2()
        => Verify("""
            /// <goo$$
            ///
            /// </goo>
            class C {}
            """, """
            /// <goo>$$
            ///
            /// </goo>
            class C {}
            """, '>');

    [WpfFact]
    public void TestNotOutsideDocComment()
        => Verify("""
            class C
            {
                private int z = <goo$$
            }
            """, """
            class C
            {
                private int z = <goo>$$
            }
            """, '>');

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")]
    public void TestNotCloseClosedTag()
        => Verify("""
            /// <summary>
            /// <$$
            /// </summary>
            class C {}
            """, """
            /// <summary>
            /// </$$
            /// </summary>
            class C {}
            """, '/');
}

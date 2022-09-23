// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments
{
    [Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
    public class XmlTagCompletionTests : AbstractXmlTagCompletionTests
    {
        [WpfFact]
        public void SimpleTagCompletion()
        {
            var text = @"
/// <goo$$
class c { }";

            var expected = @"
/// <goo>$$</goo>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact]
        public void NestedTagCompletion()
        {
            var text = @"
/// <summary>
/// <goo$$
/// </summary>
class c { }";

            var expected = @"
/// <summary>
/// <goo>$$</goo>
/// </summary>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact]
        public void CompleteBeforeIncompleteTag()
        {
            var text = @"
/// <goo$$
/// </summary>
class c { }";

            var expected = @"
/// <goo>$$</goo>
/// </summary>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact]
        public void NotEmptyElement()
        {
            var text = @"
/// <$$
class c { }";

            var expected = @"
/// <>$$
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact]
        public void NotAlreadyCompleteTag()
        {
            var text = @"
/// <goo$$</goo>
class c { }";

            var expected = @"
/// <goo>$$</goo>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact]
        public void NotAlreadyCompleteTag2()
        {
            var text = @"
/// <goo$$
///
/// </goo>
class c { }";

            var expected = @"
/// <goo>$$
///
/// </goo>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact]
        public void SimpleSlashCompletion()
        {
            var text = @"
/// <goo><$$
class c { }";

            var expected = @"
/// <goo></goo>$$
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact]
        public void NestedSlashTagCompletion()
        {
            var text = @"
/// <summary>
/// <goo><$$
/// </summary>
class c { }";

            var expected = @"
/// <summary>
/// <goo></goo>$$
/// </summary>
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact]
        public void SlashCompleteBeforeIncompleteTag()
        {
            var text = @"
/// <goo><$$
/// </summary>
class c { }";

            var expected = @"
/// <goo></goo>$$
/// </summary>
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact]
        public void SlashNotEmptyElement()
        {
            var text = @"
/// <><$$
class c { }";

            var expected = @"
/// <></$$
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact]
        public void SlashNotAlreadyCompleteTag()
        {
            var text = @"
/// <goo><$$goo>
class c { }";

            var expected = @"
/// <goo></$$goo>
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact]
        public void SlashNotAlreadyCompleteTag2()
        {
            var text = @"
/// <goo>
///
/// <$$goo>
class c { }";

            var expected = @"
/// <goo>
///
/// </$$goo>
class c { }";

            Verify(text, expected, '/');
        }

        [WorkItem(638800, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")]
        [WpfFact]
        public void NestedIdenticalTags()
        {
            var text = @"
/// <goo><goo$$</goo>
class c { }";

            var expected = @"
/// <goo><goo>$$</goo></goo>
class c { }";

            Verify(text, expected, '>');
        }

        [WorkItem(638800, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638800")]
        [WpfFact]
        public void MultipleNestedIdenticalTags()
        {
            var text = @"
/// <goo><goo><goo$$</goo></goo>
class c { }";

            var expected = @"
/// <goo><goo><goo>$$</goo></goo></goo>
class c { }";

            Verify(text, expected, '>');
        }

        [WorkItem(638235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638235")]
        [WpfFact]
        public void SlashNotIfCloseTagFollows()
        {
            var text = @"
/// <summary>
/// <$$
/// </summary>
class c { }";

            var expected = @"
/// <summary>
/// </$$
/// </summary>
class c { }";

            Verify(text, expected, '/');
        }

        internal override IChainedCommandHandler<TypeCharCommandArgs> CreateCommandHandler(TestWorkspace workspace)
            => workspace.ExportProvider.GetCommandHandler<XmlTagCompletionCommandHandler>(nameof(XmlTagCompletionCommandHandler), ContentTypeNames.CSharpContentType);

        protected override TestWorkspace CreateTestWorkspace(string initialMarkup)
            => TestWorkspace.CreateCSharp(initialMarkup);
    }
}

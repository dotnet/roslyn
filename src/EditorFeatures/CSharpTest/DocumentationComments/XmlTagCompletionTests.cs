// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments
{
    public class XmlTagCompletionTests : AbstractXmlTagCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
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

        internal override IChainedCommandHandler<TypeCharCommandArgs> CreateCommandHandler(ITextUndoHistoryRegistry undoHistory)
        {
            return new XmlTagCompletionCommandHandler(undoHistory);
        }

        protected override TestWorkspace CreateTestWorkspace(string initialMarkup)
            => TestWorkspace.CreateCSharp(initialMarkup);
    }
}

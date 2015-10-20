// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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
/// <foo$$
class c { }";

            var expected = @"
/// <foo>$$</foo>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void NestedTagCompletion()
        {
            var text = @"
/// <summary>
/// <foo$$
/// </summary>
class c { }";

            var expected = @"
/// <summary>
/// <foo>$$</foo>
/// </summary>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void CompleteBeforeIncompleteTag()
        {
            var text = @"
/// <foo$$
/// </summary>
class c { }";

            var expected = @"
/// <foo>$$</foo>
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
/// <foo$$</foo>
class c { }";

            var expected = @"
/// <foo>$$</foo>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void NotAlreadyCompleteTag2()
        {
            var text = @"
/// <foo$$
///
/// </foo>
class c { }";

            var expected = @"
/// <foo>$$
///
/// </foo>
class c { }";

            Verify(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void SimpleSlashCompletion()
        {
            var text = @"
/// <foo><$$
class c { }";

            var expected = @"
/// <foo></foo>$$
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void NestedSlashTagCompletion()
        {
            var text = @"
/// <summary>
/// <foo><$$
/// </summary>
class c { }";

            var expected = @"
/// <summary>
/// <foo></foo>$$
/// </summary>
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void SlashCompleteBeforeIncompleteTag()
        {
            var text = @"
/// <foo><$$
/// </summary>
class c { }";

            var expected = @"
/// <foo></foo>$$
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
/// <foo><$$foo>
class c { }";

            var expected = @"
/// <foo></$$foo>
class c { }";

            Verify(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void SlashNotAlreadyCompleteTag2()
        {
            var text = @"
/// <foo>
///
/// <$$foo>
class c { }";

            var expected = @"
/// <foo>
///
/// </$$foo>
class c { }";

            Verify(text, expected, '/');
        }

        [WorkItem(638800)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void NestedIdenticalTags()
        {
            var text = @"
/// <foo><foo$$</foo>
class c { }";

            var expected = @"
/// <foo><foo>$$</foo></foo>
class c { }";

            Verify(text, expected, '>');
        }

        [WorkItem(638800)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public void MultipleNestedIdenticalTags()
        {
            var text = @"
/// <foo><foo><foo$$</foo></foo>
class c { }";

            var expected = @"
/// <foo><foo><foo>$$</foo></foo></foo>
class c { }";

            Verify(text, expected, '>');
        }

        [WorkItem(638235)]
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

        internal override ICommandHandler<TypeCharCommandArgs> CreateCommandHandler(ITextUndoHistoryRegistry undoHistory)
        {
            return new XmlTagCompletionCommandHandler(undoHistory, TestWaitIndicator.Default);
        }

        protected override TestWorkspace CreateTestWorkspace(string initialMarkup)
        {
            return CSharpWorkspaceFactory.CreateWorkspaceFromLines(initialMarkup);
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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
        public async Task SimpleTagCompletion()
        {
            var text = @"
/// <foo$$
class c { }";

            var expected = @"
/// <foo>$$</foo>
class c { }";

            await VerifyAsync(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task NestedTagCompletion()
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

            await VerifyAsync(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task CompleteBeforeIncompleteTag()
        {
            var text = @"
/// <foo$$
/// </summary>
class c { }";

            var expected = @"
/// <foo>$$</foo>
/// </summary>
class c { }";

            await VerifyAsync(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task NotEmptyElement()
        {
            var text = @"
/// <$$
class c { }";

            var expected = @"
/// <>$$
class c { }";

            await VerifyAsync(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task NotAlreadyCompleteTag()
        {
            var text = @"
/// <foo$$</foo>
class c { }";

            var expected = @"
/// <foo>$$</foo>
class c { }";

            await VerifyAsync(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task NotAlreadyCompleteTag2()
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

            await VerifyAsync(text, expected, '>');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task SimpleSlashCompletion()
        {
            var text = @"
/// <foo><$$
class c { }";

            var expected = @"
/// <foo></foo>$$
class c { }";

            await VerifyAsync(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task NestedSlashTagCompletion()
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

            await VerifyAsync(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task SlashCompleteBeforeIncompleteTag()
        {
            var text = @"
/// <foo><$$
/// </summary>
class c { }";

            var expected = @"
/// <foo></foo>$$
/// </summary>
class c { }";

            await VerifyAsync(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task SlashNotEmptyElement()
        {
            var text = @"
/// <><$$
class c { }";

            var expected = @"
/// <></$$
class c { }";

            await VerifyAsync(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task SlashNotAlreadyCompleteTag()
        {
            var text = @"
/// <foo><$$foo>
class c { }";

            var expected = @"
/// <foo></$$foo>
class c { }";

            await VerifyAsync(text, expected, '/');
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task SlashNotAlreadyCompleteTag2()
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

            await VerifyAsync(text, expected, '/');
        }

        [WorkItem(638800)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task NestedIdenticalTags()
        {
            var text = @"
/// <foo><foo$$</foo>
class c { }";

            var expected = @"
/// <foo><foo>$$</foo></foo>
class c { }";

            await VerifyAsync(text, expected, '>');
        }

        [WorkItem(638800)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task MultipleNestedIdenticalTags()
        {
            var text = @"
/// <foo><foo><foo$$</foo></foo>
class c { }";

            var expected = @"
/// <foo><foo><foo>$$</foo></foo></foo>
class c { }";

            await VerifyAsync(text, expected, '>');
        }

        [WorkItem(638235)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.XmlTagCompletion)]
        public async Task SlashNotIfCloseTagFollows()
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

            await VerifyAsync(text, expected, '/');
        }

        internal override ICommandHandler<TypeCharCommandArgs> CreateCommandHandler(ITextUndoHistoryRegistry undoHistory)
        {
            return new XmlTagCompletionCommandHandler(undoHistory, TestWaitIndicator.Default);
        }

        protected override Task<TestWorkspace> CreateTestWorkspaceAsync(string initialMarkup)
        {
            return TestWorkspaceFactory.CreateCSharpWorkspaceAsync(initialMarkup);
        }
    }
}

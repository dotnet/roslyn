// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpEventHookup : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpEventHookup(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpEventHookup))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        void VerifyQuickInfoTooltip()
        {
            SetUpEditor(@"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress$$
    }
}");
            VisualStudio.Editor.SendKeys(" +=");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.EventHookup);
            Assert.Equal("Console_CancelKeyPress;     (Press TAB to insert)",
                VisualStudio.Editor.GetQuickInfo());

        }

        [Fact, Trait(Traits.Feature, Traits.Features.EventHookup)]
        void VerifyCommitAndRenameTags()
        {
            SetUpEditor(@"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress$$
    }
}");
            VisualStudio.Editor.SendKeys(" +=", VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.EventHookup);
            var expectedMarkup = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.CancelKeyPress += [|Console_CancelKeyPress$$|];
    }

    private static void [|Console_CancelKeyPress|](object sender, ConsoleCancelEventArgs e) => throw new NotImplementedException();
}";
            MarkupTestFile.GetSpans(expectedMarkup,  out var expectedText, out ImmutableArray<TextSpan> expectedSpans);

            VisualStudio.Editor.Verify.TextContains(expectedText);
            VisualStudio.Editor.Verify.CurrentLineText("Console.CancelKeyPress += Console_CancelKeyPress$$;", assertCaretPosition: true, trimWhitespace: true);
            var tagSpans = VisualStudio.Editor.GetTagSpans(VisualStudio.InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(expectedSpans, tagSpans);
        }
    }
}
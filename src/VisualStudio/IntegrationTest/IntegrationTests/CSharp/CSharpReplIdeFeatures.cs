// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIdeFeatures : AbstractIdeInteractiveWindowTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(true);
        }

        public override async Task DisposeAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);
            await VisualStudio.InteractiveWindow.ClearReplTextAsync();
            await VisualStudio.InteractiveWindow.ResetAsync();
            await base.DisposeAsync();
        }

        [IdeFact]
        public async Task VerifyDefaultUsingStatementsAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Console.WriteLine(42);");
            await VisualStudio.InteractiveWindow.WaitForLastReplOutputAsync("42");
        }

        [IdeFact]
        public async Task VerifyCodeActionsNotAvailableInPreviousSubmissionAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("Console.WriteLine(42);");
            await VisualStudio.InteractiveWindow.Verify.CodeActionsNotShowingAsync();
        }

        [IdeFact]
        public async Task VerifyQuickInfoOnStringDocCommentsFromMetadataAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("static void Goo(string[] args) { }");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("[]", charsOffset: -2);
            await VisualStudio.InteractiveWindow.InvokeQuickInfoAsync();
            var s = await VisualStudio.InteractiveWindow.GetQuickInfoAsync();
            Assert.Equal("class‎ System‎.String", s);
        }

        [IdeFact]
        public async Task InternationalAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("func", charsOffset: -1);
            await VisualStudio.InteractiveWindow.InvokeQuickInfoAsync();
            var s = await VisualStudio.InteractiveWindow.GetQuickInfoAsync();
            Assert.Equal("‎(field‎)‎ العربية‎ func", s);
        }

        [IdeFact]
        public async Task HighlightRefsSingleSubmissionVerifyRenameTagsShowUpWhenInvokedOnUnsubmittedTextAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("someint = 22", charsOffset: -6);

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
        }

        [IdeFact]
        public async Task HighlightRefsSingleSubmissionVerifyRenameTagsGoAwayAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("someint = 22", charsOffset: -6);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("22");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 0);
        }

        [IdeFact]
        public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedTextAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("class Goo { }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Goo something = new Goo();");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("something.ToString();");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("someth", charsOffset: 1, occurrence: 2);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [IdeFact]
        public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedTextAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("class Goo { }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Goo something = new Goo();");
            VisualStudio.InteractiveWindow.InsertCode("something.ToString();");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("someth", charsOffset: 1, occurrence: 2);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [IdeFact]
        public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedTextAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("class Goo { }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Goo a;");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Goo b;");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Goo b", charsOffset: -1);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [IdeFact]
        public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedTextAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("class Goo { }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Goo a;");
            VisualStudio.InteractiveWindow.InsertCode("Goo b;");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Goo b", charsOffset: -1);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [IdeFact]
        public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedTextAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("class Goo { }");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("Goo a;");
            VisualStudio.InteractiveWindow.InsertCode("Goo b;Something();");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Something();", charsOffset: -1);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [IdeFact]
        public async Task HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariableAsync()
        {
            await VisualStudio.InteractiveWindow.SubmitTextAsync("string abc = null;");
            await VisualStudio.InteractiveWindow.SubmitTextAsync("abc = string.Empty;");
            VisualStudio.InteractiveWindow.InsertCode("int abc = 42;");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("abc", occurrence: 3);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ReferenceHighlighting);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            await VisualStudio.InteractiveWindow.VerifyTagsAsyn(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [IdeFact]
        public async Task DisabledCommandsPart1Async()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"public class Class
{
    int field;

    public void Method(int x)
    {
         int abc = 1 + 1;
     }
}");

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("abc");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            Assert.False(await VisualStudio.VisualStudio.IsCommandAvailableAsync(WellKnownCommandNames.Refactor_Rename));

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("1 + 1");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            Assert.False(await VisualStudio.VisualStudio.IsCommandAvailableAsync(WellKnownCommandNames.Refactor_ExtractMethod));

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Class");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            Assert.False(await VisualStudio.VisualStudio.IsCommandAvailableAsync(WellKnownCommandNames.Refactor_ExtractInterface));

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("field");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            Assert.False(await VisualStudio.VisualStudio.IsCommandAvailableAsync(WellKnownCommandNames.Refactor_EncapsulateField));

            await VisualStudio.InteractiveWindow.PlaceCaretAsync("Method");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            Assert.False(await VisualStudio.VisualStudio.IsCommandAvailableAsync(WellKnownCommandNames.Refactor_RemoveParameters));
            Assert.False(await VisualStudio.VisualStudio.IsCommandAvailableAsync(WellKnownCommandNames.Refactor_ReorderParameters));
        }

        [IdeFact]
        public async Task AddUsingAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("typeof(ArrayList)");
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("ArrayList");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.InteractiveWindow.InvokeCodeActionListAsync();
            await VisualStudio.InteractiveWindow.Verify.CodeActionsAsync(
                new string[] { "using System.Collections;", "System.Collections.ArrayList" },
                "using System.Collections;");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"using System.Collections;

typeof(ArrayList)");
        }

        [IdeFact]
        public async Task QualifyNameAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("typeof(ArrayList)");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.InteractiveWindow.PlaceCaretAsync("ArrayList");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.InteractiveWindow.Verify.CodeActionsAsync(
    new string[] { "using System.Collections;", "System.Collections.ArrayList" },
    "System.Collections.ArrayList");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("typeof(System.Collections.ArrayList)");
        }
    }
}

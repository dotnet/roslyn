// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIdeFeatures : AbstractInteractiveWindowTest
    {
        public CSharpReplIdeFeatures(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        public override Task DisposeAsync()
        {
            VisualStudio.Editor.SetUseSuggestionMode(false);
            VisualStudio.InteractiveWindow.ClearReplText();
            VisualStudio.InteractiveWindow.Reset();
            return base.DisposeAsync();
        }

        [WpfFact]
        public void VerifyDefaultUsingStatements()
        {
            VisualStudio.InteractiveWindow.SubmitText("Console.WriteLine(42);");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("42");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void VerifyCodeActionsNotAvailableInPreviousSubmission()
        {
            VisualStudio.InteractiveWindow.InsertCode("Console.WriteLine(42);");
            VisualStudio.InteractiveWindow.Verify.CodeActionsNotShowing();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void VerifyQuickInfoOnStringDocCommentsFromMetadata()
        {
            VisualStudio.InteractiveWindow.InsertCode("static void Goo(string[] args) { }");
            VisualStudio.InteractiveWindow.PlaceCaret("[]", charsOffset: -2);
            VisualStudio.InteractiveWindow.InvokeQuickInfo();
            var s = VisualStudio.InteractiveWindow.GetQuickInfo();
            Assert.Equal("class System.String", s);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void International()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);");
            VisualStudio.InteractiveWindow.PlaceCaret("func", charsOffset: -1);
            VisualStudio.InteractiveWindow.InvokeQuickInfo();
            var s = VisualStudio.InteractiveWindow.GetQuickInfo();
            Assert.Equal("(field) العربية func", s);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsShowUpWhenInvokedOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            VisualStudio.InteractiveWindow.PlaceCaret("someint = 22", charsOffset: -6);

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsGoAway()
        {
            VisualStudio.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            VisualStudio.InteractiveWindow.PlaceCaret("someint = 22", charsOffset: -6);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);

            VisualStudio.InteractiveWindow.PlaceCaret("22");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 0);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/46027")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudio.InteractiveWindow.SubmitText("Goo something = new Goo();");
            VisualStudio.InteractiveWindow.SubmitText("something.ToString();");
            VisualStudio.InteractiveWindow.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/46027")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudio.InteractiveWindow.SubmitText("Goo something = new Goo();");
            VisualStudio.InteractiveWindow.InsertCode("something.ToString();");
            VisualStudio.InteractiveWindow.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/46027")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudio.InteractiveWindow.SubmitText("Goo a;");
            VisualStudio.InteractiveWindow.SubmitText("Goo b;");
            VisualStudio.InteractiveWindow.PlaceCaret("Goo b", charsOffset: -1);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/46027")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudio.InteractiveWindow.SubmitText("Goo a;");
            VisualStudio.InteractiveWindow.InsertCode("Goo b;");
            VisualStudio.InteractiveWindow.PlaceCaret("Goo b", charsOffset: -1);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudio.InteractiveWindow.SubmitText("Goo a;");
            VisualStudio.InteractiveWindow.InsertCode("Goo b;Something();");
            VisualStudio.InteractiveWindow.PlaceCaret("Something();", charsOffset: -1);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariable()
        {
            VisualStudio.InteractiveWindow.SubmitText("string abc = null;");
            VisualStudio.InteractiveWindow.SubmitText("abc = string.Empty;");
            VisualStudio.InteractiveWindow.InsertCode("int abc = 42;");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.PlaceCaret("abc", occurrence: 3);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void DisabledCommandsPart1()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"public class Class
{
    int field;

    public void Method(int x)
    {
         int abc = 1 + 1;
     }
}");

            VisualStudio.InteractiveWindow.PlaceCaret("abc");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_Rename));

            VisualStudio.InteractiveWindow.PlaceCaret("1 + 1");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractMethod));

            VisualStudio.InteractiveWindow.PlaceCaret("Class");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractInterface));

            VisualStudio.InteractiveWindow.PlaceCaret("field");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_EncapsulateField));

            VisualStudio.InteractiveWindow.PlaceCaret("Method");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_RemoveParameters));
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_ReorderParameters));
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void AddUsing()
        {
            VisualStudio.InteractiveWindow.InsertCode("typeof(ArrayList)");
            VisualStudio.InteractiveWindow.PlaceCaret("ArrayList");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.InvokeCodeActionList();
            VisualStudio.InteractiveWindow.Verify.CodeActions(
                new string[] { "using System.Collections;", "System.Collections.ArrayList" },
                "using System.Collections;");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"using System.Collections;

typeof(ArrayList)");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/40160")]
        public void QualifyName()
        {
            VisualStudio.InteractiveWindow.InsertCode("typeof(ArrayList)");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.PlaceCaret("ArrayList");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.Verify.CodeActions(
    new string[] { "using System.Collections;", "System.Collections.ArrayList" },
    "System.Collections.ArrayList");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("typeof(System.Collections.ArrayList)");
        }
    }
}

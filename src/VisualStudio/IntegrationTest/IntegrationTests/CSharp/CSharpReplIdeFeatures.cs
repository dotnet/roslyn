// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpReplIdeFeatures : AbstractInteractiveWindowTest
    {
        public CSharpReplIdeFeatures() : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudioInstance.Workspace.SetUseSuggestionMode(true);
        }

        public override Task DisposeAsync()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);
            VisualStudioInstance.InteractiveWindow.ClearReplText();
            VisualStudioInstance.InteractiveWindow.Reset();
            return base.DisposeAsync();
        }

        [WpfFact]
        public void VerifyDefaultUsingStatements()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("Console.WriteLine(42);");
            VisualStudioInstance.InteractiveWindow.WaitForLastReplOutput("42");
        }

        [WpfFact]
        public void VerifyCodeActionsNotAvailableInPreviousSubmission()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("Console.WriteLine(42);");
            VisualStudioInstance.InteractiveWindow.Verify.CodeActionsNotShowing();
        }

        [WpfFact]
        public void VerifyQuickInfoOnStringDocCommentsFromMetadata()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("static void Goo(string[] args) { }");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("[]", charsOffset: -2);
            VisualStudioInstance.InteractiveWindow.InvokeQuickInfo();
            var s = VisualStudioInstance.InteractiveWindow.GetQuickInfo();
            Assert.AreEqual("class System.String", s);
        }

        [WpfFact]
        public void International()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("func", charsOffset: -1);
            VisualStudioInstance.InteractiveWindow.InvokeQuickInfo();
            var s = VisualStudioInstance.InteractiveWindow.GetQuickInfo();
            Assert.AreEqual("(field) العربية func", s);
        }

        [WpfFact]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsShowUpWhenInvokedOnUnsubmittedText()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("someint = 22", charsOffset: -6);

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
        }

        [WpfFact]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsGoAway()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("someint = 22", charsOffset: -6);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);

            VisualStudioInstance.InteractiveWindow.PlaceCaret("22");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 0);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedText()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudioInstance.InteractiveWindow.SubmitText("Goo something = new Goo();");
            VisualStudioInstance.InteractiveWindow.SubmitText("something.ToString();");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedText()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudioInstance.InteractiveWindow.SubmitText("Goo something = new Goo();");
            VisualStudioInstance.InteractiveWindow.InsertCode("something.ToString();");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedText()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudioInstance.InteractiveWindow.SubmitText("Goo a;");
            VisualStudioInstance.InteractiveWindow.SubmitText("Goo b;");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Goo b", charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedText()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudioInstance.InteractiveWindow.SubmitText("Goo a;");
            VisualStudioInstance.InteractiveWindow.InsertCode("Goo b;");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Goo b", charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedText()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("class Goo { }");
            VisualStudioInstance.InteractiveWindow.SubmitText("Goo a;");
            VisualStudioInstance.InteractiveWindow.InsertCode("Goo b;Something();");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("Something();", charsOffset: -1);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [WpfFact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariable()
        {
            VisualStudioInstance.InteractiveWindow.SubmitText("string abc = null;");
            VisualStudioInstance.InteractiveWindow.SubmitText("abc = string.Empty;");
            VisualStudioInstance.InteractiveWindow.InsertCode("int abc = 42;");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("abc", occurrence: 3);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudioInstance.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [WpfFact]
        public void DisabledCommandsPart1()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode(@"public class Class
{
    int field;

    public void Method(int x)
    {
         int abc = 1 + 1;
     }
}");

            VisualStudioInstance.InteractiveWindow.PlaceCaret("abc");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.IsFalse(VisualStudioInstance.IsCommandAvailable(WellKnownCommandNames.Refactor_Rename));

            VisualStudioInstance.InteractiveWindow.PlaceCaret("1 + 1");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.IsFalse(VisualStudioInstance.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractMethod));

            VisualStudioInstance.InteractiveWindow.PlaceCaret("Class");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.IsFalse(VisualStudioInstance.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractInterface));

            VisualStudioInstance.InteractiveWindow.PlaceCaret("field");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.IsFalse(VisualStudioInstance.IsCommandAvailable(WellKnownCommandNames.Refactor_EncapsulateField));

            VisualStudioInstance.InteractiveWindow.PlaceCaret("Method");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.IsFalse(VisualStudioInstance.IsCommandAvailable(WellKnownCommandNames.Refactor_RemoveParameters));
            Assert.IsFalse(VisualStudioInstance.IsCommandAvailable(WellKnownCommandNames.Refactor_ReorderParameters));
        }

        [WpfFact]
        public void AddUsing()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("typeof(ArrayList)");
            VisualStudioInstance.InteractiveWindow.PlaceCaret("ArrayList");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.InteractiveWindow.InvokeCodeActionList();
            VisualStudioInstance.InteractiveWindow.Verify.CodeActions(
                new string[] { "using System.Collections;", "System.Collections.ArrayList" },
                "using System.Collections;");

            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput(@"using System.Collections;

typeof(ArrayList)");
        }

        [WpfFact]
        public void QualifyName()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("typeof(ArrayList)");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.InteractiveWindow.PlaceCaret("ArrayList");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.InteractiveWindow.Verify.CodeActions(
    new string[] { "using System.Collections;", "System.Collections.ArrayList" },
    "System.Collections.ArrayList");
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("typeof(System.Collections.ArrayList)");
        }
    }
}

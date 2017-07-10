// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIdeFeatures : AbstractInteractiveWindowTest
    {
        public CSharpReplIdeFeatures(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Workspace.SetUseSuggestionMode(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                VisualStudio.Workspace.SetUseSuggestionMode(false);
                VisualStudio.InteractiveWindow.Reset();
            }

            base.Dispose(disposing);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void VerifyDefaultUsingStatements()
        {
            VisualStudio.InteractiveWindow.SubmitText("Console.WriteLine(42);");
            VisualStudio.InteractiveWindow.WaitForLastReplOutput("42");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void VerifyCodeActionsNotAvailableInPreviousSubmission()
        {
            VisualStudio.InteractiveWindow.InsertCode("Console.WriteLine(42);");
            VisualStudio.InteractiveWindow.Verify.CodeActionsNotShowing();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19914")]
        public void VerifyQuickInfoOnStringDocCommentsFromMetadata()
        {
            VisualStudio.InteractiveWindow.InsertCode("static void Foo(string[] args) { }");
            VisualStudio.InteractiveWindow.PlaceCaret("[]", charsOffset: -2);
            VisualStudio.InteractiveWindow.InvokeQuickInfo();
            var s = VisualStudio.InteractiveWindow.GetQuickInfo();
            Assert.Equal("class‎ System‎.String", s);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/19914")]
        public void International()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);");
            VisualStudio.InteractiveWindow.PlaceCaret("func", charsOffset: -1);
            VisualStudio.InteractiveWindow.InvokeQuickInfo();
            var s = VisualStudio.InteractiveWindow.GetQuickInfo();
            Assert.Equal("‎(field‎)‎ العربية‎ func", s);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsShowUpWhenInvokedOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            VisualStudio.InteractiveWindow.PlaceCaret("someint = 22", charsOffset: -6);

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsGoAway()
        {
            VisualStudio.InteractiveWindow.InsertCode("int someint; someint = 22; someint = 23;");
            VisualStudio.InteractiveWindow.PlaceCaret("someint = 22", charsOffset: -6);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);

            VisualStudio.InteractiveWindow.PlaceCaret("22");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 0);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Foo { }");
            VisualStudio.InteractiveWindow.SubmitText("Foo something = new Foo();");
            VisualStudio.InteractiveWindow.SubmitText("something.ToString();");
            VisualStudio.InteractiveWindow.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Foo { }");
            VisualStudio.InteractiveWindow.SubmitText("Foo something = new Foo();");
            VisualStudio.InteractiveWindow.InsertCode("something.ToString();");
            VisualStudio.InteractiveWindow.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Foo { }");
            VisualStudio.InteractiveWindow.SubmitText("Foo a;");
            VisualStudio.InteractiveWindow.SubmitText("Foo b;");
            VisualStudio.InteractiveWindow.PlaceCaret("Foo b", charsOffset: -1);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Foo { }");
            VisualStudio.InteractiveWindow.SubmitText("Foo a;");
            VisualStudio.InteractiveWindow.InsertCode("Foo b;");
            VisualStudio.InteractiveWindow.PlaceCaret("Foo b", charsOffset: -1);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedText()
        {
            VisualStudio.InteractiveWindow.SubmitText("class Foo { }");
            VisualStudio.InteractiveWindow.SubmitText("Foo a;");
            VisualStudio.InteractiveWindow.InsertCode("Foo b;Something();");
            VisualStudio.InteractiveWindow.PlaceCaret("Something();", charsOffset: -1);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariable()
        {
            VisualStudio.InteractiveWindow.SubmitText("string abc = null;");
            VisualStudio.InteractiveWindow.SubmitText("abc = string.Empty;");
            VisualStudio.InteractiveWindow.InsertCode("int abc = 42;");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.PlaceCaret("abc", occurrence: 3);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            VisualStudio.InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20219")]
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
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_Rename));

            VisualStudio.InteractiveWindow.PlaceCaret("1 + 1");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractMethod));

            VisualStudio.InteractiveWindow.PlaceCaret("Class");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractInterface));

            VisualStudio.InteractiveWindow.PlaceCaret("field");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_EncapsulateField));

            VisualStudio.InteractiveWindow.PlaceCaret("Method");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_RemoveParameters));
            Assert.False(VisualStudio.IsCommandAvailable(WellKnownCommandNames.Refactor_ReorderParameters));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6587, No support of quick actions in ETA scenario")]
        public void AddUsing()
        {
            VisualStudio.InteractiveWindow.InsertCode("typeof(ArrayList)");
            VisualStudio.InteractiveWindow.PlaceCaret("ArrayList");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.InvokeCodeActionList();
            VisualStudio.InteractiveWindow.Verify.CodeActions(
                new string[] { "using System.Collections;", "System.Collections.ArrayList" },
                "using System.Collections;");

            VisualStudio.InteractiveWindow.Verify.LastReplInput(@"using System.Collections;
typeof(ArrayList)");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6587, No support of quick actions in ETA scenario")]
        public void QualifyName()
        {
            VisualStudio.InteractiveWindow.InsertCode("typeof(ArrayList)");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.PlaceCaret("ArrayList");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.InteractiveWindow.Verify.CodeActions(
    new string[] { "using System.Collections;", "System.Collections.ArrayList" },
    "System.Collections.ArrayList");
            VisualStudio.InteractiveWindow.Verify.LastReplInput("System.Collections.ArrayList");
        }
    }
}

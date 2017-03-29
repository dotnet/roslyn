// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIdeFeatures : AbstractInteractiveWindowTest
    {
        public CSharpReplIdeFeatures(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);
        }

        public new void Dispose()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
            this.Reset();
            base.Dispose();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/801")]
        public void VerifyDefaultUsingStatements()
        {
            this.SubmitText("Console.WriteLine(42);");
            this.WaitForLastReplOutput("42");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/801")]
        public void VerifyCodeActionsAvailableInCurrentSubmission()
        {
            this.InsertCode("Directory.Exists(\"foo\");");
            this.VerifyCodeActions(
                new string[] { "using System.IO;", "System.IO.Directory"}, 
                "using System.IO;");
            this.VerifyLastReplInput(@"using System.IO;
Directory.Exists(""foo"");");            
        }

        [Fact]
        public void VerifyCodeActionsNotAvailableInPreviousSubmission()
        {
            this.InsertCode("Console.WriteLine(42);");
            this.VerifyCodeActionsNotShowing();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/3785")]
        public void SimpleRenameOnLocalsVerifySpansAndResult()
        {
            this.InsertCode(@"static void Main(string[] args)
{
    int loc = 0;
    loc = 5;
    TestMethod(loc);
}

static void TestMethod(int y)
    {

}");

            this.PlaceCaret("loc");
            //      < Rename />
            //      < VerifyRenameTags ExpectedCount="3" SpanLength="3" SpanText="loc">
            //        <SpanStart Capacity = "3" >
            //          < int > 60 </ int >
            //          < int > 74 </ int >
            //          < int > 99 </ int >
            //        </ SpanStart >
            //      </ VerifyRenameTags >
            //      < SendKeys > y{ENTER}</SendKeys>
            //      <!- -Commit On Rename is a blocking operation, however, for the integration test perspective, we are not directly speaking
            //      with the Rename Service.We just send keys to the editor and when control returns we really don't know if Rename has finished.
            //      The below action checks to see if Rename Tags have disappeared which signals the end of a rename session. This isn't required
            //      for editor test app as it appears to be fast enough to beat us before we check for the renamed symbol.- ->
            //      <WaitForRenameCompletion />
            //      <VerifyEditorTextInRange StartPosition = "60" EndPosition= "61" Text = "y" />
            //      < VerifyEditorTextInRange StartPosition= "72" EndPosition= "73" Text = "y" />
            //      < VerifyEditorTextInRange StartPosition= "95" EndPosition= "96" Text = "y" />
            //      < ClearReplText />
            //    </ Scenario >
            //    -->
        }

        [Fact]
        public void VerifyQuickInfoOnStringDocCommentsFromMetadata()
        {
            this.InsertCode("static void Foo(string[] args) { }");
            this.PlaceCaret("[]", charsOffset: -2);
            this.InvokeQuickInfo();
            var s = InteractiveWindow.GetQuickInfo();
            Assert.Equal("class‎ System‎.String", s);
        }

        [Fact]
        public void International()
        {
            this.InsertCode(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);");
            this.PlaceCaret("func", charsOffset: -1);
            this.InvokeQuickInfo();
            var s = InteractiveWindow.GetQuickInfo();
            Assert.Equal("‎(field‎)‎ العربية‎ func", s);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/5013")]
        public void MethodSignatureHelp()
        {
            this.InsertCode("Console.WriteLine(tr");
            //      <InvokeSignatureHelp />
            //      <VerifySignatureHelp>
            //        <ExpectedSelectedSignature>
            //          <Signature Content="void Console.WriteLine(bool value)&#13;&#10;Writes the text representation of the specified Boolean value, followed by the current line terminator, to the standard output stream.">
            //            <CurrentParameter>
            //              <Parameter Name="value" Documentation="The value to write." />
            //            </CurrentParameter>
            //            <Parameters>
            //              <Parameter Name="value" Documentation="The value to write." />
            //            </Parameters>
            //          </Signature>
            //        </ExpectedSelectedSignature>
            //      </VerifySignatureHelp>
            //      <DismissSignatureHelp />
        }

        [Fact]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsShowUpWhenInvokedOnUnsubmittedText()
        {
            this.InsertCode("int someint; someint = 22; someint = 23;");
            this.PlaceCaret("someint = 22", charsOffset: -6);

            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
        }

        [Fact]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsGoAway()
        {
            this.InsertCode("int someint; someint = 22; someint = 23;");
            this.PlaceCaret("someint = 22", charsOffset: -6);
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);

            this.PlaceCaret("22");
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 0);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedText()
        {
            this.SubmitText("class Foo { }");
            this.SubmitText("Foo something = new Foo();");
            this.SubmitText("something.ToString();");
            this.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedText()
        {
            this.SubmitText("class Foo { }");
            this.SubmitText("Foo something = new Foo();");
            this.InsertCode("something.ToString();");
            this.PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedText()
        {
            this.SubmitText("class Foo { }");
            this.SubmitText("Foo a;");
            this.SubmitText("Foo b;");
            this.PlaceCaret("Foo b", charsOffset: -1);
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedText()
        {
            this.SubmitText("class Foo { }");
            this.SubmitText("Foo a;");
            this.InsertCode("Foo b;");
            this.PlaceCaret("Foo b", charsOffset: -1);
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedText()
        {
            this.SubmitText("class Foo { }");
            this.SubmitText("Foo a;");
            this.InsertCode("Foo b;Something();");
            this.PlaceCaret("Something();", charsOffset: -1);
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariable()
        {
            this.SubmitText("string abc = null;");
            this.SubmitText("abc = string.Empty;");
            this.InsertCode("int abc = 42;");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            this.PlaceCaret("abc", occurrence: 3);
            this.WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnownTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/801")]
        public void VerifyCodeActionListNotEmpty()
        {
            // see bug 711467
            this.InsertCode("Process");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            this.VerifyCodeActions(
                new string[] { "using System.Diagnostics;", "System.Diagnostics.Process" },
                "using System.Diagnostics;");
            this.VerifyLastReplInput(@"using System.Diagnostics;
Process");
        }

        [Fact]
        public void DisabledCommandsPart1()
        {
            this.InsertCode(@"public class Class
{
    int field;

    public void Method(int x)
    {
         int abc = 1 + 1;
     }
}");

            this.PlaceCaret("abc");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_Rename));

            this.PlaceCaret("1 + 1");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractMethod));

            this.PlaceCaret("Class");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractInterface));

            this.PlaceCaret("field");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_EncapsulateField));

            this.PlaceCaret("Method");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_RemoveParameters));
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_ReorderParameters));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6587, No support of quick actions in ETA scenario")]
        public void AddUsing()
        {
            this.InsertCode("typeof(ArrayList)");
            this.PlaceCaret("ArrayList");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);

            this.InvokeCodeActionList();
            this.VerifyCodeActions(
                new string[] { "using System.Collections;", "System.Collections.ArrayList" },
                "using System.Collections;");

            this.VerifyLastReplInput(@"using System.Collections;
typeof(ArrayList)");
        }

        [Fact(Skip = "No support of quick actions in ETA scenario")]
        public void QualifyName()
        {
            this.InsertCode("typeof(ArrayList)");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            this.PlaceCaret("ArrayList");
            this.WaitForAsyncOperations(FeatureAttribute.Workspace);
            this.VerifyCodeActions(
    new string[] { "using System.Collections;", "System.Collections.ArrayList" },
    "System.Collections.ArrayList");
            this.VerifyLastReplInput("System.Collections.ArrayList");
        }
    }
}
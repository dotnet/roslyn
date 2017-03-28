// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
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
            Reset();
            base.Dispose();
        }

        // https://github.com/dotnet/roslyn/issues/801
        // [Fact]
        public void VerifyDefaultUsingStatements()
        {
            SubmitText("Console.WriteLine(42);");
            this.WaitForLastReplOutput("42");
        }

        // https://github.com/dotnet/roslyn/issues/801
        // [Fact]
        public void VerifyCodeActionsAvailableInCurrentSubmission()
        {
            InsertCode("Directory.Exists(\"foo\");");
            //      <VerifyCodeActions ApplyFix = "using System.IO;" >
            //        < ExpectedItems >
            //          < string >using System.IO;</string>
            //          <string>System.IO.Directory</string>
            //        </ExpectedItems>
            //      </VerifyCodeActions>
            //      <VerifyReplInput>
            //        <![CDATA[using System.IO;

            //Directory.Exists("foo");]]>
            //      </VerifyReplInput>
        }

        // https://github.com/dotnet/roslyn/issues/801
        [Fact]
        public void VerifyCodeActionsNotAvailableInPreviousSubmission()
        {
            InsertCode("Console.WriteLine(42);");
            VerifyCodeActionsNotShowing();
        }

        //  https://github.com/dotnet/roslyn/issues/3785
        //  [Fact]
        public void SimpleRenameOnLocalsVerifySpansAndResult()
        {
            InsertCode(@"static void Main(string[] args)
{
    int loc = 0;
    loc = 5;
    TestMethod(loc);
}

static void TestMethod(int y)
    {

}");

            PlaceCaret("loc");
            //      <PlaceCursor Marker = "loc" />
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
            InsertCode("static void Foo(string[] args) { }");
            PlaceCaret("[]", charsOffset: -2);
            InvokeQuickInfo();
            var s = InteractiveWindow.GetQuickInfo();
            Assert.Equal("class‎ System‎.String", s);
        }

        [Fact]
        public void International()
        {
            InsertCode(@"delegate void العربية();
العربية func = () => System.Console.WriteLine(2);");
            PlaceCaret("func", charsOffset: -1);
            InvokeQuickInfo();
            var s = InteractiveWindow.GetQuickInfo();
            Assert.Equal("‎(field‎)‎ العربية‎ func", s);
        }

        //   https://github.com/dotnet/roslyn/issues/5013
        [Fact]
        public void MethodSignatureHelp()
        {
            InsertCode("Console.WriteLine(tr");

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
            InsertCode("int someint; someint = 22; someint = 23;");
            PlaceCaret("someint = 22", charsOffset: -6);

            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
        }

        [Fact]
        public void HighlightRefsSingleSubmissionVerifyRenameTagsGoAway()
        {
            InsertCode("int someint; someint = 22; someint = 23;");
            PlaceCaret("someint = 22", charsOffset: -6);
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 2);

            PlaceCaret("22");
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 0);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedWrittenReference, 0);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpWhenInvokedOnSubmittedText()
        {
            SubmitText("class Foo { }");
            SubmitText("Foo something = new Foo();");
            SubmitText("something.ToString();");
            PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnUnsubmittedText()
        {
            SubmitText("class Foo { }");
            SubmitText("Foo something = new Foo();");
            InsertCode("something.ToString();");
            PlaceCaret("someth", charsOffset: 1, occurrence: 2);
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 1);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnSubmittedText()
        {
            SubmitText("class Foo { }");
            SubmitText("Foo a;");
            SubmitText("Foo b;");
            PlaceCaret("Foo b", charsOffset: -1);
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsShowUpOnTypesWhenInvokedOnUnsubmittedText()
        {
            SubmitText("class Foo { }");
            SubmitText("Foo a;");
            InsertCode("Foo b;");
            PlaceCaret("Foo b", charsOffset: -1);
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 2);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsGoAwayWhenInvokedOnUnsubmittedText()
        {
            SubmitText("class Foo { }");
            SubmitText("Foo a;");
            InsertCode("Foo b;Something();");
            PlaceCaret("Something();", charsOffset: -1);
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 0);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        [Fact]
        public void HighlightRefsMultipleSubmisionsVerifyRenameTagsOnRedefinedVariable()
        {
            SubmitText("string abc = null;");
            SubmitText("abc = string.Empty;");
            InsertCode("int abc = 42;");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            PlaceCaret("abc", occurrence: 3);
            WaitForAsyncOperations(FeatureAttribute.ReferenceHighlighting);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedDefinition, 1);
            InteractiveWindow.VerifyTags(WellKnowTagNames.MarkerFormatDefinition_HighlightedReference, 0);
        }

        // https://github.com/dotnet/roslyn/issues/801
        // [Fact]
        public void VerifyCodeActionListNotEmpty()
        {
            // see bug 711467
            InsertCode("Process");

            //      < VerifyCodeActions ApplyFix="using System.Diagnostics;">
            //        <ExpectedItems>
            //          <string>using System.Diagnostics;</string>
            //          <string>System.Diagnostics.Process</string>
            //        </ExpectedItems>
            //      </VerifyCodeActions>
            //      <VerifyReplInput>
            //        <![CDATA[using System.Diagnostics;

            //Process]]>
            //      </VerifyReplInput>
        }

        [Fact]
        public void DisabledCommandsPart1()
        {
            InsertCode(@"public class Class
{
    int field;

    public void Method(int x)
    {
         int abc = 1 + 1;
     }
}");

            PlaceCaret("abc");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_Rename));

            PlaceCaret("1 + 1");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractMethod));

            PlaceCaret("Class");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_ExtractInterface));

            PlaceCaret("field");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_EncapsulateField));

            PlaceCaret("Method");
            WaitForAsyncOperations(FeatureAttribute.Workspace);
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_RemoveParameters));
            Assert.False(VisualStudio.Instance.IsCommandAvailable(WellKnownCommandNames.Refactor_ReorderParameters));
        }

        // https://github.com/dotnet/roslyn/issues/6587
        // No support of quick actions in ETA scenario
        // [Fact]
        public void AddUsing()
        {
            InsertCode("typeof(ArrayList)");
            PlaceCaret("ArrayList");
            WaitForAsyncOperations(FeatureAttribute.Workspace);

            InvokeCodeActionList();
            VerifyCodeActions(
                new string[] { "using System.Collections;", "System.Collections.ArrayList" },
                "using System.Collections;");

            VerifyLastReplInput(@"using System.Collections;
typeof(ArrayList)");
        }

        // No support of quick actions in ETA scenario
        // [Fact]
        public void QualifyName()
        {
            //      < !- -ETA doesn't handle ctrl-. in tool windows - ->
            //      <ExcludeScenarioInHost HostName = "ETA" />
            InsertCode("typeof(ArrayList)");

            //      < WaitForWorkspace />
            PlaceCaret("ArrayList");

            //      <WaitForWorkspace />
            //      <VerifyCodeActions ApplyFix = "System.Collections.ArrayList" >
            //        < ExpectedItems >
            //          < string >using System.Collections;</string>
            //          <string>System.Collections.ArrayList</string>
            //        </ExpectedItems>
            //      </VerifyCodeActions>
            //      <VerifyReplInput>
            //        <![CDATA[typeof(System.Collections.ArrayList)]]>
            //      </VerifyReplInput>
            //    </Scenario>
            //    -->
            //  </ScenarioList>

            //  <CleanupScenario>
            //    <SendKeys>{ESC}{ESC}{ESC}</SendKeys>
            //    <SubmitReplText>#cls</SubmitReplText>
            //  </CleanupScenario>
        }
    }
}
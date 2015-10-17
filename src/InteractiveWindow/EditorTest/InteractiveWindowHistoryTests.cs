// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class InteractiveWindowHistoryTests : InteractiveWindowTestBase
    {
        #region Helpers

        private readonly InteractiveWindowTestHost _testHost;    

        public InteractiveWindowHistoryTests()
        {
            _testHost = new InteractiveWindowTestHost();  
        }

        internal override InteractiveWindowTestHost TestHost => _testHost;

        public override void Dispose()
        {
            _testHost.Dispose();
        }

        /// <summary>
        /// Sets the active code to the specified text w/o executing it.
        /// </summary>
        private void SetActiveCode(string text)
        {
            using (var edit = Window.CurrentLanguageBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
            {
                edit.Replace(new Span(0, Window.CurrentLanguageBuffer.CurrentSnapshot.Length), text);
                edit.Apply();
            }
        }

        private void InsertAndExecuteInputs(params string[] inputs)
        {
            foreach (var input in inputs)
            {
                InsertAndExecuteInput(input);
            }
        }

        private void InsertAndExecuteInput(string input)
        {
            Window.InsertCode(input);
            AssertCurrentSubmission(input);
            ExecuteInput();
        }

        private void ExecuteInput()
        {
            ((InteractiveWindow)Window).ExecuteInputAsync().PumpingWait();
        }

        private void AssertCurrentSubmission(string expected)
        {
            Assert.Equal(expected, Window.CurrentLanguageBuffer.CurrentSnapshot.GetText());
        }

        #endregion Helpers    

        [WpfFact]
        public void CallClearHistoryOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            TaskRun(() => Window.Operations.ClearHistory()).PumpingWait();
        }

        [WpfFact]
        public void CallHistoryNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            TaskRun(() => Window.Operations.HistoryNext()).PumpingWait();
        }

        [WpfFact]
        public void CallHistoryPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            TaskRun(() => Window.Operations.HistoryPrevious()).PumpingWait();
        }

        [WpfFact]
        public void CallHistorySearchNextOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            TaskRun(() => Window.Operations.HistorySearchNext()).PumpingWait();
        }

        [WpfFact]
        public void CallHistorySearchPreviousOnNonUIThread()
        {
            Window.AddInput("1"); // Need a history entry.
            TaskRun(() => Window.Operations.HistorySearchPrevious()).PumpingWait();
        }

        [WpfFact]
        public void CheckHistoryPrevious()
        {
            const string inputString = "1 ";
            InsertAndExecuteInput(inputString);
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString);
        }

        [WpfFact]
        public void CheckHistoryPreviousNotCircular()
        {
            //submit, submit, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
            //this up should not be circular
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
        }

        [WpfFact]
        public void CheckHistoryPreviousAfterSubmittingEntryFromHistory()
        {
            //submit, submit, submit, up, up, submit, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);
            InsertAndExecuteInput(inputString3);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            ExecuteInput();

            //history navigation should start from the last history pointer
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            //has reached the top, no change
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
        }

        [WpfFact]
        public void CheckHistoryPreviousAfterSubmittingNewEntryWhileNavigatingHistory()
        {
            //submit, submit, up, up, submit new, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            SetActiveCode(inputString3);
            AssertCurrentSubmission(inputString3);
            ExecuteInput();

            //History pointer should be reset. Previous should now bring up last entry
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            //has reached the top, no change
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
        }

        [WpfFact]
        public void CheckHistoryNextNotCircular()
        {
            //submit, submit, down, up, down, down
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string empty = "";
            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            //Next should do nothing as history pointer is uninitialized and there is
            //no next entry. Buffer should be empty
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(empty);

            //Go back once entry
            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            //Go fwd one entry - should do nothing as history pointer is at last entry
            //buffer should have same value as before
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(inputString2);

            //Next should again do nothing as it is the last item, bufer should have the same value
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(inputString2);

            //This is to make sure the window doesn't crash
            ExecuteInput();
            AssertCurrentSubmission(empty);
        }

        [WpfFact]
        public void CheckHistoryNextAfterSubmittingEntryFromHistory()
        {
            //submit, submit, submit, up, up, submit, down, down, down
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);
            InsertAndExecuteInput(inputString3);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            //submit inputString2 again. Should be added at the end of history
            ExecuteInput();

            //history navigation should start from the last history pointer
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(inputString3);

            //This next should take us to the InputString2 which was resubmitted
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(inputString2);

            //has reached the top, no change
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(inputString2);
        }

        [WpfFact]
        public void CheckHistoryNextAfterSubmittingNewEntryWhileNavigatingHistory()
        {
            //submit, submit, up, up, submit new, down, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";
            const string empty = "";

            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            SetActiveCode(inputString3);
            AssertCurrentSubmission(inputString3);
            ExecuteInput();

            //History pointer should be reset. next should do nothing
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(empty);

            Window.Operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);
        }

        [WpfFact]
        public void CheckUncommittedInputAfterNavigatingHistory()
        {
            //submit, submit, up, up, submit new, down, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string uncommittedInput = "uncommittedInput";

            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);
            //Add uncommitted input
            SetActiveCode(uncommittedInput);
            //Navigate history. This should save uncommitted input
            Window.Operations.HistoryPrevious();
            //Navigate to next item at the end of history.
            //This should bring back uncommitted input
            Window.Operations.HistoryNext();
            AssertCurrentSubmission(uncommittedInput);
        }

        [WpfFact]
        public void CheckHistoryPreviousAfterReset()
        {
            const string resetCommand1 = "#reset";
            const string resetCommand2 = "#reset  ";
            InsertAndExecuteInput(resetCommand1);
            InsertAndExecuteInput(resetCommand2);
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission(resetCommand2);
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission(resetCommand1);
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission(resetCommand1);
        }

        [WpfFact]
        public void TestHistoryPrevious()
        {
            InsertAndExecuteInputs("1", "2", "3");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("3");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("2");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
        }

        [WpfFact]
        public void TestHistoryNext()
        {
            InsertAndExecuteInputs("1", "2", "3");

            SetActiveCode("4");

            Window.Operations.HistoryNext();      AssertCurrentSubmission("4");
            Window.Operations.HistoryNext();      AssertCurrentSubmission("4");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("3");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("2");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryNext();      AssertCurrentSubmission("2");
            Window.Operations.HistoryNext();      AssertCurrentSubmission("3");
            Window.Operations.HistoryNext();      AssertCurrentSubmission("4");
            Window.Operations.HistoryNext();      AssertCurrentSubmission("4");
        }

        [WpfFact]
        public void TestHistoryPreviousWithPattern_NoMatch()
        {
            InsertAndExecuteInputs("123", "12", "1");

            Window.Operations.HistoryPrevious("4");   AssertCurrentSubmission("");
            Window.Operations.HistoryPrevious("4");   AssertCurrentSubmission("");
        }

        [WpfFact]
        public void TestHistoryPreviousWithPattern_PatternMaintained()
        {
            InsertAndExecuteInputs("123", "12", "1");

            Window.Operations.HistoryPrevious("12");  AssertCurrentSubmission("12"); // Skip over non-matching entry.
            Window.Operations.HistoryPrevious("12");  AssertCurrentSubmission("123");
            Window.Operations.HistoryPrevious("12");  AssertCurrentSubmission("123");
        }

        [WpfFact]
        public void TestHistoryPreviousWithPattern_PatternDropped()
        {
            InsertAndExecuteInputs("1", "2", "3");

            Window.Operations.HistoryPrevious("2");   AssertCurrentSubmission("2"); // Skip over non-matching entry.
            Window.Operations.HistoryPrevious(null);  AssertCurrentSubmission("1"); // Pattern isn't passed, so return to normal iteration.
            Window.Operations.HistoryPrevious(null);  AssertCurrentSubmission("1");
        }

        [WpfFact]
        public void TestHistoryPreviousWithPattern_PatternChanged()
        {
            InsertAndExecuteInputs("10", "20", "15", "25");

            Window.Operations.HistoryPrevious("1");   AssertCurrentSubmission("15"); // Skip over non-matching entry.
            Window.Operations.HistoryPrevious("2");   AssertCurrentSubmission("20"); // Skip over non-matching entry.
            Window.Operations.HistoryPrevious("2");   AssertCurrentSubmission("20");
        }

        [WpfFact]
        public void TestHistoryNextWithPattern_NoMatch()
        {
            InsertAndExecuteInputs("start", "1", "12", "123");
            SetActiveCode("end");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("123");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("12");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("start");

            Window.Operations.HistoryNext("4");   AssertCurrentSubmission("end");
            Window.Operations.HistoryNext("4");   AssertCurrentSubmission("end");
        }

        [WpfFact]
        public void TestHistoryNextWithPattern_PatternMaintained()
        {
            InsertAndExecuteInputs("start", "1", "12", "123");
            SetActiveCode("end");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("123");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("12");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("start");

            Window.Operations.HistoryNext("12");  AssertCurrentSubmission("12"); // Skip over non-matching entry.
            Window.Operations.HistoryNext("12");  AssertCurrentSubmission("123");
            Window.Operations.HistoryNext("12");  AssertCurrentSubmission("end");
        }

        [WpfFact]
        public void TestHistoryNextWithPattern_PatternDropped()
        {
            InsertAndExecuteInputs("start", "3", "2", "1");
            SetActiveCode("end");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("2");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("3");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("start");

            Window.Operations.HistoryNext("2");   AssertCurrentSubmission("2"); // Skip over non-matching entry.
            Window.Operations.HistoryNext(null);  AssertCurrentSubmission("1"); // Pattern isn't passed, so return to normal iteration.
            Window.Operations.HistoryNext(null);  AssertCurrentSubmission("end");
        }

        [WpfFact]
        public void TestHistoryNextWithPattern_PatternChanged()
        {
            InsertAndExecuteInputs("start", "25", "15", "20", "10");
            SetActiveCode("end");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("10");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("20");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("15");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("25");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("start");

            Window.Operations.HistoryNext("1");   AssertCurrentSubmission("15"); // Skip over non-matching entry.
            Window.Operations.HistoryNext("2");   AssertCurrentSubmission("20"); // Skip over non-matching entry.
            Window.Operations.HistoryNext("2");   AssertCurrentSubmission("end");
        }

        [WpfFact]
        public void TestHistorySearchPrevious()
        {
            InsertAndExecuteInputs("123", "12", "1");

            // Default search string is empty.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("1"); // Pattern is captured before this step.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("12");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
        }

        [WpfFact]
        public void TestHistorySearchPreviousWithPattern()
        {
            InsertAndExecuteInputs("123", "12", "1");
            SetActiveCode("12");

            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("12"); // Pattern is captured before this step.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
        }

        [WpfFact]
        public void TestHistorySearchNextWithPattern()
        {
            InsertAndExecuteInputs("12", "123", "12", "1");
            SetActiveCode("end");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("12");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("123");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("12");

            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("123"); // Pattern is captured before this step.
            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("12");
            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("end");
        }

        [WpfFact]
        public void TestHistoryPreviousAndSearchPrevious()
        {
            InsertAndExecuteInputs("200", "100", "30", "20", "10", "2", "1");

            Window.Operations.HistoryPrevious();          AssertCurrentSubmission("1");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            Window.Operations.HistoryPrevious();          AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("100");
            Window.Operations.HistoryPrevious();          AssertCurrentSubmission("200");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("200"); // No-op results in non-matching history entry after SearchPrevious.
        }

        [WpfFact]
        public void TestHistoryPreviousAndSearchPrevious_ExplicitPattern()
        {
            InsertAndExecuteInputs("200", "100", "30", "20", "10", "2", "1");

            Window.Operations.HistoryPrevious();          AssertCurrentSubmission("1");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            Window.Operations.HistoryPrevious("2");       AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("100");
            Window.Operations.HistoryPrevious("2");       AssertCurrentSubmission("200");
            Window.Operations.HistorySearchPrevious();    AssertCurrentSubmission("200"); // No-op results in non-matching history entry after SearchPrevious.
        }

        [WpfFact]
        public void TestHistoryNextAndSearchNext()
        {
            InsertAndExecuteInputs("1", "2", "10", "20", "30", "100", "200");
            SetActiveCode("4");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("200");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("100");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("30");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("20");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("10");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("2");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");

            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            Window.Operations.HistoryNext();          AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("4"); // Restoring input results in non-matching history entry after SearchNext.
            Window.Operations.HistoryNext();          AssertCurrentSubmission("4");
        }

        [WpfFact]
        public void TestHistoryNextAndSearchNext_ExplicitPattern()
        {
            InsertAndExecuteInputs("1", "2", "10", "20", "30", "100", "200");
            SetActiveCode("4");

            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("200");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("100");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("30");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("20");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("10");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("2");
            Window.Operations.HistoryPrevious();  AssertCurrentSubmission("1");

            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            Window.Operations.HistoryNext("2");       AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            Window.Operations.HistorySearchNext();    AssertCurrentSubmission("4"); // Restoring input results in non-matching history entry after SearchNext.
            Window.Operations.HistoryNext("2");       AssertCurrentSubmission("4");
        }
    }
}
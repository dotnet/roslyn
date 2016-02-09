// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class InteractiveWindowHistoryTests : IDisposable
    {
        #region Helpers

        private readonly InteractiveWindowTestHost _testHost;
        private readonly IInteractiveWindow _window;
        private readonly IInteractiveWindowOperations _operations;

        public InteractiveWindowHistoryTests()
        {
            _testHost = new InteractiveWindowTestHost();
            _window = _testHost.Window;
            _operations = _window.Operations;
        }

        void IDisposable.Dispose()
        {
            _testHost.Dispose();
        }

        /// <summary>
        /// Sets the active code to the specified text w/o executing it.
        /// </summary>
        private void SetActiveCode(string text)
        {
            using (var edit = _window.CurrentLanguageBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, editTag: null))
            {
                edit.Replace(new Span(0, _window.CurrentLanguageBuffer.CurrentSnapshot.Length), text);
                edit.Apply();
            }
        }

        private async Task InsertAndExecuteInputs(params string[] inputs)
        {
            foreach (var input in inputs)
            {
                await InsertAndExecuteInput(input).ConfigureAwait(true);
            }
        }

        private async Task InsertAndExecuteInput(string input)
        {
            _window.InsertCode(input);
            AssertCurrentSubmission(input);
            await ExecuteInput().ConfigureAwait(true);
        }

        private async Task ExecuteInput()
        {
            await ((InteractiveWindow)_window).ExecuteInputAsync().ConfigureAwait(true);
        }

        private void AssertCurrentSubmission(string expected)
        {
            Assert.Equal(expected, _window.CurrentLanguageBuffer.CurrentSnapshot.GetText());
        }

        #endregion Helpers

        [WpfFact]
        public async Task CheckHistoryPrevious()
        {
            const string inputString = "1 ";
            await InsertAndExecuteInput(inputString).ConfigureAwait(true);
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString);
        }

        [WpfFact]
        public async Task CheckHistoryPreviousNotCircular()
        {
            //submit, submit, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
            //this up should not be circular
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
        }

        [WpfFact]
        public async Task CheckHistoryPreviousAfterSubmittingEntryFromHistory()
        {
            //submit, submit, submit, up, up, submit, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString3).ConfigureAwait(true);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            await ExecuteInput().ConfigureAwait(true);

            //history navigation should start from the last history pointer
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            //has reached the top, no change
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
        }

        [WpfFact]
        public async Task CheckHistoryPreviousAfterSubmittingNewEntryWhileNavigatingHistory()
        {
            //submit, submit, up, up, submit new, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            SetActiveCode(inputString3);
            AssertCurrentSubmission(inputString3);
            await ExecuteInput().ConfigureAwait(true);

            //History pointer should be reset. Previous should now bring up last entry
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            //has reached the top, no change
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);
        }

        [WpfFact]
        public async Task CheckHistoryNextNotCircular()
        {
            //submit, submit, down, up, down, down
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string empty = "";
            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);

            //Next should do nothing as history pointer is uninitialized and there is
            //no next entry. Buffer should be empty
            _operations.HistoryNext();
            AssertCurrentSubmission(empty);

            //Go back once entry
            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            //Go fwd one entry - should do nothing as history pointer is at last entry
            //buffer should have same value as before
            _operations.HistoryNext();
            AssertCurrentSubmission(inputString2);

            //Next should again do nothing as it is the last item, buffer should have the same value
            _operations.HistoryNext();
            AssertCurrentSubmission(inputString2);

            //This is to make sure the window doesn't crash
            await ExecuteInput().ConfigureAwait(true);
            AssertCurrentSubmission(empty);
        }

        [WpfFact]
        public async Task CheckHistoryNextAfterSubmittingEntryFromHistory()
        {
            //submit, submit, submit, up, up, submit, down, down, down
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString3).ConfigureAwait(true);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            //submit inputString2 again. Should be added at the end of history
            await ExecuteInput().ConfigureAwait(true);

            //history navigation should start from the last history pointer
            _operations.HistoryNext();
            AssertCurrentSubmission(inputString3);

            //This next should take us to the InputString2 which was resubmitted
            _operations.HistoryNext();
            AssertCurrentSubmission(inputString2);

            //has reached the top, no change
            _operations.HistoryNext();
            AssertCurrentSubmission(inputString2);
        }

        [WpfFact]
        public async Task CheckHistoryNextAfterSubmittingNewEntryWhileNavigatingHistory()
        {
            //submit, submit, up, up, submit new, down, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";
            const string empty = "";

            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString2);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString1);

            SetActiveCode(inputString3);
            AssertCurrentSubmission(inputString3);
            await ExecuteInput().ConfigureAwait(true);

            //History pointer should be reset. next should do nothing
            _operations.HistoryNext();
            AssertCurrentSubmission(empty);

            _operations.HistoryPrevious();
            AssertCurrentSubmission(inputString3);
        }

        [WpfFact]
        public async Task CheckUncommittedInputAfterNavigatingHistory()
        {
            //submit, submit, up, up, submit new, down, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string uncommittedInput = nameof(uncommittedInput);

            await InsertAndExecuteInput(inputString1).ConfigureAwait(true);
            await InsertAndExecuteInput(inputString2).ConfigureAwait(true);
            //Add uncommitted input
            SetActiveCode(uncommittedInput);
            //Navigate history. This should save uncommitted input
            _operations.HistoryPrevious();
            //Navigate to next item at the end of history.
            //This should bring back uncommitted input
            _operations.HistoryNext();
            AssertCurrentSubmission(uncommittedInput);
        }

        [WpfFact]
        public async Task CheckHistoryPreviousAfterReset()
        {
            const string resetCommand1 = "#reset";
            const string resetCommand2 = "#reset  ";
            await InsertAndExecuteInput(resetCommand1).ConfigureAwait(true);
            await InsertAndExecuteInput(resetCommand2).ConfigureAwait(true);
            _operations.HistoryPrevious();  AssertCurrentSubmission(resetCommand2);
            _operations.HistoryPrevious();  AssertCurrentSubmission(resetCommand1);
            _operations.HistoryPrevious();  AssertCurrentSubmission(resetCommand1);
        }

        [WpfFact]
        public async Task TestHistoryPrevious()
        {
            await InsertAndExecuteInputs("1", "2", "3").ConfigureAwait(true);

            _operations.HistoryPrevious();  AssertCurrentSubmission("3");
            _operations.HistoryPrevious();  AssertCurrentSubmission("2");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
        }

        [WpfFact]
        public async Task TestHistoryNext()
        {
            await InsertAndExecuteInputs("1", "2", "3").ConfigureAwait(true);

            SetActiveCode("4");

            _operations.HistoryNext();      AssertCurrentSubmission("4");
            _operations.HistoryNext();      AssertCurrentSubmission("4");
            _operations.HistoryPrevious();  AssertCurrentSubmission("3");
            _operations.HistoryPrevious();  AssertCurrentSubmission("2");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryNext();      AssertCurrentSubmission("2");
            _operations.HistoryNext();      AssertCurrentSubmission("3");
            _operations.HistoryNext();      AssertCurrentSubmission("4");
            _operations.HistoryNext();      AssertCurrentSubmission("4");
        }

        [WpfFact]
        public async Task TestHistoryPreviousWithPattern_NoMatch()
        {
            await InsertAndExecuteInputs("123", "12", "1").ConfigureAwait(true);

            _operations.HistoryPrevious("4");   AssertCurrentSubmission("");
            _operations.HistoryPrevious("4");   AssertCurrentSubmission("");
        }

        [WpfFact]
        public async Task TestHistoryPreviousWithPattern_PatternMaintained()
        {
            await InsertAndExecuteInputs("123", "12", "1").ConfigureAwait(true);

            _operations.HistoryPrevious("12");  AssertCurrentSubmission("12"); // Skip over non-matching entry.
            _operations.HistoryPrevious("12");  AssertCurrentSubmission("123");
            _operations.HistoryPrevious("12");  AssertCurrentSubmission("123");
        }

        [WpfFact]
        public async Task TestHistoryPreviousWithPattern_PatternDropped()
        {
            await InsertAndExecuteInputs("1", "2", "3").ConfigureAwait(true);

            _operations.HistoryPrevious("2");   AssertCurrentSubmission("2"); // Skip over non-matching entry.
            _operations.HistoryPrevious(null);  AssertCurrentSubmission("1"); // Pattern isn't passed, so return to normal iteration.
            _operations.HistoryPrevious(null);  AssertCurrentSubmission("1");
        }

        [WpfFact]
        public async Task TestHistoryPreviousWithPattern_PatternChanged()
        {
            await InsertAndExecuteInputs("10", "20", "15", "25").ConfigureAwait(true);

            _operations.HistoryPrevious("1");   AssertCurrentSubmission("15"); // Skip over non-matching entry.
            _operations.HistoryPrevious("2");   AssertCurrentSubmission("20"); // Skip over non-matching entry.
            _operations.HistoryPrevious("2");   AssertCurrentSubmission("20");
        }

        [WpfFact]
        public async Task TestHistoryNextWithPattern_NoMatch()
        {
            await InsertAndExecuteInputs("start", "1", "12", "123").ConfigureAwait(true);
            SetActiveCode("end");

            _operations.HistoryPrevious();  AssertCurrentSubmission("123");
            _operations.HistoryPrevious();  AssertCurrentSubmission("12");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("start");

            _operations.HistoryNext("4");   AssertCurrentSubmission("end");
            _operations.HistoryNext("4");   AssertCurrentSubmission("end");
        }

        [WpfFact]
        public async Task TestHistoryNextWithPattern_PatternMaintained()
        {
            await InsertAndExecuteInputs("start", "1", "12", "123").ConfigureAwait(true);
            SetActiveCode("end");

            _operations.HistoryPrevious();  AssertCurrentSubmission("123");
            _operations.HistoryPrevious();  AssertCurrentSubmission("12");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("start");

            _operations.HistoryNext("12");  AssertCurrentSubmission("12"); // Skip over non-matching entry.
            _operations.HistoryNext("12");  AssertCurrentSubmission("123");
            _operations.HistoryNext("12");  AssertCurrentSubmission("end");
        }

        [WpfFact]
        public async Task TestHistoryNextWithPattern_PatternDropped()
        {
            await InsertAndExecuteInputs("start", "3", "2", "1").ConfigureAwait(true);
            SetActiveCode("end");

            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("2");
            _operations.HistoryPrevious();  AssertCurrentSubmission("3");
            _operations.HistoryPrevious();  AssertCurrentSubmission("start");

            _operations.HistoryNext("2");   AssertCurrentSubmission("2"); // Skip over non-matching entry.
            _operations.HistoryNext(null);  AssertCurrentSubmission("1"); // Pattern isn't passed, so return to normal iteration.
            _operations.HistoryNext(null);  AssertCurrentSubmission("end");
        }

        [WpfFact]
        public async Task TestHistoryNextWithPattern_PatternChanged()
        {
            await InsertAndExecuteInputs("start", "25", "15", "20", "10").ConfigureAwait(true);
            SetActiveCode("end");

            _operations.HistoryPrevious();  AssertCurrentSubmission("10");
            _operations.HistoryPrevious();  AssertCurrentSubmission("20");
            _operations.HistoryPrevious();  AssertCurrentSubmission("15");
            _operations.HistoryPrevious();  AssertCurrentSubmission("25");
            _operations.HistoryPrevious();  AssertCurrentSubmission("start");

            _operations.HistoryNext("1");   AssertCurrentSubmission("15"); // Skip over non-matching entry.
            _operations.HistoryNext("2");   AssertCurrentSubmission("20"); // Skip over non-matching entry.
            _operations.HistoryNext("2");   AssertCurrentSubmission("end");
        }

        [WpfFact]
        public async Task TestHistorySearchPrevious()
        {
            await InsertAndExecuteInputs("123", "12", "1").ConfigureAwait(true);

            // Default search string is empty.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("1"); // Pattern is captured before this step.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("12");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
        }

        [WpfFact]
        public async Task TestHistorySearchPreviousWithPattern()
        {
            await InsertAndExecuteInputs("123", "12", "1").ConfigureAwait(true);
            SetActiveCode("12");

            _operations.HistorySearchPrevious();    AssertCurrentSubmission("12"); // Pattern is captured before this step.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("123");
        }

        [WpfFact]
        public async Task TestHistorySearchNextWithPattern()
        {
            await InsertAndExecuteInputs("12", "123", "12", "1").ConfigureAwait(true);
            SetActiveCode("end");

            _operations.HistoryPrevious();  AssertCurrentSubmission("1");
            _operations.HistoryPrevious();  AssertCurrentSubmission("12");
            _operations.HistoryPrevious();  AssertCurrentSubmission("123");
            _operations.HistoryPrevious();  AssertCurrentSubmission("12");

            _operations.HistorySearchNext();    AssertCurrentSubmission("123"); // Pattern is captured before this step.
            _operations.HistorySearchNext();    AssertCurrentSubmission("12");
            _operations.HistorySearchNext();    AssertCurrentSubmission("end");
        }

        [WpfFact]
        public async Task TestHistoryPreviousAndSearchPrevious()
        {
            await InsertAndExecuteInputs("200", "100", "30", "20", "10", "2", "1").ConfigureAwait(true);

            _operations.HistoryPrevious();          AssertCurrentSubmission("1");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            _operations.HistoryPrevious();          AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("100");
            _operations.HistoryPrevious();          AssertCurrentSubmission("200");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("200"); // No-op results in non-matching history entry after SearchPrevious.
        }

        [WpfFact]
        public async Task TestHistoryPreviousAndSearchPrevious_ExplicitPattern()
        {
            await InsertAndExecuteInputs("200", "100", "30", "20", "10", "2", "1").ConfigureAwait(true);

            _operations.HistoryPrevious();          AssertCurrentSubmission("1");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            _operations.HistoryPrevious("2");       AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("100");
            _operations.HistoryPrevious("2");       AssertCurrentSubmission("200");
            _operations.HistorySearchPrevious();    AssertCurrentSubmission("200"); // No-op results in non-matching history entry after SearchPrevious.
        }

        [WpfFact]
        public async Task TestHistoryNextAndSearchNext()
        {
            await InsertAndExecuteInputs("1", "2", "10", "20", "30", "100", "200").ConfigureAwait(true);
            SetActiveCode("4");

            _operations.HistoryPrevious();  AssertCurrentSubmission("200");
            _operations.HistoryPrevious();  AssertCurrentSubmission("100");
            _operations.HistoryPrevious();  AssertCurrentSubmission("30");
            _operations.HistoryPrevious();  AssertCurrentSubmission("20");
            _operations.HistoryPrevious();  AssertCurrentSubmission("10");
            _operations.HistoryPrevious();  AssertCurrentSubmission("2");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");

            _operations.HistorySearchNext();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            _operations.HistoryNext();          AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            _operations.HistorySearchNext();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            _operations.HistorySearchNext();    AssertCurrentSubmission("4"); // Restoring input results in non-matching history entry after SearchNext.
            _operations.HistoryNext();          AssertCurrentSubmission("4");
        }

        [WpfFact]
        public async Task TestHistoryNextAndSearchNext_ExplicitPattern()
        {
            await InsertAndExecuteInputs("1", "2", "10", "20", "30", "100", "200").ConfigureAwait(true);
            SetActiveCode("4");

            _operations.HistoryPrevious();  AssertCurrentSubmission("200");
            _operations.HistoryPrevious();  AssertCurrentSubmission("100");
            _operations.HistoryPrevious();  AssertCurrentSubmission("30");
            _operations.HistoryPrevious();  AssertCurrentSubmission("20");
            _operations.HistoryPrevious();  AssertCurrentSubmission("10");
            _operations.HistoryPrevious();  AssertCurrentSubmission("2");
            _operations.HistoryPrevious();  AssertCurrentSubmission("1");

            _operations.HistorySearchNext();    AssertCurrentSubmission("10"); // Pattern is captured before this step.
            _operations.HistoryNext("2");       AssertCurrentSubmission("20"); // NB: Doesn't match pattern.
            _operations.HistorySearchNext();    AssertCurrentSubmission("100"); // NB: Reuses existing pattern.
            _operations.HistorySearchNext();    AssertCurrentSubmission("4"); // Restoring input results in non-matching history entry after SearchNext.
            _operations.HistoryNext("2");       AssertCurrentSubmission("4");
        }
    }
}
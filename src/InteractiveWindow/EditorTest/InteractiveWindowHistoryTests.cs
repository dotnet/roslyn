// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class InteractiveWindowHistoryTests : IDisposable
    {
        #region Helpers

        private InteractiveWindowTestHost _testHost;

        public InteractiveWindowHistoryTests()
        {
            _testHost = new InteractiveWindowTestHost();
        }

        void IDisposable.Dispose()
        {
            _testHost.Dispose();
        }

        private IInteractiveWindow Window => _testHost.Window;

        private string GetTextFromCurrentLanguageBuffer()
        {
            return Window.CurrentLanguageBuffer.CurrentSnapshot.GetText();
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
            Assert.Equal(input, GetTextFromCurrentLanguageBuffer());
            ExecuteInput();
        }

        private void ExecuteInput()
        {
            ((InteractiveWindow)Window).ExecuteInputAsync().PumpingWait();
        }

        private void Test(params Step[] steps)
        {
            int i = 0;
            foreach (var step in steps)
            {
                step.Action();
                var actual = GetTextFromCurrentLanguageBuffer();
                var expected = step.ExpectedText;
                if (expected != actual)
                {
                    Assert.False(true, $"Step {i}: expected '{expected ?? "null"}', but found '{actual ?? "null"}'");
                }
                i++;
            }
        }

        private struct Step
        {
            public readonly Action Action;
            public readonly string ExpectedText;

            public Step(Action action, string expectedText)
            {
                Action = action;
                ExpectedText = expectedText;
            }
        }

        #endregion Helpers

        [Fact]
        public void CheckHistoryPrevious()
        {
            const string inputString = "1 ";
            InsertAndExecuteInput(inputString);
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
        public void CheckHistoryPreviousNotCircular()
        {
            //submit, submit, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());
            //this up should not be circular
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
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
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            ExecuteInput();

            //history navigation should start from the last history pointer
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());

            //has reached the top, no change
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
        public void CheckHistoryPreviousAfterSubmittingNewEntryWhileNavigatingHistory()
        {
            //submit, submit, up, up, submit new, up, up, up
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string inputString3 = "3 ";

            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());

            SetActiveCode(inputString3);
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());
            ExecuteInput();

            //History pointer should be reset. Previous should now bring up last entry
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());

            //has reached the top, no change
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());
        }

        public void CheckHistoryNextNotCircular()
        {
            //submit, submit, down, up, down, down
            const string inputString1 = "1 ";
            const string inputString2 = "2 ";
            const string empty = "";
            InsertAndExecuteInput(inputString1);
            InsertAndExecuteInput(inputString2);

            //Next should do nothing as history pointer is uninitialized and there is
            //no next entry. Bufer should be empty
            Window.Operations.HistoryNext();
            Assert.Equal(empty, GetTextFromCurrentLanguageBuffer());

            //Go back once entry
            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            //Go fwd one entry - should do nothing as history pointer is at last entry
            //buffer should have same value as before
            Window.Operations.HistoryNext();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            //Next should again do nothing as it is the last item, bufer should have the same value
            Window.Operations.HistoryNext();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
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
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            //submit inputString2 again. Should be added at the end of history
            ExecuteInput();

            //history navigation should start from the last history pointer
            Window.Operations.HistoryNext();
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());

            //This next should take us to the InputString2 which was resubmitted
            Window.Operations.HistoryNext();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            //has reached the top, no change
            Window.Operations.HistoryNext();
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
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
            Assert.Equal(inputString2, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString1, GetTextFromCurrentLanguageBuffer());

            SetActiveCode(inputString3);
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());
            ExecuteInput();

            //History pointer should be reset. next should do nothing
            Window.Operations.HistoryNext();
            Assert.Equal(empty, GetTextFromCurrentLanguageBuffer());

            Window.Operations.HistoryPrevious();
            Assert.Equal(inputString3, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
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
            Assert.Equal(uncommittedInput, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
        public void CheckHistoryPreviousAfterReset()
        {
            const string resetCommand1 = "#reset";
            const string resetCommand2 = "#reset  ";
            InsertAndExecuteInput(resetCommand1);
            InsertAndExecuteInput(resetCommand2);
            Window.Operations.HistoryPrevious();
            Assert.Equal(resetCommand2, GetTextFromCurrentLanguageBuffer());
            Window.Operations.HistoryPrevious();
            Assert.Equal(resetCommand1, GetTextFromCurrentLanguageBuffer());
            Window.Operations.HistoryPrevious();
            Assert.Equal(resetCommand1, GetTextFromCurrentLanguageBuffer());
        }

        [Fact]
        public void TestHistoryPrevious()
        {
            InsertAndExecuteInputs("1", "2", "3");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "3"),
                new Step(() => Window.Operations.HistoryPrevious(), "2"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"));
        }

        [Fact]
        public void TestHistoryNext()
        {
            InsertAndExecuteInputs("1", "2", "3");

            SetActiveCode("4");

            Test(
                new Step(() => Window.Operations.HistoryNext(), "4"),
                new Step(() => Window.Operations.HistoryNext(), "4"),
                new Step(() => Window.Operations.HistoryPrevious(), "3"),
                new Step(() => Window.Operations.HistoryPrevious(), "2"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryNext(), "2"),
                new Step(() => Window.Operations.HistoryNext(), "3"),
                new Step(() => Window.Operations.HistoryNext(), "4"),
                new Step(() => Window.Operations.HistoryNext(), "4"));
        }

        [Fact]
        public void TestHistoryPreviousWithPattern_NoMatch()
        {
            InsertAndExecuteInputs("123", "12", "1");

            Test(
                new Step(() => Window.Operations.HistoryPrevious("4"), ""),
                new Step(() => Window.Operations.HistoryPrevious("4"), ""));
        }

        [Fact]
        public void TestHistoryPreviousWithPattern_PatternMaintained()
        {
            InsertAndExecuteInputs("123", "12", "1");

            Test(
                new Step(() => Window.Operations.HistoryPrevious("12"), "12"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryPrevious("12"), "123"),
                new Step(() => Window.Operations.HistoryPrevious("12"), "123"));
        }

        [Fact]
        public void TestHistoryPreviousWithPattern_PatternDropped()
        {
            InsertAndExecuteInputs("1", "2", "3");

            Test(
                new Step(() => Window.Operations.HistoryPrevious("2"), "2"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryPrevious(null), "1"), // Pattern isn't passed, so return to normal iteration.
                new Step(() => Window.Operations.HistoryPrevious(null), "1"));
        }

        [Fact]
        public void TestHistoryPreviousWithPattern_PatternChanged()
        {
            InsertAndExecuteInputs("10", "20", "15", "25");

            Test(
                new Step(() => Window.Operations.HistoryPrevious("1"), "15"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryPrevious("2"), "20"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryPrevious("2"), "20"));
        }

        [Fact]
        public void TestHistoryNextWithPattern_NoMatch()
        {
            InsertAndExecuteInputs("start", "1", "12", "123");
            SetActiveCode("end");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "123"),
                new Step(() => Window.Operations.HistoryPrevious(), "12"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "start"),

                new Step(() => Window.Operations.HistoryNext("4"), "end"),
                new Step(() => Window.Operations.HistoryNext("4"), "end"));
        }

        [Fact]
        public void TestHistoryNextWithPattern_PatternMaintained()
        {
            InsertAndExecuteInputs("start", "1", "12", "123");
            SetActiveCode("end");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "123"),
                new Step(() => Window.Operations.HistoryPrevious(), "12"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "start"),

                new Step(() => Window.Operations.HistoryNext("12"), "12"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryNext("12"), "123"),
                new Step(() => Window.Operations.HistoryNext("12"), "end"));
        }

        [Fact]
        public void TestHistoryNextWithPattern_PatternDropped()
        {
            InsertAndExecuteInputs("start", "3", "2", "1");
            SetActiveCode("end");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "2"),
                new Step(() => Window.Operations.HistoryPrevious(), "3"),
                new Step(() => Window.Operations.HistoryPrevious(), "start"),

                new Step(() => Window.Operations.HistoryNext("2"), "2"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryNext(null), "1"), // Pattern isn't passed, so return to normal iteration.
                new Step(() => Window.Operations.HistoryNext(null), "end"));
        }

        [Fact]
        public void TestHistoryNextWithPattern_PatternChanged()
        {
            InsertAndExecuteInputs("start", "25", "15", "20", "10");
            SetActiveCode("end");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "10"),
                new Step(() => Window.Operations.HistoryPrevious(), "20"),
                new Step(() => Window.Operations.HistoryPrevious(), "15"),
                new Step(() => Window.Operations.HistoryPrevious(), "25"),
                new Step(() => Window.Operations.HistoryPrevious(), "start"),

                new Step(() => Window.Operations.HistoryNext("1"), "15"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryNext("2"), "20"), // Skip over non-matching entry.
                new Step(() => Window.Operations.HistoryNext("2"), "end"));
        }

        [Fact]
        public void TestHistorySearchPrevious()
        {
            InsertAndExecuteInputs("123", "12", "1");

            // Default search string is empty.
            Test(
                new Step(() => Window.Operations.HistorySearchPrevious(), "1"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistorySearchPrevious(), "12"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "123"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "123"));
        }

        [Fact]
        public void TestHistorySearchPreviousWithPattern()
        {
            InsertAndExecuteInputs("123", "12", "1");
            SetActiveCode("12");

            Test(
                new Step(() => Window.Operations.HistorySearchPrevious(), "12"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistorySearchPrevious(), "123"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "123"));
        }

        [Fact]
        public void TestHistorySearchNextWithPattern()
        {
            InsertAndExecuteInputs("12", "123", "12", "1");
            SetActiveCode("end");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistoryPrevious(), "12"),
                new Step(() => Window.Operations.HistoryPrevious(), "123"),
                new Step(() => Window.Operations.HistoryPrevious(), "12"),

                new Step(() => Window.Operations.HistorySearchNext(), "123"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistorySearchNext(), "12"),
                new Step(() => Window.Operations.HistorySearchNext(), "end"));
        }

        [Fact]
        public void TestHistoryPreviousAndSearchPrevious()
        {
            InsertAndExecuteInputs("200", "100", "30", "20", "10", "2", "1");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "10"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistoryPrevious(), "20"), // NB: Doesn't match pattern.
                new Step(() => Window.Operations.HistorySearchPrevious(), "100"), // NB: Reuses existing pattern.
                new Step(() => Window.Operations.HistorySearchPrevious(), "100"),
                new Step(() => Window.Operations.HistoryPrevious(), "200"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "200")); // No-op results in non-matching history entry after SearchPrevious.
        }

        [Fact]
        public void TestHistoryPreviousAndSearchPrevious_ExplicitPattern()
        {
            InsertAndExecuteInputs("200", "100", "30", "20", "10", "2", "1");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "1"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "10"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistoryPrevious("2"), "20"), // NB: Doesn't match pattern.
                new Step(() => Window.Operations.HistorySearchPrevious(), "100"), // NB: Reuses existing pattern.
                new Step(() => Window.Operations.HistorySearchPrevious(), "100"),
                new Step(() => Window.Operations.HistoryPrevious("2"), "200"),
                new Step(() => Window.Operations.HistorySearchPrevious(), "200")); // No-op results in non-matching history entry after SearchPrevious.
        }

        [Fact]
        public void TestHistoryNextAndSearchNext()
        {
            InsertAndExecuteInputs("1", "2", "10", "20", "30", "100", "200");
            SetActiveCode("4");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "200"),
                new Step(() => Window.Operations.HistoryPrevious(), "100"),
                new Step(() => Window.Operations.HistoryPrevious(), "30"),
                new Step(() => Window.Operations.HistoryPrevious(), "20"),
                new Step(() => Window.Operations.HistoryPrevious(), "10"),
                new Step(() => Window.Operations.HistoryPrevious(), "2"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),

                new Step(() => Window.Operations.HistorySearchNext(), "10"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistoryNext(), "20"), // NB: Doesn't match pattern.
                new Step(() => Window.Operations.HistorySearchNext(), "100"), // NB: Reuses existing pattern.
                new Step(() => Window.Operations.HistorySearchNext(), "4"), // Restoring input results in non-matching history entry after SearchNext.
                new Step(() => Window.Operations.HistoryNext(), "4"));
        }

        [Fact]
        public void TestHistoryNextAndSearchNext_ExplicitPattern()
        {
            InsertAndExecuteInputs("1", "2", "10", "20", "30", "100", "200");
            SetActiveCode("4");

            Test(
                new Step(() => Window.Operations.HistoryPrevious(), "200"),
                new Step(() => Window.Operations.HistoryPrevious(), "100"),
                new Step(() => Window.Operations.HistoryPrevious(), "30"),
                new Step(() => Window.Operations.HistoryPrevious(), "20"),
                new Step(() => Window.Operations.HistoryPrevious(), "10"),
                new Step(() => Window.Operations.HistoryPrevious(), "2"),
                new Step(() => Window.Operations.HistoryPrevious(), "1"),

                new Step(() => Window.Operations.HistorySearchNext(), "10"), // Pattern is captured before this step.
                new Step(() => Window.Operations.HistoryNext("2"), "20"), // NB: Doesn't match pattern.
                new Step(() => Window.Operations.HistorySearchNext(), "100"), // NB: Reuses existing pattern.
                new Step(() => Window.Operations.HistorySearchNext(), "4"), // Restoring input results in non-matching history entry after SearchNext.
                new Step(() => Window.Operations.HistoryNext("2"), "4"));
        }
    }
}
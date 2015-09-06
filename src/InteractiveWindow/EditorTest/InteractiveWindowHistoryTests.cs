// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    }
}
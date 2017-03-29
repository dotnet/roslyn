// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive
{
    public static partial class InteractiveExtensions
    {
        public static void SubmitText(this AbstractInteractiveWindowTest test, string text)
            => test.InteractiveWindow.SubmitText(text);

        public static void SendKeys(this AbstractInteractiveWindowTest test, params object[] input)
        {
            test.VisualStudio.Instance.SendKeys.Send(input);
        }

        public static void InsertCode(this AbstractInteractiveWindowTest test, string text)
            => test.InteractiveWindow.InsertCode(text);

        public static void PlaceCaret(
            this AbstractInteractiveWindowTest test, 
            string text, 
            int charsOffset = 0, 
            int occurrence = 0, 
            bool extendSelection = false, 
            bool selectBlock = false)
              => test.InteractiveWindow.PlaceCaret(
                  text,
                  charsOffset,
                  occurrence,
                  extendSelection,
                  selectBlock);


        public static void ClearReplText(this AbstractInteractiveWindowTest test)
        {
            // Dismiss the pop-up (if any)
            test.VisualStudio.Instance.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);

            // Clear the line
            test.VisualStudio.Instance.ExecuteCommand(WellKnownCommandNames.Edit_SelectionCancel);
        }

        public static void Reset(this AbstractInteractiveWindowTest test)
            => test.InteractiveWindow.Reset(waitForPrompt: true);
    }
}
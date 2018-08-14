// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public class AutomationElementHelper
    {
        /// <summary>
        /// Finds the automation element by <paramref name="elementName"/> and clicks on it asynchronously. 
        /// <paramref name="elementName"/> should be set to true if performing a recursive search.
        /// </summary>
        public static async Task ClickAutomationElementAsync(string elementName, bool recursive = false)
        {
            var element = await FindAutomationElementAsync(elementName, recursive).ConfigureAwait(false);

            if (element != null)
            {
                var tcs = new TaskCompletionSource<object>();

                Helper.Automation.AddAutomationEventHandler(
                    UIA_EventIds.UIA_Invoke_InvokedEventId,
                    element,
                    TreeScope.TreeScope_Element,
                    cacheRequest: null,
                    new AutomationEventHandler((src, e) => tcs.SetResult(null)));

                element.Invoke();
                await tcs.Task;
            }
        }

        /// <summary>
        /// Finds the automation element by <paramref name="elementName"/>. 
        /// <paramref name="elementName"/> should be set to true if performing a recursive search.
        /// </summary>
        /// <returns>The task referrign to the element finding.</returns>

        public static async Task<IUIAutomationElement> FindAutomationElementAsync(string elementName, bool recursive = false)
        {
            IUIAutomationElement element = null;
            var scope = recursive ? TreeScope.TreeScope_Descendants : TreeScope.TreeScope_Children;
            var condition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.NameProperty.Id, elementName);

            // TODO(Dustin): This is code is a bit terrifying. If anything goes wrong and the automation
            // element can't be found, it'll continue to spin until the heat death of the universe.
            await IntegrationHelper.WaitForResultAsync(
                () => (element = Helper.Automation.GetRootElement().FindFirst(scope, condition)) != null, expectedResult: true
            ).ConfigureAwait(false);

            return element;
        }

        private class AutomationEventHandler : IUIAutomationEventHandler
        {
            private readonly Action<IUIAutomationElement, int> _action;

            public AutomationEventHandler(Action<IUIAutomationElement, int> action)
            {
                _action = action ?? throw new ArgumentNullException(nameof(action));
            }

            public void HandleAutomationEvent(IUIAutomationElement sender, int eventId)
                => _action(sender, eventId);
        }
    }
}

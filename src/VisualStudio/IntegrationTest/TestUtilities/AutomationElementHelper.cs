// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Windows.Automation;

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

                Automation.AddAutomationEventHandler(InvokePattern.InvokedEvent, element, TreeScope.Element, (src, e) => {
                    tcs.SetResult(null);
                });

                if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePatternObj))
                {
                    var invokePattern = (InvokePattern)invokePatternObj;
                    invokePattern.Invoke();
                }

                await tcs.Task;
            }
        }

        /// <summary>
        /// Finds the automation element by <paramref name="elementName"/>. 
        /// <paramref name="elementName"/> should be set to true if performing a recursive search.
        /// </summary>
        /// <returns>The task referrign to the element finding.</returns>

        public static async Task<AutomationElement> FindAutomationElementAsync(string elementName, bool recursive = false)
        {
            AutomationElement element = null;
            var scope = recursive ? TreeScope.Descendants : TreeScope.Children;
            var condition = new PropertyCondition(AutomationElement.NameProperty, elementName);

            // TODO(Dustin): This is code is a bit terrifying. If anything goes wrong and the automation
            // element can't be found, it'll continue to spin until the heat death of the universe.
            await IntegrationHelper.WaitForResultAsync(
                () => (element = AutomationElement.RootElement.FindFirst(scope, condition)) != null, expectedResult: true
            ).ConfigureAwait(false);

            return element;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using UIAutomationClient;
using AutomationElementIdentifiers = System.Windows.Automation.AutomationElementIdentifiers;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities;

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
            var tcs = new TaskCompletionSource<VoidResult>();

            Helper.Automation.AddAutomationEventHandler(
                UIA_EventIds.UIA_Invoke_InvokedEventId,
                element,
                TreeScope.TreeScope_Element,
                cacheRequest: null,
                new AutomationEventHandler((src, e) => tcs.SetResult(default)));

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
        IUIAutomationElement? element = null;
        var scope = recursive ? TreeScope.TreeScope_Descendants : TreeScope.TreeScope_Children;
        var condition = Helper.Automation.CreatePropertyCondition(AutomationElementIdentifiers.NameProperty.Id, elementName);

        // TODO(Dustin): This is code is a bit terrifying. If anything goes wrong and the automation
        // element can't be found, it'll continue to spin until the heat death of the universe.
        await IntegrationHelper.WaitForResultAsync(
            () => (element = Helper.Automation.GetRootElement().FindFirst(scope, condition)) != null, expectedResult: true
        ).ConfigureAwait(false);

        Contract.ThrowIfNull(element);
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

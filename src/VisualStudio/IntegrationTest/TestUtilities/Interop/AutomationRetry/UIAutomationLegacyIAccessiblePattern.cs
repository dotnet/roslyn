// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationLegacyIAccessiblePattern : AutomationRetryWrapper<IUIAutomationLegacyIAccessiblePattern>, IUIAutomationLegacyIAccessiblePattern
    {
        public UIAutomationLegacyIAccessiblePattern(IUIAutomationLegacyIAccessiblePattern automationObject)
            : base(automationObject)
        {
        }

        public int CurrentChildId => Retry(obj => obj.CurrentChildId);

        public string CurrentName => Retry(obj => obj.CurrentName);

        public string CurrentValue => Retry(obj => obj.CurrentValue);

        public string CurrentDescription => Retry(obj => obj.CurrentDescription);

        public uint CurrentRole => Retry(obj => obj.CurrentRole);

        public uint CurrentState => Retry(obj => obj.CurrentState);

        public string CurrentHelp => Retry(obj => obj.CurrentHelp);

        public string CurrentKeyboardShortcut => Retry(obj => obj.CurrentKeyboardShortcut);

        public string CurrentDefaultAction => Retry(obj => obj.CurrentDefaultAction);

        public int CachedChildId => Retry(obj => obj.CachedChildId);

        public string CachedName => Retry(obj => obj.CachedName);

        public string CachedValue => Retry(obj => obj.CachedValue);

        public string CachedDescription => Retry(obj => obj.CachedDescription);

        public uint CachedRole => Retry(obj => obj.CachedRole);

        public uint CachedState => Retry(obj => obj.CachedState);

        public string CachedHelp => Retry(obj => obj.CachedHelp);

        public string CachedKeyboardShortcut => Retry(obj => obj.CachedKeyboardShortcut);

        public string CachedDefaultAction => Retry(obj => obj.CachedDefaultAction);

        public void Select(int flagsSelect) => Retry(obj => obj.Select(flagsSelect));

        public void DoDefaultAction() => Retry(obj => obj.DoDefaultAction());

        public void SetValue(string szValue) => Retry(obj => obj.SetValue(szValue));

        public IUIAutomationElementArray GetCurrentSelection() => Retry(obj => obj.GetCurrentSelection());

        public IUIAutomationElementArray GetCachedSelection() => Retry(obj => obj.GetCachedSelection());

        public IAccessible GetIAccessible() => Retry(obj => obj.GetIAccessible());
    }
}

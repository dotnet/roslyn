// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationProxyFactoryMapping : AutomationRetryWrapper<IUIAutomationProxyFactoryMapping>, IUIAutomationProxyFactoryMapping
    {
        public UIAutomationProxyFactoryMapping(IUIAutomationProxyFactoryMapping automationObject)
            : base(automationObject)
        {
        }

        public uint count => Retry(obj => obj.count);

        public IUIAutomationProxyFactoryEntry[] GetTable() => Retry(obj => obj.GetTable());

        public IUIAutomationProxyFactoryEntry GetEntry(uint index) => Retry(obj => obj.GetEntry(index));

        public void SetTable(IUIAutomationProxyFactoryEntry[] factoryList) => Retry(obj => obj.SetTable(AutomationRetryWrapper.Unwrap(factoryList)));

        public void InsertEntries(uint before, IUIAutomationProxyFactoryEntry[] factoryList) => Retry(obj => obj.InsertEntries(before, AutomationRetryWrapper.Unwrap(factoryList)));

        public void InsertEntry(uint before, IUIAutomationProxyFactoryEntry factory) => Retry(obj => obj.InsertEntry(before, AutomationRetryWrapper.Unwrap(factory)));

        public void RemoveEntry(uint index) => Retry(obj => obj.RemoveEntry(index));

        public void ClearTable() => Retry(obj => obj.ClearTable());

        public void RestoreDefaultTable() => Retry(obj => obj.RestoreDefaultTable());
    }
}

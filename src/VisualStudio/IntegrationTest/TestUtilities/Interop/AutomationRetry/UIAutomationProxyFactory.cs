// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationProxyFactory : AutomationRetryWrapper<IUIAutomationProxyFactory>, IUIAutomationProxyFactory
    {
        public UIAutomationProxyFactory(IUIAutomationProxyFactory automationObject)
            : base(automationObject)
        {
        }

        public string ProxyFactoryId => Retry(obj => obj.ProxyFactoryId);

        public IRawElementProviderSimple CreateProvider(IntPtr hwnd, int idObject, int idChild) => Retry(obj => obj.CreateProvider(hwnd, idObject, idChild));
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class RawElementProviderSimple : AutomationRetryWrapper<IRawElementProviderSimple>, IRawElementProviderSimple
    {
        public RawElementProviderSimple(IRawElementProviderSimple automationObject)
            : base(automationObject)
        {
        }

        public ProviderOptions ProviderOptions => Retry(obj => obj.ProviderOptions);

        public IRawElementProviderSimple HostRawElementProvider => Retry(obj => obj.HostRawElementProvider);

        public object GetPatternProvider(int patternId) => Retry(obj => obj.GetPatternProvider(patternId));

        public object GetPropertyValue(int propertyId) => Retry(obj => obj.GetPropertyValue(propertyId));
    }
}

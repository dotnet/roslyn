// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class Unknown : AutomationRetryWrapper<IUnknown>, IUnknown
    {
        public Unknown(IUnknown automationObject)
            : base(automationObject)
        {
        }
    }
}

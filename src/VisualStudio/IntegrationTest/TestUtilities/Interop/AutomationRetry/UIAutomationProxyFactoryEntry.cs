// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal class UIAutomationProxyFactoryEntry : AutomationRetryWrapper<IUIAutomationProxyFactoryEntry>, IUIAutomationProxyFactoryEntry
    {
        public UIAutomationProxyFactoryEntry(IUIAutomationProxyFactoryEntry automationObject)
            : base(automationObject)
        {
        }

        public IUIAutomationProxyFactory ProxyFactory => Retry(obj => obj.ProxyFactory);

        public string ClassName
        {
            get => Retry(obj => obj.ClassName);
            set => Retry(obj => obj.ClassName = value);
        }

        public string ImageName
        {
            get => Retry(obj => obj.ImageName);
            set => Retry(obj => obj.ImageName = value);
        }

        public int AllowSubstringMatch
        {
            get => Retry(obj => obj.AllowSubstringMatch);
            set => Retry(obj => obj.AllowSubstringMatch = value);
        }

        public int CanCheckBaseClass
        {
            get => Retry(obj => obj.CanCheckBaseClass);
            set => Retry(obj => obj.CanCheckBaseClass = value);
        }

        public int NeedsAdviseEvents
        {
            get => Retry(obj => obj.NeedsAdviseEvents);
            set => Retry(obj => obj.NeedsAdviseEvents = value);
        }

        public void SetWinEventsForAutomationEvent(int eventId, int propertyId, uint[] winEvents) => Retry(obj => obj.SetWinEventsForAutomationEvent(eventId, propertyId, winEvents));

        public uint[] GetWinEventsForAutomationEvent(int eventId, int propertyId) => Retry(obj => obj.GetWinEventsForAutomationEvent(eventId, propertyId));
    }
}

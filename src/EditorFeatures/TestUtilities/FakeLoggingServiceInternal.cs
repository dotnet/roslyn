// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    // Provide an export of ILoggingServiceInternal to work around https://devdiv.visualstudio.com/DevDiv/_workitems/edit/570290
    [Export(typeof(ILoggingServiceInternal))]
    [Shared]
    [PartNotDiscoverable]
    internal sealed class FakeLoggingServiceInternal : ILoggingServiceInternal
    {
        public void AdjustCounter(string key, string name, int delta = 1)
        {
            throw new NotImplementedException();
        }

        public void PostCounters()
        {
            throw new NotImplementedException();
        }

        public void PostEvent(string key, params object[] namesAndProperties)
        {
            throw new NotImplementedException();
        }

        public void PostEvent(string key, IReadOnlyList<object> namesAndProperties)
        {
            throw new NotImplementedException();
        }

        public void PostEvent(TelemetryEventType eventType, string eventName, TelemetryResult result = TelemetryResult.Success, params (string name, object property)[] namesAndProperties)
        {
            throw new NotImplementedException();
        }

        public void PostEvent(TelemetryEventType eventType, string eventName, TelemetryResult result, IReadOnlyList<(string name, object property)> namesAndProperties)
        {
            throw new NotImplementedException();
        }

        public void PostFault(string eventName, string description, Exception exceptionObject, string additionalErrorInfo, bool? isIncludedInWatsonSample)
        {
            throw new NotImplementedException();
        }
    }
}

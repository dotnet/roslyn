// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using UIAutomationClient;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Interop.AutomationRetry
{
    internal static class AutomationRetryWrapper
    {
        private static readonly Dictionary<Type, Func<object, object>> _wrapperFunctions =
            new Dictionary<Type, Func<object, object>>
            {
            };

        public static T WrapIfNecessary<T>(T value)
        {
            if (!_wrapperFunctions.TryGetValue(typeof(T), out var wrapperFunction))
            {
                // Objects which are not recognized automation objects are not wrapped
                return value;
            }

            return (T)wrapperFunction(value);
        }
    }
}

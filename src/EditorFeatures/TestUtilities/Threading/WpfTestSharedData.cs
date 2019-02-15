// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;

namespace Roslyn.Test.Utilities
{
    public sealed class WpfTestSharedData
    {
        internal static readonly WpfTestSharedData Instance = new WpfTestSharedData();

        /// <summary>
        /// Holds the last 10 test cases executed: more recent test cases will occur later in the 
        /// list. Useful for debugging deadlocks that occur because state leak between runs. 
        /// </summary>
        private readonly List<string> _recentTestCases = new List<string>();

        public readonly SemaphoreSlim TestSerializationGate = new SemaphoreSlim(1, 1);

        private WpfTestSharedData()
        {
        }

        public void ExecutingTest(ITestMethod testMethod)
        {
            var name = $"{testMethod.TestClass.Class.Name}::{testMethod.Method.Name}";
            lock (_recentTestCases)
            {
                _recentTestCases.Add(name);
            }
        }

        public void ExecutingTest(MethodInfo testMethod)
        {
            var name = $"{testMethod.DeclaringType.Name}::{testMethod.Name}";
            lock (_recentTestCases)
            {
                _recentTestCases.Add(name);
            }
        }
    }
}

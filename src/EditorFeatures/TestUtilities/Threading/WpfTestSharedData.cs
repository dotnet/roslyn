// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        private readonly List<string> _recentTestCases = [];

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

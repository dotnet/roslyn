// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;

namespace Roslyn.Test.Utilities;

public sealed class WpfTestSharedData
{
    internal static readonly WpfTestSharedData Instance = new();

    /// <summary>
    /// Holds the last 10 test cases executed: more recent test cases will occur later in the 
    /// list. Useful for debugging deadlocks that occur because state leak between runs. 
    /// </summary>
    private readonly List<string> _recentTestCases = [];

    public readonly SemaphoreSlim TestSerializationGate = new(1, 1);

    private WpfTestSharedData()
    {
    }

    public void ExecutingTest(ITestMethod testMethod)
    {
        lock (_recentTestCases)
        {
            _recentTestCases.Add($"{testMethod.TestClass.Class.Name}::{testMethod.Method.Name}");
        }
    }

    public void ExecutingTest(MethodInfo testMethod)
    {
        lock (_recentTestCases)
        {
            _recentTestCases.Add($"{testMethod.DeclaringType.Name}::{testMethod.Name}");
        }
    }
}

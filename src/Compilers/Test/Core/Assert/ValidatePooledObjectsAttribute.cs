// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit.Sdk;

#if DEBUG
using Microsoft.CodeAnalysis.PooledObjects;
#endif

namespace Roslyn.Test.Utilities;

/// <summary>
/// Apply this attribute to a test class or method to verify that all pooled objects
/// allocated during the test are returned to their pool before the test completes.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ValidatePooledObjectsAttribute : BeforeAfterTestAttribute
{
    /// <summary>
    /// When set to <see langword="true"/>, allocation stack traces are captured for each pooled object,
    /// making it easier to locate the source of leaks. This adds overhead, so it is off by default.
    /// </summary>
    public bool TraceLeaks { get; set; }

#if DEBUG
    [ThreadStatic]
    private static PoolTrackingContext? s_context;
#endif

    public override void Before(MethodInfo methodUnderTest)
    {
#if DEBUG
        PoolTracker.StartTracking(out var context, TraceLeaks);
        s_context = context;
#endif
    }

    public override void After(MethodInfo methodUnderTest)
    {
#if DEBUG
        var context = s_context;
        s_context = null;

        PoolTracker.StopTracking();

        if (context?.HasLeaks == true)
        {
            throw new XunitException(context.GetLeakSummary());
        }
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using Xunit;
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
    /// making it easier to locate the source of leaks. This adds significant overhead, so it is off by default.
    /// </summary>
    public bool TraceLeaks { get; set; }

    /// <summary>
    /// When set to a non-<see langword="null"/> value, pool leak validation is skipped.
    /// The value should describe the reason for skipping. When applied at the method level,
    /// this also suppresses validation from a class-level attribute.
    /// </summary>
    public string? Skip { get; set; }

#if DEBUG
    /// <summary>
    /// Set to <see langword="true"/> on the current execution context when any instance
    /// has <see cref="Skip"/> set. Once set, all instances (including a class-level attribute)
    /// suppress leak validation for the current test.
    /// </summary>
    private static readonly AsyncLocal<bool> s_skipValidation = new();

    private PoolTrackingContext? _context;
#endif

    public override void Before(MethodInfo methodUnderTest)
    {
#if DEBUG
        if (Skip is not null)
        {
            s_skipValidation.Value = true;
            return;
        }

        Assert.True(!TraceLeaks || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")),
            "Tracing leaks is very slow, shouldn't be set in CI.");

        PoolTracker.StartTracking(out var context, TraceLeaks);
        _context = context;
#endif
    }

    public override void After(MethodInfo methodUnderTest)
    {
#if DEBUG
        if (Skip is not null)
            return;

        var context = _context;
        _context = null;

        PoolTracker.StopTracking();

        if (!s_skipValidation.Value && context?.HasLeaks == true)
        {
            throw new XunitException(context.GetLeakSummary());
        }
#endif
    }
}

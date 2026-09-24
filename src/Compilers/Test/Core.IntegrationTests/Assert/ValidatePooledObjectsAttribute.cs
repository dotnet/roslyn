// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using Xunit;
using Xunit.Sdk;
using Microsoft.CodeAnalysis;

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
    /// When set to a non-<see langword="null"/> value, pool leak validation is skipped entirely (no tracking).
    /// The value should describe the reason for skipping. When applied at the method level,
    /// this also suppresses validation from a class-level attribute.
    /// </summary>
    public string? Skip { get; set; }

    /// <summary>
    /// When set to a non-<see langword="null"/> value, leaks are expected and the test will
    /// fail if no leaks are detected (i.e., the expectation is no longer necessary and should be removed).
    /// The value should describe the reason leaks are expected.
    /// When applied at the method level, this also suppresses validation from a class-level attribute.
    /// </summary>
    public string? LeakReason { get; set; }

    /// <summary>
    /// When set to <see langword="true"/>, waits briefly for outstanding objects to be freed before reporting leaks.
    /// Command-line tests need this because the analyzer driver has a background task which <see cref="CommonCompiler.Run"/> doesn't always wait for.
    /// </summary>
    public bool WaitForOutstandingObjectsToBeFreed { get; set; }

#if DEBUG
    /// <summary>
    /// When a method-level attribute has <see cref="Skip"/> or <see cref="LeakReason"/> set,
    /// this flag suppresses the class-level attribute's validation for the same test.
    /// </summary>
    private static readonly AsyncLocal<bool> s_suppressClassLevelValidation = new();

    private static readonly TimeSpan s_asyncCleanupTimeout = TimeSpan.FromSeconds(5);

    private PoolTrackingContext? _context;
#endif

    public override void Before(MethodInfo methodUnderTest)
    {
#if DEBUG
        if (Skip is not null)
        {
            s_suppressClassLevelValidation.Value = true;
            return;
        }

        Assert.True(!TraceLeaks || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI")),
            "Tracing leaks is very slow, shouldn't be set in CI.");

        if (LeakReason is not null)
        {
            s_suppressClassLevelValidation.Value = true;
        }

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

        if (LeakReason is null && !s_suppressClassLevelValidation.Value && WaitForOutstandingObjectsToBeFreed)
        {
            context?.WaitForOutstandingObjectsToBeFreed(s_asyncCleanupTimeout);
        }

        PoolTracker.StopTracking();

        if (LeakReason is not null)
        {
            if (context?.HasLeaks != true)
            {
                throw new XunitException(
                    $"{nameof(LeakReason)} was set but no leaks were detected. Remove {nameof(LeakReason)}. Reason was: {LeakReason}");
            }

            return;
        }

        if (!s_suppressClassLevelValidation.Value && context?.HasLeaks == true)
        {
            throw new XunitException(context.GetLeakSummary());
        }
#endif
    }
}

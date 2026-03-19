// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

internal sealed class TestFixtureHelper<TFixture>
    where TFixture : class, IDisposable, new()
{
    private readonly object _gate = new();

    /// <summary>
    /// Holds a weak reference to the current test fixture instance. This reference allows
    /// <see cref="GetOrCreateFixture"/> to access and add a reference to the current test fixture if one exists,
    /// but does not prevent the fixture from being disposed after the last reference to it is released.
    /// </summary>
    private ReferenceCountedDisposable<TFixture>.WeakReference _weakFixture;

    /// <summary>
    /// Gets a reference to a test fixture, or creates it if one does not already exist.
    /// </summary>
    /// <remarks>
    /// <para>The resulting test fixture will not be disposed until the last referencer disposes of its reference.
    /// It is possible for more than one test fixture to be created during the life of any single test, but only one
    /// test fixture will be live at any given point.</para>
    ///
    /// <para>The following shows how a block of test code can ensure a single test fixture is created and used
    /// within any given block of code:</para>
    ///
    /// <code>
    /// using (var fixture = GetOrCreateFixture())
    /// {
    ///   // The test fixture 'fixture' is guaranteed to not be released or replaced within this block
    /// }
    /// </code>
    /// </remarks>
    /// <returns>The test fixture instance.</returns>
    internal ReferenceCountedDisposable<TFixture> GetOrCreateFixture()
    {
        lock (_gate)
        {
            if (_weakFixture.TryAddReference() is { } fixture)
                return fixture;

            var result = new ReferenceCountedDisposable<TFixture>(new TFixture());
            _weakFixture = new ReferenceCountedDisposable<TFixture>.WeakReference(result);
            return result;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET472
using System;
using System.IO;
using System.Reflection;
using Xunit.Abstractions;

namespace Roslyn.Test.Utilities.Desktop;

/// <summary>
/// Allows using an <see cref="ITestOutputHelper"/> across <see cref="AppDomain"/>
/// instances
/// </summary>
public sealed class AppDomainTestOutputHelper : MarshalByRefObject, ITestOutputHelper
{
    public ITestOutputHelper TestOutputHelper { get; }

    public AppDomainTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        TestOutputHelper = testOutputHelper;
    }

    public void WriteLine(string message) =>
        TestOutputHelper.WriteLine(message);

    public void WriteLine(string format, params object[] args) =>
        TestOutputHelper.WriteLine(format, args);
}

#endif

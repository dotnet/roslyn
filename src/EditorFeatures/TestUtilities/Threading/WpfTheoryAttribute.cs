// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias XunitStaFact;

using System;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer(typeof(XunitStaFact::Xunit.Sdk.WpfTheoryDiscoverer))]
public class WpfTheoryAttribute : TheoryAttribute
{
    public WpfTheoryAttribute(
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
    }
}

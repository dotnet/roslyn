// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

/// <summary>
/// A test case that runs on a WPF STA thread with a Dispatcher.
/// </summary>
public sealed class WpfTestCase : XunitTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public WpfTestCase() { }

    public WpfTestCase(
        IXunitTestMethod testMethod,
        string testCaseDisplayName,
        string uniqueID,
        bool @explicit,
        Type[] skipExceptions = null,
        string skipReason = null,
        Type skipType = null,
        string skipUnless = null,
        string skipWhen = null,
        Dictionary<string, HashSet<string>> traits = null,
        object[] testMethodArguments = null)
        : base(testMethod, testCaseDisplayName, uniqueID, @explicit,
               skipExceptions, skipReason, skipType, skipUnless, skipWhen,
               traits, testMethodArguments,
               sourceFilePath: null, sourceLineNumber: null, timeout: 0)
    {
    }
}

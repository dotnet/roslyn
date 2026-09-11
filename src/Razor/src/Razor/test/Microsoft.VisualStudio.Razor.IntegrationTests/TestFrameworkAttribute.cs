// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

[assembly: XunitV3::Xunit.TestFrameworkAttribute(typeof(Xunit.Harness.IdeTestFramework))]

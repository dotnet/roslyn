// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Test.Utilities;

/// <summary>
/// This is a marker attribute used to define that all tests in this assembly
/// should run in a single partition without other assemblies due to state sharing concerns.
/// For example, Microsoft.CodeAnalysis.CSharp.EndToEnd.UnitTests
/// 
/// RunTests uses this attribute when partitioning tests.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class RunTestsInSinglePartitionAttribute : Attribute
{
}
